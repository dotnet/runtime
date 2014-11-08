// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace System.Xml
{
    internal static class XmlDocumentEx
    {
        // The only constructors of XmlAttribute are internal and protected so let's use protected attribute to access it
        private class FakeXmlAttribute : XmlAttribute
        {
            public FakeXmlAttribute(string prefix, string localName, string namespaceURI, XmlDocument doc)
                : base(prefix, localName, namespaceURI, doc)
            { }
        }
        private static Dictionary<XmlDocument, XmlAttribute> namespaceXmlOfDocument = new Dictionary<XmlDocument, XmlAttribute>();
        public static XmlAttribute GetNamespaceXml(this XmlDocument xmlDocument)
        {
            XmlAttribute ret;
            if (!namespaceXmlOfDocument.TryGetValue(xmlDocument, out ret))
            {
                ret = new FakeXmlAttribute(XmlConst.NsXmlNs, XmlConst.NsXml, XmlConst.ReservedNsXmlNs, xmlDocument);
                ret.Value = XmlConst.ReservedNsXml;
                namespaceXmlOfDocument[xmlDocument] = ret;
            }
            return ret;
        }
    }
}
