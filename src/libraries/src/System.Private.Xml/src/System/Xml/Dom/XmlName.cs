// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml
{
    using System.Text;
    using System.Diagnostics;
    using System.Xml.Schema;

    internal class XmlName : IXmlSchemaInfo
    {
        private string _prefix;
        private string _localName;
        private string _ns;
        private string _name;
        private int _hashCode;
        internal XmlDocument ownerDoc;
        internal XmlName next;

        public static XmlName Create(string prefix, string localName, string ns, int hashCode, XmlDocument ownerDoc, XmlName next, IXmlSchemaInfo schemaInfo)
        {
            if (schemaInfo == null)
            {
                return new XmlName(prefix, localName, ns, hashCode, ownerDoc, next);
            }
            else
            {
                return new XmlNameEx(prefix, localName, ns, hashCode, ownerDoc, next, schemaInfo);
            }
        }

        internal XmlName(string prefix, string localName, string ns, int hashCode, XmlDocument ownerDoc, XmlName next)
        {
            _prefix = prefix;
            _localName = localName;
            _ns = ns;
            _name = null;
            _hashCode = hashCode;
            this.ownerDoc = ownerDoc;
            this.next = next;
        }

        public string LocalName
        {
            get
            {
                return _localName;
            }
        }

        public string NamespaceURI
        {
            get
            {
                return _ns;
            }
        }

        public string Prefix
        {
            get
            {
                return _prefix;
            }
        }

        public int HashCode
        {
            get
            {
                return _hashCode;
            }
        }

        public XmlDocument OwnerDocument
        {
            get
            {
                return ownerDoc;
            }
        }

        public string Name
        {
            get
            {
                if (_name == null)
                {
                    Debug.Assert(_prefix != null);
                    if (_prefix.Length > 0)
                    {
                        if (_localName.Length > 0)
                        {
                            string n = string.Concat(_prefix, ":", _localName);
                            lock (ownerDoc.NameTable)
                            {
                                if (_name == null)
                                {
                                    _name = ownerDoc.NameTable.Add(n);
                                }
                            }
                        }
                        else
                        {
                            _name = _prefix;
                        }
                    }
                    else
                    {
                        _name = _localName;
                    }
                    Debug.Assert(Ref.Equal(_name, ownerDoc.NameTable.Get(_name)));
                }
                return _name;
            }
        }

        public virtual XmlSchemaValidity Validity
        {
            get
            {
                return XmlSchemaValidity.NotKnown;
            }
        }

        public virtual bool IsDefault
        {
            get
            {
                return false;
            }
        }

        public virtual bool IsNil
        {
            get
            {
                return false;
            }
        }

        public virtual XmlSchemaSimpleType MemberType
        {
            get
            {
                return null;
            }
        }

        public virtual XmlSchemaType SchemaType
        {
            get
            {
                return null;
            }
        }

        public virtual XmlSchemaElement SchemaElement
        {
            get
            {
                return null;
            }
        }

        public virtual XmlSchemaAttribute SchemaAttribute
        {
            get
            {
                return null;
            }
        }

        public virtual bool Equals(IXmlSchemaInfo schemaInfo)
        {
            return schemaInfo == null;
        }

        public static int GetHashCode(string name)
        {
            int hashCode = 0;
            if (name != null)
            {
                unchecked
                {
                    for (int i = name.Length - 1; i >= 0; i--)
                    {
                        char ch = name[i];
                        if (ch == ':') break;
                        hashCode += (hashCode << 7) ^ ch;
                    }
                    hashCode -= hashCode >> 17;
                    hashCode -= hashCode >> 11;
                    hashCode -= hashCode >> 5;
                }
            }
            return hashCode;
        }
    }

    internal sealed class XmlNameEx : XmlName
    {
        private byte _flags;
        private XmlSchemaSimpleType _memberType;
        private XmlSchemaType _schemaType;
        private object _decl;

        // flags
        // 0,1  : Validity
        // 2    : IsDefault
        // 3    : IsNil
        private const byte ValidityMask = 0x03;
        private const byte IsDefaultBit = 0x04;
        private const byte IsNilBit = 0x08;

        internal XmlNameEx(string prefix, string localName, string ns, int hashCode, XmlDocument ownerDoc, XmlName next, IXmlSchemaInfo schemaInfo) : base(prefix, localName, ns, hashCode, ownerDoc, next)
        {
            SetValidity(schemaInfo.Validity);
            SetIsDefault(schemaInfo.IsDefault);
            SetIsNil(schemaInfo.IsNil);
            _memberType = schemaInfo.MemberType;
            _schemaType = schemaInfo.SchemaType;
            _decl = schemaInfo.SchemaElement != null
                   ? (object)schemaInfo.SchemaElement
                   : (object)schemaInfo.SchemaAttribute;
        }

        public override XmlSchemaValidity Validity
        {
            get
            {
                return ownerDoc.CanReportValidity ? (XmlSchemaValidity)(_flags & ValidityMask) : XmlSchemaValidity.NotKnown;
            }
        }

        public override bool IsDefault
        {
            get
            {
                return (_flags & IsDefaultBit) != 0;
            }
        }

        public override bool IsNil
        {
            get
            {
                return (_flags & IsNilBit) != 0;
            }
        }

        public override XmlSchemaSimpleType MemberType
        {
            get
            {
                return _memberType;
            }
        }

        public override XmlSchemaType SchemaType
        {
            get
            {
                return _schemaType;
            }
        }

        public override XmlSchemaElement SchemaElement
        {
            get
            {
                return _decl as XmlSchemaElement;
            }
        }

        public override XmlSchemaAttribute SchemaAttribute
        {
            get
            {
                return _decl as XmlSchemaAttribute;
            }
        }

        public void SetValidity(XmlSchemaValidity value)
        {
            _flags = (byte)((_flags & ~ValidityMask) | (byte)(value));
        }

        public void SetIsDefault(bool value)
        {
            if (value) _flags = (byte)(_flags | IsDefaultBit);
            else _flags = (byte)(_flags & ~IsDefaultBit);
        }

        public void SetIsNil(bool value)
        {
            if (value) _flags = (byte)(_flags | IsNilBit);
            else _flags = (byte)(_flags & ~IsNilBit);
        }

        public override bool Equals(IXmlSchemaInfo schemaInfo)
        {
            if (schemaInfo != null
                && schemaInfo.Validity == (XmlSchemaValidity)(_flags & ValidityMask)
                && schemaInfo.IsDefault == ((_flags & IsDefaultBit) != 0)
                && schemaInfo.IsNil == ((_flags & IsNilBit) != 0)
                && (object)schemaInfo.MemberType == (object)_memberType
                && (object)schemaInfo.SchemaType == (object)_schemaType
                && (object)schemaInfo.SchemaElement == (object)(_decl as XmlSchemaElement)
                && (object)schemaInfo.SchemaAttribute == (object)(_decl as XmlSchemaAttribute))
            {
                return true;
            }
            return false;
        }
    }
}
