// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Xml.XPath
{
    internal static class XPathNavigatorEx
    {
        // Get mask that will allow XPathNodeType content matching to be performed using only a shift and an and operation
        private const int AllMask = 0x7FFFFFFF;
        private const int NoAttrNmspMask = AllMask & ~(1 << (int)XPathNodeType.Attribute) & ~(1 << (int)XPathNodeType.Namespace);
        private const int TextMask = (1 << (int)XPathNodeType.Text) | (1 << (int)XPathNodeType.SignificantWhitespace) | (1 << (int)XPathNodeType.Whitespace);
        private static readonly int[] _ContentKindMasks = {
            (1 << (int) XPathNodeType.Root),                        // Root
            (1 << (int) XPathNodeType.Element),                     // Element
            0,                                                      // Attribute (not content)
            0,                                                      // Namespace (not content)
            TextMask,                                               // Text
            (1 << (int) XPathNodeType.SignificantWhitespace),       // SignificantWhitespace
            (1 << (int) XPathNodeType.Whitespace),                  // Whitespace
            (1 << (int) XPathNodeType.ProcessingInstruction),       // ProcessingInstruction
            (1 << (int) XPathNodeType.Comment),                     // Comment
            NoAttrNmspMask,                                         // All
        };

        public static int GetContentKindMask(XPathNodeType type)
        {
            return _ContentKindMasks[(int)type];
        }

        public static int GetKindMask(XPathNodeType type)
        {
            if (type == XPathNodeType.All)
                return AllMask;
            else if (type == XPathNodeType.Text)
                return TextMask;

            return (1 << (int)type);
        }

        public static bool MoveToNonDescendant(this XPathNavigator xpNavigator)
        {
            // If current node is document, there is no next non-descendant
            if (xpNavigator.NodeType == XPathNodeType.Root)
                return false;

            // If sibling exists, it is the next non-descendant
            if (xpNavigator.MoveToNext())
                return true;

            // The current node is either an attribute, namespace, or last child node
            XPathNavigator navSave = xpNavigator.Clone();

            if (!xpNavigator.MoveToParent())
                return false;

            switch (navSave.NodeType)
            {
                case XPathNodeType.Attribute:
                case XPathNodeType.Namespace:
                    // Next node in document order is first content-child of parent
                    if (xpNavigator.MoveToFirstChild())
                        return true;
                    break;
            }

            while (!xpNavigator.MoveToNext())
            {
                if (!xpNavigator.MoveToParent())
                {
                    // Restore original position and return false
                    xpNavigator.MoveTo(navSave);
                    return false;
                }
            }

            return true;
        }
    }
}
