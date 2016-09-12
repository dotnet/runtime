// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System.Collections;
    using System.Xml.Serialization;

    /// <include file='doc\XmlSchemaAttributeGroup.uex' path='docs/doc[@for="XmlSchemaAttributeGroup"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlSchemaAttributeGroup : XmlSchemaAnnotated
    {
        private string _name;
        private XmlSchemaObjectCollection _attributes = new XmlSchemaObjectCollection();
        private XmlSchemaAnyAttribute _anyAttribute;
        private XmlQualifiedName _qname = XmlQualifiedName.Empty;
        private XmlSchemaAttributeGroup _redefined;
        private XmlSchemaObjectTable _attributeUses;
        private XmlSchemaAnyAttribute _attributeWildcard;
        private int _selfReferenceCount;

        /// <include file='doc\XmlSchemaAttributeGroup.uex' path='docs/doc[@for="XmlSchemaAttributeGroup.Name"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlAttribute("name")]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <include file='doc\XmlSchemaAttributeGroup.uex' path='docs/doc[@for="XmlSchemaAttributeGroup.Attributes"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlElement("attribute", typeof(XmlSchemaAttribute)),
         XmlElement("attributeGroup", typeof(XmlSchemaAttributeGroupRef))]
        public XmlSchemaObjectCollection Attributes
        {
            get { return _attributes; }
        }

        /// <include file='doc\XmlSchemaAttributeGroup.uex' path='docs/doc[@for="XmlSchemaAttributeGroup.AnyAttribute"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlElement("anyAttribute")]
        public XmlSchemaAnyAttribute AnyAttribute
        {
            get { return _anyAttribute; }
            set { _anyAttribute = value; }
        }

        [XmlIgnore]
        public XmlQualifiedName QualifiedName
        {
            get { return _qname; }
        }

        [XmlIgnore]
        internal XmlSchemaObjectTable AttributeUses
        {
            get
            {
                if (_attributeUses == null)
                {
                    _attributeUses = new XmlSchemaObjectTable();
                }
                return _attributeUses;
            }
        }

        [XmlIgnore]
        internal XmlSchemaAnyAttribute AttributeWildcard
        {
            get { return _attributeWildcard; }
            set { _attributeWildcard = value; }
        }

        /// <include file='doc\XmlSchemaAttributeGroup.uex' path='docs/doc[@for="XmlSchemaAttributeGroup.RedefinedAttributeGroup"]/*' />
        [XmlIgnore]
        public XmlSchemaAttributeGroup RedefinedAttributeGroup
        {
            get { return _redefined; }
        }

        [XmlIgnore]
        internal XmlSchemaAttributeGroup Redefined
        {
            get { return _redefined; }
            set { _redefined = value; }
        }

        [XmlIgnore]
        internal int SelfReferenceCount
        {
            get { return _selfReferenceCount; }
            set { _selfReferenceCount = value; }
        }

        [XmlIgnore]
        internal override string NameAttribute
        {
            get { return Name; }
            set { Name = value; }
        }

        internal void SetQualifiedName(XmlQualifiedName value)
        {
            _qname = value;
        }

        internal override XmlSchemaObject Clone()
        {
            XmlSchemaAttributeGroup newGroup = (XmlSchemaAttributeGroup)MemberwiseClone();
            if (XmlSchemaComplexType.HasAttributeQNameRef(_attributes))
            { //If a ref/type name is present
                newGroup._attributes = XmlSchemaComplexType.CloneAttributes(_attributes);

                //Clear compiled tables
                newGroup._attributeUses = null;
            }
            return newGroup;
        }
    }
}
