// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.DataContracts;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace System.Runtime.Serialization
{
    internal static partial class Globals
    {
        /// <SecurityNote>
        /// Review - changes to const could affect code generation logic; any changes should be reviewed.
        /// </SecurityNote>
        internal const BindingFlags ScanAllMembers = BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        internal static XmlQualifiedName IdQualifiedName => field ??= new XmlQualifiedName(Globals.IdLocalName, Globals.SerializationNamespace);
        internal static XmlQualifiedName RefQualifiedName => field ??= new XmlQualifiedName(Globals.RefLocalName, Globals.SerializationNamespace);
        internal static Type TypeOfObject => field ??= typeof(object);
        internal static Type TypeOfValueType => field ??= typeof(ValueType);
        internal static Type TypeOfArray => field ??= typeof(Array);
        internal static Type TypeOfString => field ??= typeof(string);
        internal static Type TypeOfInt => field ??= typeof(int);
        internal static Type TypeOfULong => field ??= typeof(ulong);
        internal static Type TypeOfVoid => field ??= typeof(void);
        internal static Type TypeOfByteArray => field ??= typeof(byte[]);
        internal static Type TypeOfTimeSpan => field ??= typeof(TimeSpan);
        internal static Type TypeOfGuid => field ??= typeof(Guid);
        internal static Type TypeOfDateTimeOffset => field ??= typeof(DateTimeOffset);
        internal static Type TypeOfDateTimeOffsetAdapter => field ??= typeof(DateTimeOffsetAdapter);
        internal static Type TypeOfMemoryStream => field ??= typeof(MemoryStream);
        internal static Type TypeOfMemoryStreamAdapter => field ??= typeof(MemoryStreamAdapter);
        internal static Type TypeOfUri => field ??= typeof(Uri);
        internal static Type TypeOfTypeEnumerable => field ??= typeof(IEnumerable<Type>);
        internal static Type TypeOfStreamingContext => field ??= typeof(StreamingContext);
        internal static Type TypeOfISerializable => field ??= typeof(ISerializable);
        internal static Type TypeOfIDeserializationCallback => field ??= typeof(IDeserializationCallback);
#pragma warning disable SYSLIB0050 // IObjectReference is obsolete
        internal static Type TypeOfIObjectReference => field ??= typeof(IObjectReference);
#pragma warning restore SYSLIB0050
        internal static Type TypeOfXmlFormatClassWriterDelegate => field ??= typeof(XmlFormatClassWriterDelegate);
        internal static Type TypeOfXmlFormatCollectionWriterDelegate => field ??= typeof(XmlFormatCollectionWriterDelegate);
        internal static Type TypeOfXmlFormatClassReaderDelegate => field ??= typeof(XmlFormatClassReaderDelegate);
        internal static Type TypeOfXmlFormatCollectionReaderDelegate => field ??= typeof(XmlFormatCollectionReaderDelegate);
        internal static Type TypeOfXmlFormatGetOnlyCollectionReaderDelegate => field ??= typeof(XmlFormatGetOnlyCollectionReaderDelegate);
        internal static Type TypeOfKnownTypeAttribute => field ??= typeof(KnownTypeAttribute);
        internal static Type TypeOfDataContractAttribute => field ??= typeof(DataContractAttribute);
        internal static Type TypeOfDataMemberAttribute => field ??= typeof(DataMemberAttribute);
        internal static Type TypeOfEnumMemberAttribute => field ??= typeof(EnumMemberAttribute);
        internal static Type TypeOfCollectionDataContractAttribute => field ??= typeof(CollectionDataContractAttribute);
        internal static Type TypeOfOptionalFieldAttribute => field ??= typeof(OptionalFieldAttribute);
        internal static Type TypeOfObjectArray => field ??= typeof(object[]);
        internal static Type TypeOfOnSerializingAttribute => field ??= typeof(OnSerializingAttribute);
        internal static Type TypeOfOnSerializedAttribute => field ??= typeof(OnSerializedAttribute);
        internal static Type TypeOfOnDeserializingAttribute => field ??= typeof(OnDeserializingAttribute);
        internal static Type TypeOfOnDeserializedAttribute => field ??= typeof(OnDeserializedAttribute);
        internal static Type TypeOfFlagsAttribute => field ??= typeof(FlagsAttribute);
        internal static Type TypeOfIXmlSerializable => field ??= typeof(IXmlSerializable);
        internal static Type TypeOfXmlSchemaProviderAttribute => field ??= typeof(XmlSchemaProviderAttribute);
        internal static Type TypeOfXmlRootAttribute => field ??= typeof(XmlRootAttribute);
        internal static Type TypeOfXmlQualifiedName => field ??= typeof(XmlQualifiedName);
        internal static Type TypeOfXmlSchemaType => field ??= typeof(XmlSchemaType);
        internal static Type TypeOfIExtensibleDataObject => field ??= typeof(IExtensibleDataObject);
        internal static Type TypeOfExtensionDataObject => field ??= typeof(ExtensionDataObject);
        internal static Type TypeOfISerializableDataNode => field ??= typeof(ISerializableDataNode);
        internal static Type TypeOfClassDataNode => field ??= typeof(ClassDataNode);
        internal static Type TypeOfCollectionDataNode => field ??= typeof(CollectionDataNode);
        internal static Type TypeOfXmlDataNode => field ??= typeof(XmlDataNode);
        internal static Type TypeOfNullable => field ??= typeof(Nullable<>);
        internal static Type TypeOfReflectionPointer => field ??= typeof(System.Reflection.Pointer);
        internal static Type TypeOfIDictionaryGeneric => field ??= typeof(IDictionary<,>);
        internal static Type TypeOfIDictionary => field ??= typeof(IDictionary);
        internal static Type TypeOfIListGeneric => field ??= typeof(IList<>);
        internal static Type TypeOfIList => field ??= typeof(IList);
        internal static Type TypeOfICollectionGeneric => field ??= typeof(ICollection<>);
        internal static Type TypeOfICollection => field ??= typeof(ICollection);
        internal static Type TypeOfIEnumerableGeneric =>  field ??= typeof(IEnumerable<>);
        internal static Type TypeOfIEnumerable => field ??= typeof(IEnumerable);
        internal static Type TypeOfIEnumeratorGeneric => field ??= typeof(IEnumerator<>);
        internal static Type TypeOfIEnumerator => field ??= typeof(IEnumerator);
        internal static Type TypeOfKeyValuePair => field ??= typeof(KeyValuePair<,>);
        internal static Type TypeOfKeyValue => field ??= typeof(KeyValue<,>);
        internal static Type TypeOfIDictionaryEnumerator => field ??= typeof(IDictionaryEnumerator);
        internal static Type TypeOfDictionaryEnumerator => field ??= typeof(CollectionDataContract.DictionaryEnumerator);
        internal static Type TypeOfGenericDictionaryEnumerator =>  field ??= typeof(CollectionDataContract.GenericDictionaryEnumerator<,>);
        internal static Type TypeOfDictionaryGeneric => field ??= typeof(Dictionary<,>);
        internal static Type TypeOfHashtable
        {
            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get => field ??= TypeOfDictionaryGeneric.MakeGenericType(TypeOfObject, TypeOfObject);
        }

        internal static Type TypeOfXmlElement => field ??= typeof(XmlElement);
        internal static Type TypeOfXmlNodeArray => field ??= typeof(XmlNode[]);
        internal static Type TypeOfDBNull => field ??= typeof(DBNull);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicFields)]
        private static Type? s_typeOfSchemaDefinedType;
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicFields)]
        internal static Type TypeOfSchemaDefinedType =>
            s_typeOfSchemaDefinedType ??= typeof(SchemaDefinedType);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicFields)]
        private static Type? s_typeOfSchemaDefinedEnum;
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicFields)]
        internal static Type TypeOfSchemaDefinedEnum =>
            s_typeOfSchemaDefinedEnum ??= typeof(SchemaDefinedEnum);

        internal static MemberInfo SchemaMemberInfoPlaceholder => field ??= TypeOfSchemaDefinedType.GetField(nameof(SchemaDefinedType._xmlName), BindingFlags.NonPublic | BindingFlags.Instance)!;
        internal static Uri DataContractXsdBaseNamespaceUri => field ??= new Uri(DataContractXsdBaseNamespace);

        public const bool DefaultIsRequired = false;
        public const bool DefaultEmitDefaultValue = true;
        public const int DefaultOrder = 0;
        public const bool DefaultIsReference = false;
        // The value string.Empty aids comparisons (can do simple length checks
        //     instead of string comparison method calls in IL.)
        public static readonly string NewObjectId = string.Empty;
        public const string NullObjectId = null;
        public const string FullSRSInternalsVisiblePattern = @"^[\s]*System\.Runtime\.Serialization[\s]*,[\s]*PublicKey[\s]*=[\s]*(?i:00240000048000009400000006020000002400005253413100040000010001008d56c76f9e8649383049f383c44be0ec204181822a6c31cf5eb7ef486944d032188ea1d3920763712ccb12d75fb77e9811149e6148e5d32fbaab37611c1878ddc19e20ef135d0cb2cff2bfec3d115810c3d9069638fe4be215dbf795861920e5ab6f7db2e2ceef136ac23d5dd2bf031700aec232f6c6b1c785b4305c123b37ab)[\s]*$";
        [GeneratedRegex(FullSRSInternalsVisiblePattern)]
        public static partial Regex FullSRSInternalsVisibleRegex { get; }
        public const char SpaceChar = ' ';
        public const char OpenBracketChar = '[';
        public const char CloseBracketChar = ']';
        public const char CommaChar = ',';
        public const string Space = " ";
        public const string XsiPrefix = "i";
        public const string XsdPrefix = "x";
        public const string SerPrefix = "z";
        public const string SerPrefixForSchema = "ser";
        public const string ElementPrefix = "q";
        public const string DataContractXsdBaseNamespace = "http://schemas.datacontract.org/2004/07/";
        public const string DataContractXmlNamespace = DataContractXsdBaseNamespace + "System.Xml";
        public const string SchemaInstanceNamespace = "http://www.w3.org/2001/XMLSchema-instance";
        public const string SchemaNamespace = "http://www.w3.org/2001/XMLSchema";
        public const string XsiNilLocalName = "nil";
        public const string XsiTypeLocalName = "type";
        public const string TnsPrefix = "tns";
        public const string OccursUnbounded = "unbounded";
        public const string AnyTypeLocalName = "anyType";
        public const string StringLocalName = "string";
        public const string IntLocalName = "int";
        public const string True = "true";
        public const string False = "false";
        public const string ArrayPrefix = "ArrayOf";
        public const string XmlnsNamespace = "http://www.w3.org/2000/xmlns/";
        public const string XmlnsPrefix = "xmlns";
        public const string SchemaLocalName = "schema";
        public const string CollectionsNamespace = "http://schemas.microsoft.com/2003/10/Serialization/Arrays";
        public const string DefaultClrNamespace = "GeneratedNamespace";
        public const string DefaultTypeName = "GeneratedType";
        public const string DefaultGeneratedMember = "GeneratedMember";
        public const string DefaultFieldSuffix = "Field";
        public const string DefaultPropertySuffix = "Property";
        public const string DefaultMemberSuffix = "Member";
        public const string NameProperty = "Name";
        public const string NamespaceProperty = "Namespace";
        public const string OrderProperty = "Order";
        public const string IsReferenceProperty = "IsReference";
        public const string IsRequiredProperty = "IsRequired";
        public const string EmitDefaultValueProperty = "EmitDefaultValue";
        public const string ClrNamespaceProperty = "ClrNamespace";
        public const string ItemNameProperty = "ItemName";
        public const string KeyNameProperty = "KeyName";
        public const string ValueNameProperty = "ValueName";
        public const string SerializationInfoPropertyName = "SerializationInfo";
        public const string SerializationInfoFieldName = "info";
        public const string NodeArrayPropertyName = "Nodes";
        public const string NodeArrayFieldName = "nodesField";
        public const string ExportSchemaMethod = "ExportSchema";
        public const string IsAnyProperty = "IsAny";
        public const string ContextFieldName = "context";
        public const string GetObjectDataMethodName = "GetObjectData";
        public const string GetEnumeratorMethodName = "GetEnumerator";
        public const string MoveNextMethodName = "MoveNext";
        public const string AddValueMethodName = "AddValue";
        public const string CurrentPropertyName = "Current";
        public const string ValueProperty = "Value";
        public const string EnumeratorFieldName = "enumerator";
        public const string SerializationEntryFieldName = "entry";
        public const string ExtensionDataSetMethod = "set_ExtensionData";
        public const string ExtensionDataSetExplicitMethod = "System.Runtime.Serialization.IExtensibleDataObject.set_ExtensionData";
        public const string ExtensionDataObjectPropertyName = "ExtensionData";
        public const string ExtensionDataObjectFieldName = "extensionDataField";
        public const string AddMethodName = "Add";
        public const string GetCurrentMethodName = "get_Current";
        // NOTE: These values are used in schema below. If you modify any value, please make the same change in the schema.
        public const string SerializationNamespace = "http://schemas.microsoft.com/2003/10/Serialization/";
        public const string ClrTypeLocalName = "Type";
        public const string ClrAssemblyLocalName = "Assembly";
        public const string IsValueTypeLocalName = "IsValueType";
        public const string EnumerationValueLocalName = "EnumerationValue";
        public const string SurrogateDataLocalName = "Surrogate";
        public const string GenericTypeLocalName = "GenericType";
        public const string GenericParameterLocalName = "GenericParameter";
        public const string GenericNameAttribute = "Name";
        public const string GenericNamespaceAttribute = "Namespace";
        public const string GenericParameterNestedLevelAttribute = "NestedLevel";
        public const string IsDictionaryLocalName = "IsDictionary";
        public const string ActualTypeLocalName = "ActualType";
        public const string ActualTypeNameAttribute = "Name";
        public const string ActualTypeNamespaceAttribute = "Namespace";
        public const string DefaultValueLocalName = "DefaultValue";
        public const string EmitDefaultValueAttribute = "EmitDefaultValue";
        public const string IdLocalName = "Id";
        public const string RefLocalName = "Ref";
        public const string ArraySizeLocalName = "Size";
        public const string KeyLocalName = "Key";
        public const string ValueLocalName = "Value";
        public const string MscorlibAssemblyName = "0";
        public const string ParseMethodName = "Parse";
        public const string SafeSerializationManagerName = "SafeSerializationManager";
        public const string SafeSerializationManagerNamespace = "http://schemas.datacontract.org/2004/07/System.Runtime.Serialization";
        public const string ISerializableFactoryTypeLocalName = "FactoryType";
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
