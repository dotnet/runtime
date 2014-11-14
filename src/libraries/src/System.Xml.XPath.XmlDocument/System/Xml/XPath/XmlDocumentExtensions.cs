// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Xml.XPath
{
    public static class XmlDocumentExtensions
    {
        private class XmlDocumentNavigable : IXPathNavigable
        {
            private XmlNode node;
            public XmlDocumentNavigable(XmlNode n)
            {
                this.node = n;
            }

            public XPathNavigator CreateNavigator()
            {
                return node.CreateNavigator();
            }
        }

        public static XmlNodeList SelectNodes(this XmlNode node, string xpath)
        {
            XPathNavigator navigator = CreateNavigator(node);
            if (navigator == null)
                return null;
            return new XPathNodeList(navigator.Select(xpath));
        }

        public static XmlNodeList SelectNodes(this XmlNode node, string xpath, XmlNamespaceManager nsmgr)
        {
            XPathNavigator navigator = CreateNavigator(node);
            if (navigator == null)
                return null;
            return new XPathNodeList(navigator.Select(xpath, nsmgr));
        }

        public static XmlNode SelectSingleNode(this XmlNode node, string xpath)
        {
            XmlNodeList list = SelectNodes(node, xpath);
            return list != null ? list[0] : null;
        }

        public static XmlNode SelectSingleNode(this XmlNode node, string xpath, XmlNamespaceManager nsmgr)
        {
            XmlNodeList list = SelectNodes(node, xpath, nsmgr);
            return list != null ? list[0] : null;
        }

        //if the method is called on node types like DocType, Entity, XmlDeclaration,
        //the navigator returned is null. So just return null from here for those node types.
        public static XPathNavigator CreateNavigator(this XmlNode node)
        {
            XmlDocument thisAsDoc = node as XmlDocument;
            if (thisAsDoc != null)
            {
                return CreateNavigator(thisAsDoc, node);
            }
            XmlDocument doc = node.OwnerDocument;
            System.Diagnostics.Debug.Assert(doc != null);
            return CreateNavigator(doc, node);
        }

        public static IXPathNavigable ToXPathNavigable(this XmlNode node)
        {
            return new XmlDocumentNavigable(node);
        }

        public static XPathNavigator CreateNavigator(this XmlDocument document)
        {
            return CreateNavigator(document, document);
        }

        public static XPathNavigator CreateNavigator(this XmlDocument document, XmlNode node)
        {
            XmlNodeType nodeType = node.NodeType;
            XmlNode parent;
            XmlNodeType parentType;

            switch (nodeType)
            {
                case XmlNodeType.EntityReference:
                case XmlNodeType.Entity:
                case XmlNodeType.DocumentType:
                case XmlNodeType.Notation:
                case XmlNodeType.XmlDeclaration:
                    return null;
                case XmlNodeType.Text:
                case XmlNodeType.CDATA:
                case XmlNodeType.SignificantWhitespace:
                    parent = node.ParentNode;
                    if (parent != null)
                    {
                        do
                        {
                            parentType = parent.NodeType;
                            if (parentType == XmlNodeType.Attribute)
                            {
                                return null;
                            }
                            else if (parentType == XmlNodeType.EntityReference)
                            {
                                parent = parent.ParentNode;
                            }
                            else
                            {
                                break;
                            }
                        }
                        while (parent != null);
                    }
                    node = NormalizeText(node);
                    break;
                case XmlNodeType.Whitespace:
                    parent = node.ParentNode;
                    if (parent != null)
                    {
                        do
                        {
                            parentType = parent.NodeType;
                            if (parentType == XmlNodeType.Document
                                || parentType == XmlNodeType.Attribute)
                            {
                                return null;
                            }
                            else if (parentType == XmlNodeType.EntityReference)
                            {
                                parent = parent.ParentNode;
                            }
                            else
                            {
                                break;
                            }
                        }
                        while (parent != null);
                    }
                    node = NormalizeText(node);
                    break;
                default:
                    break;
            }
            return new DocumentXPathNavigator(document, node);
        }

        private static XmlNode NormalizeText(XmlNode n)
        {
            XmlNode retnode = null;
            while (n.IsText())
            {
                retnode = n;
                n = n.PreviousSibling;

                if (n == null)
                {
                    XmlNode intnode = retnode;
                    while (true)
                    {
                        if (intnode.ParentNode != null && intnode.ParentNode.NodeType == XmlNodeType.EntityReference)
                        {
                            if (intnode.ParentNode.PreviousSibling != null)
                            {
                                n = intnode.ParentNode.PreviousSibling;
                                break;
                            }
                            else
                            {
                                intnode = intnode.ParentNode;
                                if (intnode == null)
                                    break;
                            }
                        }
                        else
                            break;
                    }
                }

                if (n == null)
                    break;
                while (n.NodeType == XmlNodeType.EntityReference)
                {
                    n = n.LastChild;
                    // The only possibility of this happening is when
                    // XmlEntityReference is not expanded.
                    // All entities are expanded when created in XmlDocument.
                    // If you try to create standalone XmlEntityReference
                    // it is not expanded until you set its parent.
                    Debug.Assert(n != null);
                }
            }
            return retnode;
        }
    }
}
