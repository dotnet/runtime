// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml.XPath;

namespace XPathTests.Common
{
    public class XPathResultToken
    {
        public XPathNodeType NodeType { get; set; }
        public string BaseURI { get; set; }
        public bool HasChildren { get; set; }
        public bool HasAttributes { get; set; }
        public bool IsEmptyElement { get; set; }
        public string LocalName { get; set; }
        public string Name { get; set; }
        public string NamespaceURI { get; set; }
        public bool HasNameTable { get; set; }
        public string Prefix { get; set; }
        public string Value { get; set; }
        public string XmlLang { get; set; }

        public XPathResultToken()
        {
            BaseURI = String.Empty;
            LocalName = String.Empty;
            Name = String.Empty;
            NamespaceURI = String.Empty;
            Prefix = String.Empty;
            Value = String.Empty;
            XmlLang = String.Empty;
        }
    }
}
