// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;

namespace System.Xml.XPath
{
    internal static class XAttributeExtensions
    {
        public static string GetPrefixOfNamespace(this XAttribute attribute, XNamespace ns)
        {
            string namespaceName = ns.NamespaceName;
            if (namespaceName.Length == 0) return string.Empty;
            if (attribute.GetParent() != null) return ((XElement)attribute.GetParent()).GetPrefixOfNamespace(ns);
            if ((object)namespaceName == (object)XNodeNavigator.xmlPrefixNamespace) return "xml";
            if ((object)namespaceName == (object)XNodeNavigator.xmlnsPrefixNamespace) return "xmlns";
            return null;
        }
    }
}
