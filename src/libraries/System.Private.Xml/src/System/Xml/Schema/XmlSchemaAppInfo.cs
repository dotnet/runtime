// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Xml.Serialization;

namespace System.Xml.Schema
{
    public class XmlSchemaAppInfo : XmlSchemaObject
    {
        private string? _source;
        private XmlNode?[]? _markup;

        [XmlAttribute("source", DataType = "anyURI")]
        public string? Source
        {
            get { return _source; }
            set { _source = value; }
        }

        [XmlText, XmlAnyElement]
        public XmlNode?[]? Markup
        {
            get { return _markup; }
            set { _markup = value; }
        }
    }
}
