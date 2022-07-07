// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace System.Runtime.Serialization.Schema
{
    internal static class Globals
    {
        internal const string SerializerTrimmerWarning = "Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the " +
            "required types are preserved.";

        public const string ActualTypeLocalName = "ActualType";
        public const string ActualTypeNameAttribute = "Name";
        public const string ActualTypeNamespaceAttribute = "Namespace";
        public const string AddValueMethodName = "AddValue";
        public const string AnyTypeLocalName = "anyType";
        public const string ArrayPrefix = "ArrayOf";
        public const string ClrNamespaceProperty = "ClrNamespace";
        public const string CollectionsNamespace = "http://schemas.microsoft.com/2003/10/Serialization/Arrays";
        public const string ContextFieldName = "context";
        public const string CurrentPropertyName = "Current";
        public const string DataContractXsdBaseNamespace = "http://schemas.datacontract.org/2004/07/";
        public const string DefaultClrNamespace = "GeneratedNamespace";
        public const bool   DefaultEmitDefaultValue = true;
        public const string DefaultGeneratedMember = "GeneratedMember";
        public const string DefaultFieldSuffix = "Field";
        public const bool   DefaultIsReference = false;
        public const bool   DefaultIsRequired = false;
        public const string DefaultMemberSuffix = "Member";
        public const int    DefaultOrder = 0;
        public const string DefaultTypeName = "GeneratedType";
        public const string DefaultValueLocalName = "DefaultValue";
        public const string EmitDefaultValueAttribute = "EmitDefaultValue";
        public const string EmitDefaultValueProperty = "EmitDefaultValue";
        public const string EnumerationValueLocalName = "EnumerationValue";
        public const string EnumeratorFieldName = "enumerator";
        public const string ExportSchemaMethod = "ExportSchema";
        public const string ExtensionDataObjectFieldName = "extensionDataField";
        public const string ExtensionDataObjectPropertyName = "ExtensionData";
        public const string False = "false";
        public const string GenericNameAttribute = "Name";
        public const string GenericNamespaceAttribute = "Namespace";
        public const string GenericParameterLocalName = "GenericParameter";
        public const string GenericParameterNestedLevelAttribute = "NestedLevel";
        public const string GenericTypeLocalName = "GenericType";
        public const string GetEnumeratorMethodName = "GetEnumerator";
        public const string GetObjectDataMethodName = "GetObjectData";
        public const string IdLocalName = "Id";
        public const string IntLocalName = "int";
        public const string IsAnyProperty = "IsAny";
        public const string IsDictionaryLocalName = "IsDictionary";
        public const string ISerializableFactoryTypeLocalName = "FactoryType";
        public const string IsReferenceProperty = "IsReference";
        public const string IsRequiredProperty = "IsRequired";
        public const string IsValueTypeLocalName = "IsValueType";
        public const string ItemNameProperty = "ItemName";
        public const string KeyLocalName = "Key";
        public const string KeyNameProperty = "KeyName";
        public const string MoveNextMethodName = "MoveNext";
        public const string NameProperty = "Name";
        public const string NamespaceProperty = "Namespace";
        public const string NodeArrayFieldName = "nodesField";
        public const string NodeArrayPropertyName = "Nodes";
        public const string OccursUnbounded = "unbounded";
        public const string OrderProperty = "Order";
        public const string RefLocalName = "Ref";
        public const string SchemaInstanceNamespace = "http://www.w3.org/2001/XMLSchema-instance";
        public const string SchemaLocalName = "schema";
        public const string SchemaNamespace = "http://www.w3.org/2001/XMLSchema";
        public const string SerializationEntryFieldName = "entry";
        public const string SerializationInfoFieldName = "info";
        public const string SerializationInfoPropertyName = "SerializationInfo";
        public const string SerializationNamespace = "http://schemas.microsoft.com/2003/10/Serialization/";
        public const string SerPrefixForSchema = "ser";
        public const string StringLocalName = "string";
        public const string SurrogateDataLocalName = "Surrogate";
        public const string TnsPrefix = "tns";
        public const string True = "true";
        public const string ValueLocalName = "Value";
        public const string ValueNameProperty = "ValueName";
        public const string ValueProperty = "Value";

        private static Uri? s_dataContractXsdBaseNamespaceUri;
        internal static Uri DataContractXsdBaseNamespaceUri => s_dataContractXsdBaseNamespaceUri ??= new Uri(DataContractXsdBaseNamespace);

        private static XmlQualifiedName? s_idQualifiedName;
        internal static XmlQualifiedName IdQualifiedName => s_idQualifiedName ??= new XmlQualifiedName(Globals.IdLocalName, Globals.SerializationNamespace);

        private static XmlQualifiedName? s_refQualifiedName;
        internal static XmlQualifiedName RefQualifiedName => s_refQualifiedName ??= new XmlQualifiedName(Globals.RefLocalName, Globals.SerializationNamespace);

        private static Type? s_typeOfXmlElement;
        internal static Type TypeOfXmlElement => s_typeOfXmlElement ??= typeof(XmlElement);

        private static Type? s_typeOfXmlNodeArray;
        internal static Type TypeOfXmlNodeArray => s_typeOfXmlNodeArray ??= typeof(XmlNode[]);

        private static Type? s_typeOfXmlQualifiedName;
        internal static Type TypeOfXmlQualifiedName => s_typeOfXmlQualifiedName ??= typeof(XmlQualifiedName);

        private static Type? s_typeOfXmlSchemaProviderAttribute;
        internal static Type TypeOfXmlSchemaProviderAttribute => s_typeOfXmlSchemaProviderAttribute ??= typeof(XmlSchemaProviderAttribute);

        private static Type? s_typeOfXmlSchemaType;
        internal static Type TypeOfXmlSchemaType => s_typeOfXmlSchemaType ??= typeof(XmlSchemaType);


        public const string SerializationSchema = @"<?xml version='1.0' encoding='utf-8'?>
<xs:schema elementFormDefault='qualified' attributeFormDefault='qualified' xmlns:tns='http://schemas.microsoft.com/2003/10/Serialization/' targetNamespace='http://schemas.microsoft.com/2003/10/Serialization/' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
  <xs:element name='anyType' nillable='true' type='xs:anyType' />
  <xs:element name='anyURI' nillable='true' type='xs:anyURI' />
  <xs:element name='base64Binary' nillable='true' type='xs:base64Binary' />
  <xs:element name='boolean' nillable='true' type='xs:boolean' />
  <xs:element name='byte' nillable='true' type='xs:byte' />
  <xs:element name='dateTime' nillable='true' type='xs:dateTime' />
  <xs:element name='decimal' nillable='true' type='xs:decimal' />
  <xs:element name='double' nillable='true' type='xs:double' />
  <xs:element name='float' nillable='true' type='xs:float' />
  <xs:element name='int' nillable='true' type='xs:int' />
  <xs:element name='long' nillable='true' type='xs:long' />
  <xs:element name='QName' nillable='true' type='xs:QName' />
  <xs:element name='short' nillable='true' type='xs:short' />
  <xs:element name='string' nillable='true' type='xs:string' />
  <xs:element name='unsignedByte' nillable='true' type='xs:unsignedByte' />
  <xs:element name='unsignedInt' nillable='true' type='xs:unsignedInt' />
  <xs:element name='unsignedLong' nillable='true' type='xs:unsignedLong' />
  <xs:element name='unsignedShort' nillable='true' type='xs:unsignedShort' />
  <xs:element name='char' nillable='true' type='tns:char' />
  <xs:simpleType name='char'>
    <xs:restriction base='xs:int'/>
  </xs:simpleType>
  <xs:element name='duration' nillable='true' type='tns:duration' />
  <xs:simpleType name='duration'>
    <xs:restriction base='xs:duration'>
      <xs:pattern value='\-?P(\d*D)?(T(\d*H)?(\d*M)?(\d*(\.\d*)?S)?)?' />
      <xs:minInclusive value='-P10675199DT2H48M5.4775808S' />
      <xs:maxInclusive value='P10675199DT2H48M5.4775807S' />
    </xs:restriction>
  </xs:simpleType>
  <xs:element name='guid' nillable='true' type='tns:guid' />
  <xs:simpleType name='guid'>
    <xs:restriction base='xs:string'>
      <xs:pattern value='[\da-fA-F]{8}-[\da-fA-F]{4}-[\da-fA-F]{4}-[\da-fA-F]{4}-[\da-fA-F]{12}' />
    </xs:restriction>
  </xs:simpleType>
  <xs:attribute name='FactoryType' type='xs:QName' />
  <xs:attribute name='Id' type='xs:ID' />
  <xs:attribute name='Ref' type='xs:IDREF' />
</xs:schema>
";
    }
}
