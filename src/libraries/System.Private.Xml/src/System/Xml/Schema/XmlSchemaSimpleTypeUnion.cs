// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System.Xml.Serialization;

    public class XmlSchemaSimpleTypeUnion : XmlSchemaSimpleTypeContent
    {
        private XmlSchemaObjectCollection _baseTypes = new XmlSchemaObjectCollection();
        private XmlQualifiedName[] _memberTypes;
        private XmlSchemaSimpleType[] _baseMemberTypes; // Compiled

        [XmlElement("simpleType", typeof(XmlSchemaSimpleType))]
        public XmlSchemaObjectCollection BaseTypes
        {
            get { return _baseTypes; }
        }

        [XmlAttribute("memberTypes")]
        public XmlQualifiedName[] MemberTypes
        {
            get { return _memberTypes; }
            set { _memberTypes = value; }
        }

        //Compiled Information
        [XmlIgnore]
        public XmlSchemaSimpleType[] BaseMemberTypes
        {
            get { return _baseMemberTypes; }
        }

        internal void SetBaseMemberTypes(XmlSchemaSimpleType[] baseMemberTypes)
        {
            _baseMemberTypes = baseMemberTypes;
        }

        internal override XmlSchemaObject Clone()
        {
            if (_memberTypes != null && _memberTypes.Length > 0)
            { //Only if the union has MemberTypes defined
                XmlSchemaSimpleTypeUnion newUnion = (XmlSchemaSimpleTypeUnion)MemberwiseClone();
                XmlQualifiedName[] newQNames = new XmlQualifiedName[_memberTypes.Length];

                for (int i = 0; i < _memberTypes.Length; i++)
                {
                    newQNames[i] = _memberTypes[i].Clone();
                }
                newUnion.MemberTypes = newQNames;
                return newUnion;
            }
            return this;
        }
    }
}

