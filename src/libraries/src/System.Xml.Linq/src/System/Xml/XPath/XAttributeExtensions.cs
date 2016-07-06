// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
