using FFXIVClientStructs.FFXIV.Component.GUI;
using Optional;
using Optional.Collections;
using Optional.Unsafe;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DomanMahjongStatus
{
    public static class GUINodeExtra
    {
        public unsafe class UnmanagedPtr<T> where T : unmanaged
        {
            public T* Ptr { get; init; }

            private UnmanagedPtr(T* rawPtr)
            {
                Ptr = rawPtr;
            }

            public static Option<UnmanagedPtr<U>> MaybeFrom<U>(U* maybeNull) where U : unmanaged
            {
                if (maybeNull == null)
                    return Option.None<UnmanagedPtr<U>>();
                else
                    return Option.Some(new UnmanagedPtr<U>(maybeNull));
            }

            public UnmanagedPtr<U> Cast<U>() where U : unmanaged
            {
                return new UnmanagedPtr<U>((U*)Ptr);
            }

            public static implicit operator T*(UnmanagedPtr<T> p) => p.Ptr;
            public T Deref() => *Ptr;
        }
        public static unsafe Option<UnmanagedPtr<T>> MaybePtr<T>(T* maybeNull) where T : unmanaged
            => UnmanagedPtr<T>.MaybeFrom(maybeNull);

        public static unsafe bool IsComponent(this UnmanagedPtr<AtkResNode> node)
            => (int)node.Ptr->Type >= 1000;

        public static unsafe bool IdMatches(AtkResNode* node, int id) => node is not null && node->NodeID == id;
        public static unsafe bool IdMatches(this UnmanagedPtr<AtkResNode> node, int id) => IdMatches(node.Ptr, id);

        public static unsafe UnmanagedPtr<AtkResNode>[] GetChildren(this UnmanagedPtr<AtkResNode> root)
        {
            var children = new List<UnmanagedPtr<AtkResNode>>();
            var child = MaybePtr(root.Ptr->ChildNode);
            while (child.HasValue)
            {
                child.MatchSome(node =>
                {
                    children.Add(node);
                    child = MaybePtr(node.Ptr->PrevSiblingNode);
                });
            }
            return children.ToArray();
        }
        public static unsafe UnmanagedPtr<AtkResNode>[] GetChildren(this UnmanagedPtr<AtkComponentBase> cmpBaseNode)
        {
            var children = new List<UnmanagedPtr<AtkResNode>>();
            var root = MaybePtr(cmpBaseNode.Ptr->UldManager.RootNode);
            root.MatchSome(rootChild =>
            {
                children.Add(rootChild);

                var nodePtr = MaybePtr(rootChild.Ptr->PrevSiblingNode);
                while (nodePtr.HasValue)
                {
                    nodePtr.MatchSome(node =>
                    {
                        children.Add(node);
                        nodePtr = MaybePtr(node.Ptr->PrevSiblingNode);
                    });
                }
            });

            return children.ToArray();
        }

        public static unsafe Option<UnmanagedPtr<AtkResNode>> GetChild(this UnmanagedPtr<AtkResNode> node, params int[] ids)
        {
            if (ids.Length == 0)
            {
                return node.Some();
            }
            else if (node.IsComponent())
            {
                var componentBase = MaybePtr(node.Ptr->GetComponent());
                Option<UnmanagedPtr<AtkResNode>> child = componentBase.Map(GetChildren).ValueOrDefault()
                    .FirstOrNone(child => child.IdMatches(ids[0]));
                return child.FlatMap(childNode => childNode.GetChild(ids.Rest()));
            }
            else
            {
                Option<UnmanagedPtr<AtkResNode>> child = node.GetChildren()
                    .FirstOrNone(child => child.IdMatches(ids[0]));
                return child.FlatMap(childNode => childNode.GetChild(ids.Rest()));
            }
        }

        public static unsafe Option<string> GetNodeText(this UnmanagedPtr<AtkResNode> maybeTextNode)
        {
            return maybeTextNode.SomeWhen(node => node.Ptr->Type == NodeType.Text)
                .Map(ptr => ptr.Cast<AtkTextNode>())
                .FlatMap(GetNodeText);
        }
        public static unsafe Option<string> GetNodeText(this UnmanagedPtr<AtkTextNode> textNode)
        {
            string text = Marshal.PtrToStringUTF8(new IntPtr(textNode.Ptr->NodeText.StringPtr));
            return text.SomeWhen(t => t != null && t.Length > 0);
        }

        public static unsafe Option<UnmanagedPtr<AtkTextureResource>> GetImageTextureResource(this UnmanagedPtr<AtkResNode> maybeImageNode)
            => maybeImageNode.SomeWhen(node => node.Ptr->Type == NodeType.Image)
                .Map(node => node.Cast<AtkImageNode>())
                .FlatMap(GetImageTextureResource);
        public static unsafe Option<UnmanagedPtr<AtkTextureResource>> GetImageTextureResource(this UnmanagedPtr<AtkImageNode> imageNode)
        {
            AtkImageNode* imagePtr = imageNode.Ptr;
            int partId = imagePtr->PartId;
            return MaybePtr(imagePtr->PartsList)
                .Filter(parts => partId <= parts.Ptr->PartCount)
                .FlatMap(parts => MaybePtr(parts.Ptr->Parts[imagePtr->PartId].UldAsset))
                .Filter(part => part.Ptr->AtkTexture.TextureType == TextureType.Resource)
                .FlatMap(tex => MaybePtr(tex.Ptr->AtkTexture.Resource));
        }

        public static T[] Rest<T>(this T[] arr) => new ArraySegment<T>(arr).Slice(1).ToArray();

        public static Option<int> TryParseInt(this string s)
        {
            if (int.TryParse(s, out int i))
                return i.Some();
            else
                return Option.None<int>();
        }

        public static Option<TEnum> TryParseEnum<TEnum>(this string s, bool ignoreCase = false) where TEnum : struct
        {
            if (Enum.TryParse(s, ignoreCase, out TEnum value))
                return value.Some();
            else
                return Option.None<TEnum>();
        }
    }
}