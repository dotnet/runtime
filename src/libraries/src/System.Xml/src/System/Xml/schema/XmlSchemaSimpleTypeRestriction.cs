// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System.Collections;
    using System.Xml.Serialization;

    /// <include file='doc\XmlSchemaSimpleTypeRestriction.uex' path='docs/doc[@for="XmlSchemaSimpleTypeRestriction"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlSchemaSimpleTypeRestriction : XmlSchemaSimpleTypeContent
    {
        private XmlQualifiedName _baseTypeName = XmlQualifiedName.Empty;
        private XmlSchemaSimpleType _baseType;
        private XmlSchemaObjectCollection _facets = new XmlSchemaObjectCollection();

        /// <include file='doc\XmlSchemaSimpleTypeRestriction.uex' path='docs/doc[@for="XmlSchemaSimpleTypeRestriction.BaseTypeName"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlAttribute("base")]
        public XmlQualifiedName BaseTypeName
        {
            get { return _baseTypeName; }
            set { _baseTypeName = (value == null ? XmlQualifiedName.Empty : value); }
        }

        /// <include file='doc\XmlSchemaSimpleTypeRestriction.uex' path='docs/doc[@for="XmlSchemaSimpleTypeRestriction.BaseType"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlElement("simpleType", typeof(XmlSchemaSimpleType))]
        public XmlSchemaSimpleType BaseType
        {
            get { return _baseType; }
            set { _baseType = value; }
        }

        /// <include file='doc\XmlSchemaSimpleTypeRestriction.uex' path='docs/doc[@for="XmlSchemaSimpleTypeRestriction.Facets"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlElement("length", typeof(XmlSchemaLengthFacet)),
         XmlElement("minLength", typeof(XmlSchemaMinLengthFacet)),
         XmlElement("maxLength", typeof(XmlSchemaMaxLengthFacet)),
         XmlElement("pattern", typeof(XmlSchemaPatternFacet)),
         XmlElement("enumeration", typeof(XmlSchemaEnumerationFacet)),
         XmlElement("maxInclusive", typeof(XmlSchemaMaxInclusiveFacet)),
         XmlElement("maxExclusive", typeof(XmlSchemaMaxExclusiveFacet)),
         XmlElement("minInclusive", typeof(XmlSchemaMinInclusiveFacet)),
         XmlElement("minExclusive", typeof(XmlSchemaMinExclusiveFacet)),
         XmlElement("totalDigits", typeof(XmlSchemaTotalDigitsFacet)),
         XmlElement("fractionDigits", typeof(XmlSchemaFractionDigitsFacet)),
         XmlElement("whiteSpace", typeof(XmlSchemaWhiteSpaceFacet))]
        public XmlSchemaObjectCollection Facets
        {
            get { return _facets; }
        }

        internal override XmlSchemaObject Clone()
        {
            XmlSchemaSimpleTypeRestriction newRestriction = (XmlSchemaSimpleTypeRestriction)MemberwiseClone();
            newRestriction.BaseTypeName = _baseTypeName.Clone();
            return newRestriction;
        }
    }
}

