// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System.Xml.Serialization;
    using System.Diagnostics;

    public class XmlSchemaSimpleType : XmlSchemaType
    {
        private XmlSchemaSimpleTypeContent _content;

        public XmlSchemaSimpleType()
        {
            Debug.Assert(SchemaContentType == XmlSchemaContentType.TextOnly);
        }

        [XmlElement("restriction", typeof(XmlSchemaSimpleTypeRestriction)),
        XmlElement("list", typeof(XmlSchemaSimpleTypeList)),
        XmlElement("union", typeof(XmlSchemaSimpleTypeUnion))]
        public XmlSchemaSimpleTypeContent Content
        {
            get { return _content; }
            set { _content = value; }
        }

        internal override XmlQualifiedName DerivedFrom
        {
            get
            {
                if (_content == null)
                {
                    // type derived from anyType
                    return XmlQualifiedName.Empty;
                }
                if (_content is XmlSchemaSimpleTypeRestriction)
                {
                    return ((XmlSchemaSimpleTypeRestriction)_content).BaseTypeName;
                }
                return XmlQualifiedName.Empty;
            }
        }

        internal override XmlSchemaObject Clone()
        {
            XmlSchemaSimpleType newSimpleType = (XmlSchemaSimpleType)MemberwiseClone();
            if (_content != null)
            {
                newSimpleType.Content = (XmlSchemaSimpleTypeContent)_content.Clone();
            }
            return newSimpleType;
        }
    }
}

