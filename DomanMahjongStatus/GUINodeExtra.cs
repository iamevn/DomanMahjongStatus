using FFXIVClientStructs.FFXIV.Component.GUI;
using Optional;
using Optional.Collections;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DomanMahjongStatus
{
    public static class GUINodeExtra
    {
        #region old defs
        public static unsafe AtkTextureResource* GetImageTextureResource(AtkResNode* maybeImageNode)
        {
            if (maybeImageNode != null && maybeImageNode->Type == NodeType.Image)
                return GetImageTextureResource((AtkImageNode*)maybeImageNode);
            else
                return null;
        }

        public static unsafe AtkTextureResource* GetImageTextureResource(AtkImageNode* imageNode)
        {
            if (imageNode->PartsList != null && imageNode->PartId <= imageNode->PartsList->PartCount)
            {
                var texInfo = imageNode->PartsList->Parts[imageNode->PartId].UldAsset;
                var texType = texInfo->AtkTexture.TextureType;
                if (texType == TextureType.Resource)
                {
                    return texInfo->AtkTexture.Resource;
                }
            }
            return null;
        }

        public static unsafe AtkResNode* GetImmediateChildWithId(AtkResNode* node, int childId)
        {
            AtkResNode*[] children = GUINodeUtils.GetImmediateChildNodes(node);
            if (children != null)
            {
                foreach (AtkResNode* child in children)
                {
                    if (child != null && child->NodeID == childId)
                        return child;
                }
            }
            return null;
        }

        public static unsafe AtkResNode* GetChildWithId(AtkResNode* node, int childId)
        {
            AtkResNode*[] children = GUINodeUtils.GetAllChildNodes(node);
            foreach (AtkResNode* child in children)
            {
                if (child != null && child->NodeID == childId)
                    return child;
            }
            return null;
        }


        public static unsafe AtkResNode* GetChildWithId(AtkComponentBase* node, int childId)
        {
            if (node != null)
            {
                foreach (AtkResNode* child in GetChildren(node))
                {
                    if (child != null && child->NodeID == childId)
                        return child;
                }
            }
            return null;
        }

        public static unsafe AtkResNode* GetChildNested(AtkResNode* root, params int[] ids)
        {
            var node = root;
            for (int i = 0; i < ids.Length; i++)
            {
                node = GetImmediateChildWithId(node, ids[i]);
            }
            return node;
        }

        public static unsafe int GetImmediateChildCount(AtkResNode* node)
        {
            int count = 0;
            if (node != null)
            {
                var child = node->ChildNode;
                while (child != null)
                {
                    count += 1;
                    child = child->PrevSiblingNode;
                }
            }
            return count;
        }

        public static unsafe AtkResNode*[] GetChildren(AtkComponentBase* cmpBaseNode)
        {
            var listAddr = new List<ulong>();

            if (cmpBaseNode != null && (cmpBaseNode->UldManager.RootNode) != null)
            {
                listAddr.Add((ulong)cmpBaseNode->UldManager.RootNode);

                var node = cmpBaseNode->UldManager.RootNode;
                while (node->PrevSiblingNode != null)
                {
                    listAddr.Add((ulong)node->PrevSiblingNode);
                    node = node->PrevSiblingNode;
                }
            }

            return GUINodeUtils.ConvertToNodeArr(listAddr);
        }
        #endregion old defs

        #region take two
        public static unsafe Option<T> MaybeDeref<T>(T* maybeNull) where T : unmanaged => maybeNull switch
        {
            null => Option.None<T>(),
            _ => maybeNull->Some(),
        };

        public static unsafe bool IsComponent(AtkResNode* maybeComponent)
        {
            return (maybeComponent != null && maybeComponent->IsComponent());
        }
        public static bool IsComponent(this AtkResNode maybeComponent)
        {
            return (int)maybeComponent.Type >= 1000;
        }

        public static unsafe List<AtkResNode> ListSiblings(AtkResNode* node, bool onlyPrevSiblings = false)
        {
            if (node == null)
                return new();
            else
                return node->ListSiblings(onlyPrevSiblings);
        }
        public static List<AtkResNode> ListSiblings(this AtkResNode node, bool onlyPrevSiblings = false)
        {
            List<AtkResNode> siblings = new();
            var start = node;
            if (!onlyPrevSiblings)
            {
                unsafe
                {
                    while (start.NextSiblingNode != null)
                    {
                        start = *start.NextSiblingNode;
                    }
                }
            }

            unsafe
            {
                for (AtkResNode* siblingPtr = &start; siblingPtr != null; siblingPtr = siblingPtr->PrevSiblingNode)
                {
                    siblings.Add(*siblingPtr);
                }
            }

            return siblings;
        }

        public static unsafe int CountChildren(AtkResNode* node)
        {
            if (node == null)
                return 0;
            else
                return node->CountChildren();
        }
        public static int CountChildren(this AtkResNode node)
        {
            int count = 0;
            unsafe
            {
                for (AtkResNode* child = node.ChildNode; child != null; child = child->PrevSiblingNode)
                    count += 1;
            }
            return count;
        }

        public static unsafe List<AtkResNode> ListChildren(AtkResNode* node)
        {
            if (node == null)
            {
                return new();
            }
            else
            {
                return node->ListChildren();
            }
        }
        public static List<AtkResNode> ListChildren(this AtkResNode node)
        {
            unsafe
            {
                List<AtkResNode> children = ListSiblings(node.ChildNode);

                if (node.ChildCount != children.Count)
                    Dalamud.Logging.PluginLog.Log("AtkResNode* {ptr} [id:{id}] reports {claim} children but has {check} children.",
                                                  ((IntPtr)(&node)).ToString("X"),
                                                  node.NodeID,
                                                  node.ChildCount,
                                                  children.Count);
                return children;
            }
        }
        public static unsafe List<AtkResNode> ListChildren(AtkComponentBase* node)
        {
            if (node == null)
            {
                return new();
            }
            else
            {
                return node->ListChildren();
            }
        }
        public static List<AtkResNode> ListChildren(this AtkComponentBase node)
        {
            unsafe
            {
                List<AtkResNode> children = ListSiblings(node.UldManager.RootNode);
                return children;
            }
        }
        public static unsafe List<AtkResNode> ListComponentChildren(AtkComponentNode* node)
        {
            if (node == null)
                return new();
            else
                return node->ListComponentChildren();
        }
        public static List<AtkResNode> ListComponentChildren(this AtkComponentNode node)
        {
            unsafe
            {
                return ListChildren(node.Component);
            }
        }

        public static unsafe Option<AtkResNode> GetChild(AtkResNode* parent, int childID)
            => MaybeDeref(parent).FlatMap(node => node.GetChild(childID));
        public static Option<AtkResNode> GetChild(this AtkResNode parent, int childID)
            => parent.ListChildren().FirstOrNone(child => child.NodeID == childID);
        public static unsafe Option<AtkResNode> GetChild(AtkComponentBase* parentComponentBase, int childID)
            => MaybeDeref(parentComponentBase).FlatMap(node => node.GetChild(childID));
        public static Option<AtkResNode> GetChild(this AtkComponentBase parentComponentBase, int childID)
            => parentComponentBase.ListChildren().FirstOrNone(child => child.NodeID == childID);
        public static unsafe Option<AtkResNode> GetComponentChild(AtkComponentNode* parentNode, int childID)
            => MaybeDeref(parentNode).FlatMap(node => node.GetComponentChild(childID));
        public static Option<AtkResNode> GetComponentChild(this AtkComponentNode parentNode, int childID)
            => parentNode.ListComponentChildren().FirstOrNone(child => child.NodeID == childID);
        #endregion take two

        #region ptr
        public unsafe class UnmanagedPtr<T> where T : unmanaged
        {
            public T* Ptr { get; init; }
            public UnmanagedPtr(T* rawPtr)
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
        }

        public static unsafe Option<UnmanagedPtr<T>> MaybePtr<T>(T* maybeNull) where T : unmanaged
            => UnmanagedPtr<T>.MaybeFrom(maybeNull);

        #endregion ptr

        public static unsafe Option<UnmanagedPtr<AtkResNode>> ChildChain(this UnmanagedPtr<AtkResNode> node, params int[] ids)
        {
            if (ids.Length == 0)
            {
                return node.Some();
            }
            else if (IsComponent(node.Ptr))
            {
                var componentBase = MaybePtr(node.Ptr->GetComponent());
                Option<UnmanagedPtr<AtkResNode>> child = componentBase.FlatMap(cb => MaybePtr(GetChildWithId(cb.Ptr, ids[0])));
                return child.FlatMap(childNode => childNode.ChildChain(ids.Rest()));
            }
            else
            {
                Option<UnmanagedPtr<AtkResNode>> child = MaybePtr(GetChildWithId(node.Ptr, ids[0]));
                return child.FlatMap(childNode => childNode.ChildChain(ids.Rest()));
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