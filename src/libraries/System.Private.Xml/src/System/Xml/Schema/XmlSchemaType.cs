// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

namespace System.Xml.Schema
{
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlSchemaType : XmlSchemaAnnotated
    {
        private string? _name;
        private XmlSchemaDerivationMethod _final = XmlSchemaDerivationMethod.None;
        private XmlSchemaDerivationMethod _derivedBy;
        private XmlSchemaType? _baseSchemaType;
        private XmlSchemaDatatype? _datatype;
        private XmlSchemaDerivationMethod _finalResolved;
        private volatile SchemaElementDecl? _elementDecl;
        private volatile XmlQualifiedName _qname = XmlQualifiedName.Empty;
        private XmlSchemaType? _redefined;

        //compiled information
        private XmlSchemaContentType _contentType;

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static XmlSchemaSimpleType? GetBuiltInSimpleType(XmlQualifiedName qualifiedName)
        {
            ArgumentNullException.ThrowIfNull(qualifiedName);

            return DatatypeImplementation.GetSimpleTypeFromXsdType(qualifiedName);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static XmlSchemaSimpleType GetBuiltInSimpleType(XmlTypeCode typeCode)
        {
            return DatatypeImplementation.GetSimpleTypeFromTypeCode(typeCode);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static XmlSchemaComplexType? GetBuiltInComplexType(XmlTypeCode typeCode)
        {
            if (typeCode == XmlTypeCode.Item)
            {
                return XmlSchemaComplexType.AnyType;
            }

            return null;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static XmlSchemaComplexType? GetBuiltInComplexType(XmlQualifiedName qualifiedName)
        {
            ArgumentNullException.ThrowIfNull(qualifiedName);

            if (qualifiedName.Equals(XmlSchemaComplexType.AnyType.QualifiedName))
            {
                return XmlSchemaComplexType.AnyType;
            }

            if (qualifiedName.Equals(XmlSchemaComplexType.UntypedAnyType.QualifiedName))
            {
                return XmlSchemaComplexType.UntypedAnyType;
            }

            return null;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlAttribute("name")]
        public string? Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlAttribute("final"), DefaultValue(XmlSchemaDerivationMethod.None)]
        public XmlSchemaDerivationMethod Final
        {
            get { return _final; }
            set { _final = value; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlIgnore]
        public XmlQualifiedName QualifiedName
        {
            get { return _qname; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlIgnore]
        public XmlSchemaDerivationMethod FinalResolved
        {
            get { return _finalResolved; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlIgnore]
        [Obsolete("XmlSchemaType.BaseSchemaType has been deprecated. Use the BaseXmlSchemaType property that returns a strongly typed base schema type instead.")]
        public object? BaseSchemaType
        {
            get
            {
                if (_baseSchemaType == null)
                    return null;

                if (_baseSchemaType.QualifiedName.Namespace == XmlReservedNs.NsXs)
                {
                    return _baseSchemaType.Datatype;
                }

                return _baseSchemaType;
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlIgnore]
        public XmlSchemaType? BaseXmlSchemaType
        {
            get { return _baseSchemaType; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlIgnore]
        public XmlSchemaDerivationMethod DerivedBy
        {
            get { return _derivedBy; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlIgnore]
        public XmlSchemaDatatype? Datatype
        {
            get { return _datatype; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlIgnore]
        public virtual bool IsMixed
        {
            get { return false; }
            set { }
        }

        [XmlIgnore]
        public XmlTypeCode TypeCode
        {
            get
            {
                if (this == XmlSchemaComplexType.AnyType)
                {
                    return XmlTypeCode.Item;
                }
                if (_datatype == null)
                {
                    return XmlTypeCode.None;
                }
                return _datatype.TypeCode;
            }
        }

        [XmlIgnore]
        internal XmlValueConverter ValueConverter
        {
            get
            {
                if (_datatype == null)
                { //Default converter
                    return XmlUntypedConverter.Untyped;
                }
                return _datatype.ValueConverter;
            }
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

        internal XmlSchemaContentType SchemaContentType
        {
            get
            {
                return _contentType;
            }
        }

        internal void SetQualifiedName(XmlQualifiedName value)
        {
            _qname = value;
        }

        internal void SetFinalResolved(XmlSchemaDerivationMethod value)
        {
            _finalResolved = value;
        }

        internal void SetBaseSchemaType(XmlSchemaType? value)
        {
            _baseSchemaType = value;
        }

        internal void SetDerivedBy(XmlSchemaDerivationMethod value)
        {
            _derivedBy = value;
        }

        internal void SetDatatype(XmlSchemaDatatype? value)
        {
            _datatype = value;
        }

        internal SchemaElementDecl? ElementDecl
        {
            get { return _elementDecl; }
            set { _elementDecl = value; }
        }

        [XmlIgnore]
        internal XmlSchemaType? Redefined
        {
            get { return _redefined; }
            set { _redefined = value; }
        }

        internal virtual XmlQualifiedName DerivedFrom
        {
            get { return XmlQualifiedName.Empty; }
        }

        internal void SetContentType(XmlSchemaContentType value)
        {
            _contentType = value;
        }

        public static bool IsDerivedFrom([NotNullWhen(true)] XmlSchemaType? derivedType, [NotNullWhen(true)] XmlSchemaType? baseType, XmlSchemaDerivationMethod except)
        {
            if (derivedType == null || baseType == null)
            {
                return false;
            }

            if (derivedType == baseType)
            {
                return true;
            }

            if (baseType == XmlSchemaComplexType.AnyType)
            { //Not checking for restriction blocked since all types are implicitly derived by restriction from xs:anyType
                return true;
            }

            do
            {
                XmlSchemaSimpleType? dt = derivedType as XmlSchemaSimpleType;
                XmlSchemaSimpleType? bt = baseType as XmlSchemaSimpleType;
                if (bt != null && dt != null)
                { //SimpleTypes
                    if (bt == DatatypeImplementation.AnySimpleType)
                    { //Not checking block=restriction
                        return true;
                    }

                    if ((except & derivedType.DerivedBy) != 0 || !dt.Datatype!.IsDerivedFrom(bt.Datatype!))
                    {
                        return false;
                    }
                    return true;
                }
                else
                { //Complex types
                    if ((except & derivedType.DerivedBy) != 0)
                    {
                        return false;
                    }

                    derivedType = derivedType.BaseXmlSchemaType;
                    if (derivedType == baseType)
                    {
                        return true;
                    }
                }
            } while (derivedType != null);

            return false;
        }


        internal static bool IsDerivedFromDatatype(XmlSchemaDatatype derivedDataType, XmlSchemaDatatype baseDataType, XmlSchemaDerivationMethod except)
        {
            if (DatatypeImplementation.AnySimpleType.Datatype == baseDataType)
            {
                return true;
            }
            return derivedDataType.IsDerivedFrom(baseDataType);
        }

        [XmlIgnore]
        internal override string? NameAttribute
        {
            get { return Name; }
            set { Name = value; }
        }
    }
}
