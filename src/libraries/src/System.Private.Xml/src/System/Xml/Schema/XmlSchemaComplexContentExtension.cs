// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System.Collections;
    using System.Xml.Serialization;

    /// <include file='doc\XmlSchemaComplexContentExtension.uex' path='docs/doc[@for="XmlSchemaComplexContentExtension"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlSchemaComplexContentExtension : XmlSchemaContent
    {
        private XmlSchemaParticle _particle;
        private XmlSchemaObjectCollection _attributes = new XmlSchemaObjectCollection();
        private XmlSchemaAnyAttribute _anyAttribute;
        private XmlQualifiedName _baseTypeName = XmlQualifiedName.Empty;

        /// <include file='doc\XmlSchemaComplexContentExtension.uex' path='docs/doc[@for="XmlSchemaComplexContentExtension.BaseTypeName"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlAttribute("base")]
        public XmlQualifiedName BaseTypeName
        {
            get { return _baseTypeName; }
            set { _baseTypeName = (value == null ? XmlQualifiedName.Empty : value); }
        }

        /// <include file='doc\XmlSchemaComplexContentExtension.uex' path='docs/doc[@for="XmlSchemaComplexContentExtension.Particle"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlElement("group", typeof(XmlSchemaGroupRef)),
         XmlElement("choice", typeof(XmlSchemaChoice)),
         XmlElement("all", typeof(XmlSchemaAll)),
         XmlElement("sequence", typeof(XmlSchemaSequence))]
        public XmlSchemaParticle Particle
        {
            get { return _particle; }
            set { _particle = value; }
        }

        /// <include file='doc\XmlSchemaComplexContentExtension.uex' path='docs/doc[@for="XmlSchemaComplexContentExtension.Attributes"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlElement("attribute", typeof(XmlSchemaAttribute)),
         XmlElement("attributeGroup", typeof(XmlSchemaAttributeGroupRef))]
        public XmlSchemaObjectCollection Attributes
        {
            get { return _attributes; }
        }


        /// <include file='doc\XmlSchemaComplexContentExtension.uex' path='docs/doc[@for="XmlSchemaComplexContentExtension.AnyAttribute"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlElement("anyAttribute")]
        public XmlSchemaAnyAttribute AnyAttribute
        {
            get { return _anyAttribute; }
            set { _anyAttribute = value; }
        }

        internal void SetAttributes(XmlSchemaObjectCollection newAttributes)
        {
            _attributes = newAttributes;
        }
    }
}

