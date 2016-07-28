// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Xml.Serialization;
using System.Diagnostics;

namespace System.Xml.Schema
{
    /// <include file='doc\XmlSchemaElement.uex' path='docs/doc[@for="XmlSchemaElement"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlSchemaElement : XmlSchemaParticle
    {
        private bool _isAbstract;
        private bool _hasAbstractAttribute;
        private bool _isNillable;
        private bool _hasNillableAttribute;
        private bool _isLocalTypeDerivationChecked;

        private XmlSchemaDerivationMethod _block = XmlSchemaDerivationMethod.None;
        private XmlSchemaDerivationMethod _final = XmlSchemaDerivationMethod.None;
        private XmlSchemaForm _form = XmlSchemaForm.None;
        private string _defaultValue;
        private string _fixedValue;
        private string _name;

        private XmlQualifiedName _refName = XmlQualifiedName.Empty;
        private XmlQualifiedName _substitutionGroup = XmlQualifiedName.Empty;
        private XmlQualifiedName _typeName = XmlQualifiedName.Empty;
        private XmlSchemaType _type = null;

        private XmlQualifiedName _qualifiedName = XmlQualifiedName.Empty;
        private XmlSchemaType _elementType;
        private XmlSchemaDerivationMethod _blockResolved;
        private XmlSchemaDerivationMethod _finalResolved;
        private XmlSchemaObjectCollection _constraints;
        private SchemaElementDecl _elementDecl;


        /// <include file='doc\XmlSchemaElement.uex' path='docs/doc[@for="XmlSchemaElement.IsAbstract"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlAttribute("abstract"), DefaultValue(false)]
        public bool IsAbstract
        {
            get { return _isAbstract; }
            set
            {
                _isAbstract = value;
                _hasAbstractAttribute = true;
            }
        }

        /// <include file='doc\XmlSchemaElement.uex' path='docs/doc[@for="XmlSchemaElement.Block"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlAttribute("block"), DefaultValue(XmlSchemaDerivationMethod.None)]
        public XmlSchemaDerivationMethod Block
        {
            get { return _block; }
            set { _block = value; }
        }

        /// <include file='doc\XmlSchemaElement.uex' path='docs/doc[@for="XmlSchemaElement.DefaultValue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlAttribute("default")]
        [DefaultValue(null)]
        public string DefaultValue
        {
            get { return _defaultValue; }
            set { _defaultValue = value; }
        }

        /// <include file='doc\XmlSchemaElement.uex' path='docs/doc[@for="XmlSchemaElement.Final"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlAttribute("final"), DefaultValue(XmlSchemaDerivationMethod.None)]
        public XmlSchemaDerivationMethod Final
        {
            get { return _final; }
            set { _final = value; }
        }

        /// <include file='doc\XmlSchemaElement.uex' path='docs/doc[@for="XmlSchemaElement.FixedValue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlAttribute("fixed")]
        [DefaultValue(null)]
        public string FixedValue
        {
            get { return _fixedValue; }
            set { _fixedValue = value; }
        }

        /// <include file='doc\XmlSchemaElement.uex' path='docs/doc[@for="XmlSchemaElement.Form"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlAttribute("form"), DefaultValue(XmlSchemaForm.None)]
        public XmlSchemaForm Form
        {
            get { return _form; }
            set { _form = value; }
        }

        /// <include file='doc\XmlSchemaElement.uex' path='docs/doc[@for="XmlSchemaElement.Name"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlAttribute("name"), DefaultValue("")]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <include file='doc\XmlSchemaElement.uex' path='docs/doc[@for="XmlSchemaElement.IsNillable"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlAttribute("nillable"), DefaultValue(false)]
        public bool IsNillable
        {
            get { return _isNillable; }
            set { _isNillable = value; _hasNillableAttribute = true; }
        }

        [XmlIgnore]
        internal bool HasNillableAttribute
        {
            get { return _hasNillableAttribute; }
        }

        [XmlIgnore]
        internal bool HasAbstractAttribute
        {
            get { return _hasAbstractAttribute; }
        }
        /// <include file='doc\XmlSchemaElement.uex' path='docs/doc[@for="XmlSchemaElement.RefName"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlAttribute("ref")]
        public XmlQualifiedName RefName
        {
            get { return _refName; }
            set { _refName = (value == null ? XmlQualifiedName.Empty : value); }
        }

        /// <include file='doc\XmlSchemaElement.uex' path='docs/doc[@for="XmlSchemaElement.SubstitutionGroup"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlAttribute("substitutionGroup")]
        public XmlQualifiedName SubstitutionGroup
        {
            get { return _substitutionGroup; }
            set { _substitutionGroup = (value == null ? XmlQualifiedName.Empty : value); }
        }

        /// <include file='doc\XmlSchemaElement.uex' path='docs/doc[@for="XmlSchemaElement.SchemaTypeName"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlAttribute("type")]
        public XmlQualifiedName SchemaTypeName
        {
            get { return _typeName; }
            set { _typeName = (value == null ? XmlQualifiedName.Empty : value); }
        }

        /// <include file='doc\XmlSchemaElement.uex' path='docs/doc[@for="XmlSchemaElement.SchemaType"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlElement("complexType", typeof(XmlSchemaComplexType)),
         XmlElement("simpleType", typeof(XmlSchemaSimpleType))]
        public XmlSchemaType SchemaType
        {
            get { return _type; }
            set { _type = value; }
        }

