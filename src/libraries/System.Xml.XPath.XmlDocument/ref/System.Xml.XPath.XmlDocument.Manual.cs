// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

// These types as extension methods support a better contract factoring between XmlDocument and XPath.

namespace System.Xml
{
    public static partial class XmlDocumentXPathExtensions
    {
        public static System.Xml.XmlNodeList SelectNodes(this XmlNode node, string xpath) { return default(System.Xml.XmlNodeList); }
        public static System.Xml.XmlNodeList SelectNodes(this XmlNode node, string xpath, System.Xml.XmlNamespaceManager nsmgr) { return default(System.Xml.XmlNodeList); }
        public static System.Xml.XmlNode SelectSingleNode(this XmlNode node, string xpath) { return default(System.Xml.XmlNode); }
        public static System.Xml.XmlNode SelectSingleNode(this XmlNode node, string xpath, System.Xml.XmlNamespaceManager nsmgr) { return default(System.Xml.XmlNode); }
        public static System.Xml.XPath.XPathNavigator CreateNavigator(this XmlNode node) { return default(System.Xml.XPath.XPathNavigator); }
        public static System.Xml.XPath.IXPathNavigable ToXPathNavigable(this XmlNode node) { return default(System.Xml.XPath.IXPathNavigable); }

        public static System.Xml.XPath.XPathNavigator CreateNavigator(this XmlDocument document) { return default(System.Xml.XPath.XPathNavigator); }
        public static System.Xml.XPath.XPathNavigator CreateNavigator(this XmlDocument document, System.Xml.XmlNode node) { return default(System.Xml.XPath.XPathNavigator); }
    }
}
