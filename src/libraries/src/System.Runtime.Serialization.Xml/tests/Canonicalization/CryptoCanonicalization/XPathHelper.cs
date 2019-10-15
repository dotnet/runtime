// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;

namespace System.Runtime.Serialization.Xml.Canonicalization.Tests
{
    internal static class XPathHelper
    {
        public static XmlElement FindElement(XmlNode node, string elementName)
        {
            if (node.Name == elementName && node is XmlElement)
            {
                return node as XmlElement;
            }

            for (XmlNode child = node.FirstChild; child != null; child = child.NextSibling)
            {
                XmlElement result = FindElement(child, elementName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        public static SimpleXmlNodeList GetAllDescendents(XmlNode root)
        {
            SimpleXmlNodeList bfsQueue = new SimpleXmlNodeList();
            bfsQueue.Add(root);
            for (int i = 0; i < bfsQueue.Count; i++)
            {
                XmlNode node = bfsQueue[i];
                for (XmlNode child = node.FirstChild; child != null; child = child.NextSibling)
                {
                    bfsQueue.Add(child);
                }

                AddAttributes(bfsQueue, node, false);
            }

            return bfsQueue;
        }

        public static SimpleXmlNodeList CreateSubtreeNodeList(XmlNode root, string elementToStartAt)
        {
            XmlElement inclusionRoot = FindElement(root, elementToStartAt);
            if (inclusionRoot == null)
            {
                throw new ArgumentException("elementToStartAt", elementToStartAt + " not found in document");
            }

            SimpleXmlNodeList list = GetAllDescendents(inclusionRoot);
            for (XmlNode node = inclusionRoot.ParentNode; node != null; node = node.ParentNode)
            {
                AddAttributes(list, node, true);
            }

            return list;
        }

        private static void AddAttributes(SimpleXmlNodeList list, XmlNode node, bool onlyNamespaces)
        {
            XmlAttributeCollection attributes = node.Attributes;
            if (attributes != null)
            {
                for (int i = 0; i < attributes.Count; i++)
                {
                    XmlAttribute a = attributes[i];
                    if (!onlyNamespaces || IsNamespaceNode(a))
                    {
                        list.Add(a);
                    }
                }
            }
        }

        private static bool IsNamespaceNode(XmlAttribute node)
        {
            return node.Prefix == "xmlns" || (node.Prefix.Length == 0 && node.LocalName == "xmlns");
        }
    }
}
