// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml
{
    public static partial class XmlDocumentXPathExtensions
    {
        public static System.Xml.XmlNodeList SelectNodes(this XmlNode node, string xpath)
        {
            return node.SelectNodes(xpath);
        }

        public static System.Xml.XmlNodeList SelectNodes(this XmlNode node, string xpath, System.Xml.XmlNamespaceManager nsmgr)
        {
            return node.SelectNodes(xpath, nsmgr);
        }

        public static System.Xml.XmlNode SelectSingleNode(this XmlNode node, string xpath)
        {
            return node.SelectSingleNode(xpath);
        }

        public static System.Xml.XmlNode SelectSingleNode(this XmlNode node, string xpath, System.Xml.XmlNamespaceManager nsmgr)
        {
            return node.SelectSingleNode(xpath, nsmgr);
        }

        public static System.Xml.XPath.XPathNavigator CreateNavigator(this XmlNode node)
        {
            return node.CreateNavigator();
        }

        public static System.Xml.XPath.IXPathNavigable ToXPathNavigable(this XmlNode node)
        {
            return node.ToXPathNavigable();
        }

        public static System.Xml.XPath.XPathNavigator CreateNavigator(this XmlDocument document)
        {
            return document.CreateNavigator();
        }

        public static System.Xml.XPath.XPathNavigator CreateNavigator(this XmlDocument document, System.Xml.XmlNode node)
        {
            return document.CreateNavigator(node);
        }
    }
}
