// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System.Collections;
    using System.ComponentModel;
    using System.Xml.Serialization;

    public class XmlSchemaDocumentation : XmlSchemaObject
    {
        private string _source;
        private string _language;
        private XmlNode[] _markup;
        private static XmlSchemaSimpleType s_languageType = DatatypeImplementation.GetSimpleTypeFromXsdType(new XmlQualifiedName("language", XmlReservedNs.NsXs));

        [XmlAttribute("source", DataType = "anyURI")]
        public string Source
        {
            get { return _source; }
            set { _source = value; }
        }

        [XmlAttribute("xml:lang")]
        public string Language
        {
            get { return _language; }
            set { _language = (string)s_languageType.Datatype.ParseValue(value, (XmlNameTable)null, (IXmlNamespaceResolver)null); }
        }

        [XmlText(), XmlAnyElement]
        public XmlNode[] Markup
        {
            get { return _markup; }
            set { _markup = value; }
        }
    }
}
