// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

namespace System.Xml.Schema
{
    public class XmlSchemaAttribute : XmlSchemaAnnotated
    {
        private string? _defaultValue;
        private string? _fixedValue;
        private string? _name;

        private XmlSchemaForm _form = XmlSchemaForm.None;
        private XmlSchemaUse _use = XmlSchemaUse.None;

        private XmlQualifiedName _refName = XmlQualifiedName.Empty;
        private XmlQualifiedName _typeName = XmlQualifiedName.Empty;
        private XmlQualifiedName _qualifiedName = XmlQualifiedName.Empty;

        private XmlSchemaSimpleType? _type;
        private XmlSchemaSimpleType? _attributeType;

        private SchemaAttDef? _attDef;

        [XmlAttribute("default")]
        [DefaultValue(null)]
        public string? DefaultValue
        {
            get { return _defaultValue; }
            set { _defaultValue = value; }
        }

        [XmlAttribute("fixed")]
        [DefaultValue(null)]
        public string? FixedValue
        {
            get { return _fixedValue; }
            set { _fixedValue = value; }
        }

        [XmlAttribute("form"), DefaultValue(XmlSchemaForm.None)]
        public XmlSchemaForm Form
        {
            get { return _form; }
            set { _form = value; }
        }

        [XmlAttribute("name")]
        public string? Name
        {
            get { return _name; }
            set { _name = value; }
        }

        [XmlAttribute("ref")]
        public XmlQualifiedName RefName
        {
            get { return _refName; }
            set { _refName = (value == null ? XmlQualifiedName.Empty : value); }
        }

        [XmlAttribute("type")]
        public XmlQualifiedName SchemaTypeName
        {
            get { return _typeName; }
            set { _typeName = (value == null ? XmlQualifiedName.Empty : value); }
        }

        [XmlElement("simpleType")]
        public XmlSchemaSimpleType? SchemaType
        {
            get { return _type; }
            set { _type = value; }
        }

        [XmlAttribute("use"), DefaultValue(XmlSchemaUse.None)]
        public XmlSchemaUse Use
        {
            get { return _use; }
            set { _use = value; }
        }

        [XmlIgnore]
        public XmlQualifiedName QualifiedName
        {
            get { return _qualifiedName; }
        }

        [XmlIgnore]
        [Obsolete("This property has been deprecated. Please use AttributeSchemaType property that returns a strongly typed attribute type. https://go.microsoft.com/fwlink/?linkid=14202")]
        public object? AttributeType
        {
            get
            {
                if (_attributeType == null)
                    return null;

                if (_attributeType.QualifiedName.Namespace == XmlReservedNs.NsXs)
                {
                    return _attributeType.Datatype;
                }
                return _attributeType;
            }
        }

        [XmlIgnore]
        public XmlSchemaSimpleType? AttributeSchemaType
        {
            get { return _attributeType; }
        }

        [return: NotNullIfNotNull("schemaSet")]
        internal XmlReader? Validate(XmlReader reader, XmlResolver? resolver, XmlSchemaSet schemaSet, ValidationEventHandler valEventHandler)
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

        [XmlIgnore]
        internal XmlSchemaDatatype? Datatype
        {
            get
            {
                if (_attributeType != null)
                {
                    return _attributeType.Datatype;
                }
                return null;
            }
        }

        internal void SetQualifiedName(XmlQualifiedName value)
        {
            _qualifiedName = value;
        }

        internal void SetAttributeType(XmlSchemaSimpleType? value)
        {
            _attributeType = value;
        }

        internal SchemaAttDef? AttDef
        {
            get { return _attDef; }
            set { _attDef = value; }
        }

        internal bool HasDefault
        {
            get { return _defaultValue != null; }
        }

        [XmlIgnore]
        internal override string? NameAttribute
        {
            get { return Name; }
            set { Name = value; }
        }

        internal override XmlSchemaObject Clone()
        {
            XmlSchemaAttribute newAtt = (XmlSchemaAttribute)MemberwiseClone();

            //Deep clone the QNames as these will be updated on chameleon includes
            newAtt._refName = _refName.Clone();
            newAtt._typeName = _typeName.Clone();
            newAtt._qualifiedName = _qualifiedName.Clone();
            return newAtt;
        }
    }
}
