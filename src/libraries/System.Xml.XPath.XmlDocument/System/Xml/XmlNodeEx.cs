// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Xml.XPath;

namespace System.Xml
{
    internal static class XmlNodeEx
    {
        public static String GetXmlLang(this XmlNode xmlNode)
        {
            XmlNode node = xmlNode;
            XmlElement elem = null;
            do
            {
                elem = node as XmlElement;
                if (elem != null)
                {
                    if (elem.HasAttribute(XmlConst.AttrXmlLang))
                        return elem.GetAttribute(XmlConst.AttrXmlLang);
                }
                node = node.ParentNode;
            } while (node != null);
            return String.Empty;
        }

        public static bool IsText(this XmlNode xmlNode)
        {
            return IsTextNode(xmlNode.NodeType);
        }

        // source: XmlDocument's internals
        private static bool IsTextNode(XmlNodeType nt)
        {
            switch (nt)
            {
                case XmlNodeType.Text:
                case XmlNodeType.CDATA:
                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                    return true;
                default:
                    return false;
            }
        }

        public static bool DecideXPNodeTypeForTextNodes(this XmlNode thisObj, XmlNode node, ref XPathNodeType xnt)
        {
            //returns true - if all siblings of the node are processed else returns false.
            //The reference XPathNodeType argument being passed in is the watermark that
            //changes according to the siblings nodetype and will contain the correct
            //nodetype when it returns.

            Debug.Assert(IsTextNode(node.NodeType) || (node.ParentNode != null && node.ParentNode.NodeType == XmlNodeType.EntityReference));
            while (node != null)
            {
                switch (node.NodeType)
                {
                    case XmlNodeType.Whitespace:
                        break;
                    case XmlNodeType.SignificantWhitespace:
                        xnt = XPathNodeType.SignificantWhitespace;
                        break;
                    case XmlNodeType.Text:
                    case XmlNodeType.CDATA:
                        xnt = XPathNodeType.Text;
                        return false;
                    case XmlNodeType.EntityReference:
                        if (!thisObj.DecideXPNodeTypeForTextNodes(node.FirstChild, ref xnt))
                        {
                            return false;
                        }
                        break;
                    default:
                        return false;
                }
                node = node.NextSibling;
            }
            return true;
        }

        public static XPathNodeType GetXPNodeType(this XmlNode node)
        {
            // Simulating behavior of deleted virtual property XmlNode.XPNodeType by using the fact
            // that each XmlNodeType has different NodeType property value
            switch (node.NodeType)
            {
                case XmlNodeType.Attribute:
                    {
                        XmlAttribute xmlAttribute = node as XmlAttribute;
                        if (xmlAttribute != null)
                        {
                            return xmlAttribute.IsNamespace() ? XPathNodeType.Namespace : XPathNodeType.Attribute;
                        }
                        else
                        {
                            goto default;
                        }
                    }
                case XmlNodeType.CDATA:
                case XmlNodeType.Text:
                    return XPathNodeType.Text;
                case XmlNodeType.Comment:
                    return XPathNodeType.Comment;
                case XmlNodeType.Document:
                case XmlNodeType.DocumentFragment:
                    return XPathNodeType.Root;
                case XmlNodeType.Element:
                    return XPathNodeType.Element;
                case XmlNodeType.ProcessingInstruction:
                    return XPathNodeType.ProcessingInstruction;
                case XmlNodeType.SignificantWhitespace:
                    {
                        XPathNodeType xnt = XPathNodeType.SignificantWhitespace;
                        node.DecideXPNodeTypeForTextNodes(node, ref xnt);
                        return xnt;
                    }
                case XmlNodeType.Whitespace:
                    {
                        XPathNodeType xnt = XPathNodeType.Whitespace;
                        node.DecideXPNodeTypeForTextNodes(node, ref xnt);
                        return xnt;
                    }
                default:
                    return (XPathNodeType)(-1);
            }
        }
    }
}
