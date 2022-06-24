using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Collections.Generic;

namespace DomanMahjongStatus
{
    public static class GUINodeExtra
    {
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

        public static unsafe AtkResNode* GetChildWithId(AtkComponentNode* node, int childId)
        {
            if (node != null)
            {
                return GetChildWithId(node->Component, childId);
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
    }
}