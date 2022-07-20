// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Serialization;

namespace System.Xml.Schema
{
    public class XmlSchemaRedefine : XmlSchemaExternal
    {
        private readonly XmlSchemaObjectCollection _items = new XmlSchemaObjectCollection();
        private readonly XmlSchemaObjectTable _attributeGroups = new XmlSchemaObjectTable();
        private readonly XmlSchemaObjectTable _types = new XmlSchemaObjectTable();
        private readonly XmlSchemaObjectTable _groups = new XmlSchemaObjectTable();

        public XmlSchemaRedefine()
        {
            Compositor = Compositor.Redefine;
        }

        [XmlElement("annotation", typeof(XmlSchemaAnnotation)),
         XmlElement("attributeGroup", typeof(XmlSchemaAttributeGroup)),
         XmlElement("complexType", typeof(XmlSchemaComplexType)),
         XmlElement("group", typeof(XmlSchemaGroup)),
         XmlElement("simpleType", typeof(XmlSchemaSimpleType))]
        public XmlSchemaObjectCollection Items
        {
            get { return _items; }
        }

        [XmlIgnore]
        public XmlSchemaObjectTable AttributeGroups
        {
            get { return _attributeGroups; }
        }

        [XmlIgnore]
        public XmlSchemaObjectTable SchemaTypes
        {
            get { return _types; }
        }

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
