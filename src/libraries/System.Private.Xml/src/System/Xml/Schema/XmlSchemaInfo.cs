// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;
using System.Collections;

namespace System.Xml.Schema
{
    public class XmlSchemaInfo : IXmlSchemaInfo
    {
        private bool _isDefault;
        private bool _isNil;
        private XmlSchemaElement _schemaElement;
        private XmlSchemaAttribute _schemaAttribute;
        private XmlSchemaType _schemaType;
        private XmlSchemaSimpleType _memberType;
        private XmlSchemaValidity _validity;
        private XmlSchemaContentType _contentType;

        public XmlSchemaInfo()
        {
            Clear();
        }

        internal XmlSchemaInfo(XmlSchemaValidity validity) : this()
        {
            _validity = validity;
        }

        public XmlSchemaValidity Validity
        {
            get
            {
                return _validity;
            }
            set
            {
                _validity = value;
            }
        }

        public bool IsDefault
        {
            get
            {
                return _isDefault;
            }
            set
            {
                _isDefault = value;
            }
        }

        public bool IsNil
        {
            get
            {
                return _isNil;
            }
            set
            {
                _isNil = value;
            }
        }

        public XmlSchemaSimpleType MemberType
        {
            get
            {
                return _memberType;
            }
            set
            {
                _memberType = value;
            }
        }

        public XmlSchemaType SchemaType
        {
            get
            {
                return _schemaType;
            }
            set
            {
                _schemaType = value;
                if (_schemaType != null)
                { //Member type will not change its content type
                    _contentType = _schemaType.SchemaContentType;
                }
                else
                {
                    _contentType = XmlSchemaContentType.Empty;
                }
            }
        }

        public XmlSchemaElement SchemaElement
        {
            get
            {
                return _schemaElement;
            }
            set
            {
                _schemaElement = value;
                if (value != null)
                { //Setting non-null SchemaElement means SchemaAttribute should be null
                    _schemaAttribute = null;
                }
            }
        }

        public XmlSchemaAttribute SchemaAttribute
        {
            get
            {
                return _schemaAttribute;
            }
            set
            {
                _schemaAttribute = value;
                if (value != null)
                { //Setting non-null SchemaAttribute means SchemaElement should be null
                    _schemaElement = null;
                }
            }
        }

        public XmlSchemaContentType ContentType
        {
            get
            {
                return _contentType;
            }
            set
            {
                _contentType = value;
            }
        }

        internal XmlSchemaType XmlType
        {
            get
            {
                if (_memberType != null)
                {
                    return _memberType;
                }
                return _schemaType;
            }
        }

        internal bool HasDefaultValue
        {
            get
            {
                return _schemaElement != null && _schemaElement.ElementDecl.DefaultValueTyped != null;
            }
        }

        internal bool IsUnionType
        {
            get
            {
                if (_schemaType == null || _schemaType.Datatype == null)
                {
                    return false;
                }
                return _schemaType.Datatype.Variety == XmlSchemaDatatypeVariety.Union;
            }
        }

        internal void Clear()
        {
            _isNil = false;
            _isDefault = false;
            _schemaType = null;
            _schemaElement = null;
            _schemaAttribute = null;
            _memberType = null;
            _validity = XmlSchemaValidity.NotKnown;
            _contentType = XmlSchemaContentType.Empty;
        }
    }
}
