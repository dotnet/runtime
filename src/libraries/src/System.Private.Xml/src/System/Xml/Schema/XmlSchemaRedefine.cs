// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System.Xml.Serialization;

    /// <include file='doc\XmlSchemaRedefine.uex' path='docs/doc[@for="XmlSchemaRedefine"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlSchemaRedefine : XmlSchemaExternal
    {
        private XmlSchemaObjectCollection _items = new XmlSchemaObjectCollection();
        private XmlSchemaObjectTable _attributeGroups = new XmlSchemaObjectTable();
        private XmlSchemaObjectTable _types = new XmlSchemaObjectTable();
        private XmlSchemaObjectTable _groups = new XmlSchemaObjectTable();


        /// <include file='doc\XmlSchemaRedefine.uex' path='docs/doc[@for="XmlSchemaRedefine.XmlSchemaRedefine"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSchemaRedefine()
        {
            Compositor = Compositor.Redefine;
        }

        /// <include file='doc\XmlSchemaRedefine.uex' path='docs/doc[@for="XmlSchemaRedefine.Items"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlElement("annotation", typeof(XmlSchemaAnnotation)),
         XmlElement("attributeGroup", typeof(XmlSchemaAttributeGroup)),
         XmlElement("complexType", typeof(XmlSchemaComplexType)),
         XmlElement("group", typeof(XmlSchemaGroup)),
         XmlElement("simpleType", typeof(XmlSchemaSimpleType))]
        public XmlSchemaObjectCollection Items
        {
            get { return _items; }
        }

        /// <include file='doc\XmlSchemaRedefine.uex' path='docs/doc[@for="XmlSchemaRedefine.AttributeGroups"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlIgnore]
        public XmlSchemaObjectTable AttributeGroups
        {
            get { return _attributeGroups; }
        }

        /// <include file='doc\XmlSchemaRedefine.uex' path='docs/doc[@for="XmlSchemaRedefine.SchemaTypes"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlIgnore]
        public XmlSchemaObjectTable SchemaTypes
        {
            get { return _types; }
        }

        /// <include file='doc\XmlSchemaRedefine.uex' path='docs/doc[@for="XmlSchemaRedefine.Groups"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlIgnore]
        public XmlSchemaObjectTable Groups
        {
            get { return _groups; }
        }

        internal override void AddAnnotation(XmlSchemaAnnotation annotation)
        {
            _items.Add(annotation);
        }
    }
}
