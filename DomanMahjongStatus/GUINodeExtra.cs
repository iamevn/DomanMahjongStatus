using FFXIVClientStructs.FFXIV.Component.GUI;
using Optional;
using Optional.Collections;
using Optional.Unsafe;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static DomanMahjongStatus.Util;

namespace DomanMahjongStatus
{
    public static class GUINodeExtra
    {

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

        public static unsafe Option<UnmanagedPtr<AtkResNode>> GetChild(this UnmanagedPtr<AtkComponentBase> node, params int[] ids)
            => MaybePtr(node.Deref().OwnerNode)
            .Map(node => node.Cast<AtkResNode>())
            .FlatMap(node => node.GetChild(ids));

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

        public static unsafe bool ComponentTypeIs(this UnmanagedPtr<AtkComponentBase> basePtr, ComponentType componentType)
        {
            Option<UnmanagedPtr<AtkUldComponentInfo>> info = MaybePtr(basePtr.Deref().UldManager.Objects)
                .Map(ptr => ptr.Cast<AtkUldComponentInfo>());
            var baseType = info.Map(info => info.Deref().ComponentType);
            var res = baseType.Map(ct => ct == componentType);
            return res.ValueOr(false);
        }
        public static unsafe Option<UnmanagedPtr<AtkComponentList>> GetAsListComponent(this UnmanagedPtr<AtkComponentBase> basePtr)
            // this is safeish (the other thing Objects could be cast as has an enum at same offset as AtkUldComponentInfo.ComponentType
            // which doesn't have an item with the same value as ComponentType.List)
            => basePtr
                .SomeWhen(b => b.ComponentTypeIs(ComponentType.List))
                .Map(b => b.Cast<AtkComponentList>());
        public static unsafe Option<UnmanagedPtr<AtkComponentList>> GetAsListComponent(this UnmanagedPtr<AtkResNode> nodePtr) 
            => nodePtr.SomeWhen(n => n.IsComponent())
            .FlatMap(n => MaybePtr(n.Cast<AtkComponentNode>().Deref().Component))
            .FlatMap(c => c.GetAsListComponent());

        public static unsafe Option<UnmanagedPtr<AtkComponentButton>> GetAsButtonComponent(this UnmanagedPtr<AtkComponentBase> basePtr)
            => basePtr
                .SomeWhen(b => b.ComponentTypeIs(ComponentType.Button))
                .Map(b => b.Cast<AtkComponentButton>());
        public static unsafe Option<UnmanagedPtr<AtkComponentButton>> GetAsButtonComponent(this UnmanagedPtr<AtkResNode> nodePtr)
            => nodePtr.SomeWhen(n => n.IsComponent())
                .FlatMap(n => MaybePtr(n.Cast<AtkComponentNode>().Deref().Component))
                .FlatMap(c => c.GetAsButtonComponent());

        public static unsafe UnmanagedPtr<AtkComponentBase>[] GetChildren(this UnmanagedPtr<AtkComponentList> listComponent)
        {
            // Dalamud.Logging.PluginLog.Log("GetChildren on list component at {addr}", ((IntPtr)listComponent.Ptr).ToString("X"));
            var children = new List<UnmanagedPtr<AtkComponentBase>>();
            for (int i = 0; i < listComponent.Deref().ListLength; i += 1)
            {
                MaybePtr(listComponent.Deref().ItemRendererList[i].AtkComponentListItemRenderer)
                    .Map(p => p.Cast<AtkComponentBase>())
                    .MatchSome(children.Add);

            }
            return children.ToArray();
        }
    }
}