        /// <include file='doc\XmlSchemaElement.uex' path='docs/doc[@for="XmlSchemaElement.Constraints"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlElement("key", typeof(XmlSchemaKey)),
         XmlElement("keyref", typeof(XmlSchemaKeyref)),
         XmlElement("unique", typeof(XmlSchemaUnique))]
        public XmlSchemaObjectCollection Constraints
        {
            get
            {
                if (_constraints == null)
                {
                    _constraints = new XmlSchemaObjectCollection();
                }
                return _constraints;
            }
        }

        /// <include file='doc\XmlSchemaElement.uex' path='docs/doc[@for="XmlSchemaElement.QualifiedName"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlIgnore]
        public XmlQualifiedName QualifiedName
        {
            get { return _qualifiedName; }
        }

        /// <include file='doc\XmlSchemaElement.uex' path='docs/doc[@for="XmlSchemaElement.ElementType"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlIgnore]
        [Obsolete("This property has been deprecated. Please use ElementSchemaType property that returns a strongly typed element type. http://go.microsoft.com/fwlink/?linkid=14202")]
        public object ElementType
        {
            get
            {
                if (_elementType == null)
                    return null;

                if (_elementType.QualifiedName.Namespace == XmlReservedNs.NsXs)
                {
                    return _elementType.Datatype; //returns XmlSchemaDatatype;
                }
                return _elementType;
            }
        }

        /// <include file='doc\XmlSchemaElement.uex' path='docs/doc[@for="XmlSchemaElement.ElementType"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlIgnore]
        public XmlSchemaType ElementSchemaType
        {
            get { return _elementType; }
        }

        /// <include file='doc\XmlSchemaElement.uex' path='docs/doc[@for="XmlSchemaElement.BlockResolved"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlIgnore]
        public XmlSchemaDerivationMethod BlockResolved
        {
            get { return _blockResolved; }
        }

        /// <include file='doc\XmlSchemaElement.uex' path='docs/doc[@for="XmlSchemaElement.FinalResolved"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlIgnore]
        public XmlSchemaDerivationMethod FinalResolved
        {
            get { return _finalResolved; }
        }

        internal XmlReader Validate(XmlReader reader, XmlResolver resolver, XmlSchemaSet schemaSet, ValidationEventHandler valEventHandler)
        {
            if (schemaSet != null)
            {
                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.ValidationType = ValidationType.Schema;
                readerSettings.Schemas = schemaSet;
                readerSettings.ValidationEventHandler += valEventHandler;
                return new XsdValidatingReader(reader, resolver, readerSettings, this);
            }
            return null;
        }

        internal void SetQualifiedName(XmlQualifiedName value)
        {
            _qualifiedName = value;
        }

        internal void SetElementType(XmlSchemaType value)
        {
            _elementType = value;
        }

        internal void SetBlockResolved(XmlSchemaDerivationMethod value)
        {
            _blockResolved = value;
        }

        internal void SetFinalResolved(XmlSchemaDerivationMethod value)
        {
            _finalResolved = value;
        }

        [XmlIgnore]
        internal bool HasDefault
        {
            get { return _defaultValue != null && _defaultValue.Length > 0; }
        }

        internal bool HasConstraints
        {
            get { return _constraints != null && _constraints.Count > 0; }
        }

        internal bool IsLocalTypeDerivationChecked
        {
            get
            {
                return _isLocalTypeDerivationChecked;
            }
            set
            {
                _isLocalTypeDerivationChecked = value;
            }
        }

        internal SchemaElementDecl ElementDecl
        {
            get { return _elementDecl; }
            set { _elementDecl = value; }
        }

        [XmlIgnore]
        internal override string NameAttribute
        {
            get { return Name; }
            set { Name = value; }
        }

        [XmlIgnore]
        internal override string NameString
        {
            get
            {
                return _qualifiedName.ToString();
            }
        }

        internal override XmlSchemaObject Clone()
        {
            System.Diagnostics.Debug.Assert(false, "Should never call Clone() on XmlSchemaElement. Call Clone(XmlSchema) instead.");
            return Clone(null);
        }

        internal XmlSchemaObject Clone(XmlSchema parentSchema)
        {
            XmlSchemaElement newElem = (XmlSchemaElement)MemberwiseClone();

            //Deep clone the QNames as these will be updated on chameleon includes
            newElem._refName = _refName.Clone();
            newElem._substitutionGroup = _substitutionGroup.Clone();
            newElem._typeName = _typeName.Clone();
            newElem._qualifiedName = _qualifiedName.Clone();
            // If this element has a complex type which is anonymous (declared in place with the element)
            //  it needs to be cloned as well, since it may contain named elements and such. And these names
            //  will need to be cloned since they may change their namespace on chameleon includes
            XmlSchemaComplexType complexType = _type as XmlSchemaComplexType;
            if (complexType != null && complexType.QualifiedName.IsEmpty)
            {
                newElem._type = (XmlSchemaType)complexType.Clone(parentSchema);
            }

            //Clear compiled tables
            newElem._constraints = null;
            return newElem;
        }
    }
}

