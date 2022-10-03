// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
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

        private static XmlQualifiedName? s_idQualifiedName;
        internal static XmlQualifiedName IdQualifiedName =>
            s_idQualifiedName ??= new XmlQualifiedName(Globals.IdLocalName, Globals.SerializationNamespace);

        private static XmlQualifiedName? s_refQualifiedName;
        internal static XmlQualifiedName RefQualifiedName =>
            s_refQualifiedName ??= new XmlQualifiedName(Globals.RefLocalName, Globals.SerializationNamespace);

        private static Type? s_typeOfObject;
        internal static Type TypeOfObject =>
            s_typeOfObject ??= typeof(object);

        private static Type? s_typeOfValueType;
        internal static Type TypeOfValueType =>
            s_typeOfValueType ??= typeof(ValueType);

        private static Type? s_typeOfArray;
        internal static Type TypeOfArray =>
            s_typeOfArray ??= typeof(Array);

        private static Type? s_typeOfString;
        internal static Type TypeOfString =>
            s_typeOfString ??= typeof(string);

        private static Type? s_typeOfInt;
        internal static Type TypeOfInt =>
            s_typeOfInt ??= typeof(int);

        private static Type? s_typeOfULong;
        internal static Type TypeOfULong =>
            s_typeOfULong ??= typeof(ulong);

        private static Type? s_typeOfVoid;
        internal static Type TypeOfVoid =>
            s_typeOfVoid ??= typeof(void);

        private static Type? s_typeOfByteArray;
        internal static Type TypeOfByteArray =>
            s_typeOfByteArray ??= typeof(byte[]);

        private static Type? s_typeOfTimeSpan;
        internal static Type TypeOfTimeSpan =>
            s_typeOfTimeSpan ??= typeof(TimeSpan);

        private static Type? s_typeOfGuid;
        internal static Type TypeOfGuid =>
            s_typeOfGuid ??= typeof(Guid);

        private static Type? s_typeOfDateTimeOffset;
        internal static Type TypeOfDateTimeOffset =>
            s_typeOfDateTimeOffset ??= typeof(DateTimeOffset);

        private static Type? s_typeOfDateTimeOffsetAdapter;
        internal static Type TypeOfDateTimeOffsetAdapter =>
            s_typeOfDateTimeOffsetAdapter ??= typeof(DateTimeOffsetAdapter);

        private static Type? s_typeOfMemoryStream;
        internal static Type TypeOfMemoryStream =>
            s_typeOfMemoryStream ??= typeof(MemoryStream);

        private static Type? s_typeOfMemoryStreamAdapter;
        internal static Type TypeOfMemoryStreamAdapter =>
            s_typeOfMemoryStreamAdapter ??= typeof(MemoryStreamAdapter);

        private static Type? s_typeOfUri;
        internal static Type TypeOfUri =>
            s_typeOfUri ??= typeof(Uri);

        private static Type? s_typeOfTypeEnumerable;
        internal static Type TypeOfTypeEnumerable =>
            s_typeOfTypeEnumerable ??= typeof(IEnumerable<Type>);

        private static Type? s_typeOfStreamingContext;
        internal static Type TypeOfStreamingContext =>
            s_typeOfStreamingContext ??= typeof(StreamingContext);

        private static Type? s_typeOfISerializable;
        internal static Type TypeOfISerializable =>
            s_typeOfISerializable ??= typeof(ISerializable);

        private static Type? s_typeOfIDeserializationCallback;
        internal static Type TypeOfIDeserializationCallback =>
            s_typeOfIDeserializationCallback ??= typeof(IDeserializationCallback);

        private static Type? s_typeOfIObjectReference;
        internal static Type TypeOfIObjectReference =>
            s_typeOfIObjectReference ??= typeof(IObjectReference);

        private static Type? s_typeOfXmlFormatClassWriterDelegate;
        internal static Type TypeOfXmlFormatClassWriterDelegate =>
            s_typeOfXmlFormatClassWriterDelegate ??= typeof(XmlFormatClassWriterDelegate);

        private static Type? s_typeOfXmlFormatCollectionWriterDelegate;
        internal static Type TypeOfXmlFormatCollectionWriterDelegate =>
            s_typeOfXmlFormatCollectionWriterDelegate ??= typeof(XmlFormatCollectionWriterDelegate);

        private static Type? s_typeOfXmlFormatClassReaderDelegate;
        internal static Type TypeOfXmlFormatClassReaderDelegate =>
            s_typeOfXmlFormatClassReaderDelegate ??= typeof(XmlFormatClassReaderDelegate);

        private static Type? s_typeOfXmlFormatCollectionReaderDelegate;
        internal static Type TypeOfXmlFormatCollectionReaderDelegate =>
            s_typeOfXmlFormatCollectionReaderDelegate ??= typeof(XmlFormatCollectionReaderDelegate);

        private static Type? s_typeOfXmlFormatGetOnlyCollectionReaderDelegate;
        internal static Type TypeOfXmlFormatGetOnlyCollectionReaderDelegate =>
            s_typeOfXmlFormatGetOnlyCollectionReaderDelegate ??= typeof(XmlFormatGetOnlyCollectionReaderDelegate);

        private static Type? s_typeOfKnownTypeAttribute;
        internal static Type TypeOfKnownTypeAttribute =>
            s_typeOfKnownTypeAttribute ??= typeof(KnownTypeAttribute);

        private static Type? s_typeOfDataContractAttribute;
        internal static Type TypeOfDataContractAttribute =>
            s_typeOfDataContractAttribute ??= typeof(DataContractAttribute);

        private static Type? s_typeOfDataMemberAttribute;
        internal static Type TypeOfDataMemberAttribute =>
            s_typeOfDataMemberAttribute ??= typeof(DataMemberAttribute);

        private static Type? s_typeOfEnumMemberAttribute;
        internal static Type TypeOfEnumMemberAttribute =>
            s_typeOfEnumMemberAttribute ??= typeof(EnumMemberAttribute);

        private static Type? s_typeOfCollectionDataContractAttribute;
        internal static Type TypeOfCollectionDataContractAttribute =>
            s_typeOfCollectionDataContractAttribute ??= typeof(CollectionDataContractAttribute);

        private static Type? s_typeOfOptionalFieldAttribute;
        internal static Type TypeOfOptionalFieldAttribute =>
            s_typeOfOptionalFieldAttribute ??= typeof(OptionalFieldAttribute);

        private static Type? s_typeOfObjectArray;
        internal static Type TypeOfObjectArray =>
            s_typeOfObjectArray ??= typeof(object[]);

        private static Type? s_typeOfOnSerializingAttribute;
        internal static Type TypeOfOnSerializingAttribute =>
            s_typeOfOnSerializingAttribute ??= typeof(OnSerializingAttribute);

        private static Type? s_typeOfOnSerializedAttribute;
        internal static Type TypeOfOnSerializedAttribute =>
            s_typeOfOnSerializedAttribute ??= typeof(OnSerializedAttribute);

        private static Type? s_typeOfOnDeserializingAttribute;
        internal static Type TypeOfOnDeserializingAttribute =>
            s_typeOfOnDeserializingAttribute ??= typeof(OnDeserializingAttribute);

        private static Type? s_typeOfOnDeserializedAttribute;
        internal static Type TypeOfOnDeserializedAttribute =>
            s_typeOfOnDeserializedAttribute ??= typeof(OnDeserializedAttribute);

        private static Type? s_typeOfFlagsAttribute;
        internal static Type TypeOfFlagsAttribute =>
            s_typeOfFlagsAttribute ??= typeof(FlagsAttribute);

        private static Type? s_typeOfIXmlSerializable;
        internal static Type TypeOfIXmlSerializable =>
            s_typeOfIXmlSerializable ??= typeof(IXmlSerializable);

        private static Type? s_typeOfXmlSchemaProviderAttribute;
        internal static Type TypeOfXmlSchemaProviderAttribute =>
            s_typeOfXmlSchemaProviderAttribute ??= typeof(XmlSchemaProviderAttribute);

        private static Type? s_typeOfXmlRootAttribute;
        internal static Type TypeOfXmlRootAttribute =>
            s_typeOfXmlRootAttribute ??= typeof(XmlRootAttribute);

        private static Type? s_typeOfXmlQualifiedName;
        internal static Type TypeOfXmlQualifiedName =>
            s_typeOfXmlQualifiedName ??= typeof(XmlQualifiedName);

        private static Type? s_typeOfXmlSchemaType;
        internal static Type TypeOfXmlSchemaType =>
            s_typeOfXmlSchemaType ??= typeof(XmlSchemaType);

        private static Type? s_typeOfIExtensibleDataObject;
        internal static Type TypeOfIExtensibleDataObject =>
            s_typeOfIExtensibleDataObject ??= typeof(IExtensibleDataObject);

        private static Type? s_typeOfExtensionDataObject;
        internal static Type TypeOfExtensionDataObject =>
            s_typeOfExtensionDataObject ??= typeof(ExtensionDataObject);

        private static Type? s_typeOfISerializableDataNode;
        internal static Type TypeOfISerializableDataNode =>
            s_typeOfISerializableDataNode ??= typeof(ISerializableDataNode);

        private static Type? s_typeOfClassDataNode;
        internal static Type TypeOfClassDataNode =>
            s_typeOfClassDataNode ??= typeof(ClassDataNode);

        private static Type? s_typeOfCollectionDataNode;
        internal static Type TypeOfCollectionDataNode =>
            s_typeOfCollectionDataNode ??= typeof(CollectionDataNode);

        private static Type? s_typeOfXmlDataNode;
        internal static Type TypeOfXmlDataNode =>
            s_typeOfXmlDataNode ??= typeof(XmlDataNode);

        private static Type? s_typeOfNullable;
        internal static Type TypeOfNullable =>
            s_typeOfNullable ??= typeof(Nullable<>);

        private static Type? s_typeOfIDictionaryGeneric;
        internal static Type TypeOfIDictionaryGeneric =>
            s_typeOfIDictionaryGeneric ??= typeof(IDictionary<,>);

        private static Type? s_typeOfIDictionary;
        internal static Type TypeOfIDictionary =>
            s_typeOfIDictionary ??= typeof(IDictionary);

        private static Type? s_typeOfIListGeneric;
        internal static Type TypeOfIListGeneric =>
            s_typeOfIListGeneric ??= typeof(IList<>);

        private static Type? s_typeOfIList;
        internal static Type TypeOfIList =>
            s_typeOfIList ??= typeof(IList);

        private static Type? s_typeOfICollectionGeneric;
        internal static Type TypeOfICollectionGeneric =>
            s_typeOfICollectionGeneric ??= typeof(ICollection<>);

        private static Type? s_typeOfICollection;
        internal static Type TypeOfICollection =>
            s_typeOfICollection ??= typeof(ICollection);

        private static Type? s_typeOfIEnumerableGeneric;
        internal static Type TypeOfIEnumerableGeneric =>
            s_typeOfIEnumerableGeneric ??= typeof(IEnumerable<>);

        private static Type? s_typeOfIEnumerable;
        internal static Type TypeOfIEnumerable =>
            s_typeOfIEnumerable ??= typeof(IEnumerable);

        private static Type? s_typeOfIEnumeratorGeneric;
        internal static Type TypeOfIEnumeratorGeneric =>
            s_typeOfIEnumeratorGeneric ??= typeof(IEnumerator<>);

        private static Type? s_typeOfIEnumerator;
        internal static Type TypeOfIEnumerator =>
            s_typeOfIEnumerator ??= typeof(IEnumerator);

        private static Type? s_typeOfKeyValuePair;
        internal static Type TypeOfKeyValuePair =>
            s_typeOfKeyValuePair ??= typeof(KeyValuePair<,>);

        private static Type? s_typeOfKeyValuePairAdapter;
        internal static Type TypeOfKeyValuePairAdapter =>
            s_typeOfKeyValuePairAdapter ??= typeof(KeyValuePairAdapter<,>);

        private static Type? s_typeOfKeyValue;
        internal static Type TypeOfKeyValue =>
            s_typeOfKeyValue ??= typeof(KeyValue<,>);

        private static Type? s_typeOfIDictionaryEnumerator;
        internal static Type TypeOfIDictionaryEnumerator =>
            s_typeOfIDictionaryEnumerator ??= typeof(IDictionaryEnumerator);

        private static Type? s_typeOfDictionaryEnumerator;
        internal static Type TypeOfDictionaryEnumerator =>
            s_typeOfDictionaryEnumerator ??= typeof(CollectionDataContract.DictionaryEnumerator);

        private static Type? s_typeOfGenericDictionaryEnumerator;
        internal static Type TypeOfGenericDictionaryEnumerator =>
            s_typeOfGenericDictionaryEnumerator ??= typeof(CollectionDataContract.GenericDictionaryEnumerator<,>);

        private static Type? s_typeOfDictionaryGeneric;
        internal static Type TypeOfDictionaryGeneric =>
            s_typeOfDictionaryGeneric ??= typeof(Dictionary<,>);

        private static Type? s_typeOfHashtable;
        internal static Type TypeOfHashtable
        {
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get => s_typeOfHashtable ??= TypeOfDictionaryGeneric.MakeGenericType(TypeOfObject, TypeOfObject);
        }

        private static Type? s_typeOfXmlElement;
        internal static Type TypeOfXmlElement =>
            s_typeOfXmlElement ??= typeof(XmlElement);

        private static Type? s_typeOfXmlNodeArray;
        internal static Type TypeOfXmlNodeArray =>
            s_typeOfXmlNodeArray ??= typeof(XmlNode[]);

        private static Type? s_typeOfDBNull;
        internal static Type TypeOfDBNull =>
            s_typeOfDBNull ??= typeof(DBNull);

        private static Uri? s_dataContractXsdBaseNamespaceUri;
        internal static Uri DataContractXsdBaseNamespaceUri =>
            s_dataContractXsdBaseNamespaceUri ??= new Uri(DataContractXsdBaseNamespace);

        private static readonly Type? s_typeOfScriptObject;

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static ClassDataContract CreateScriptObjectClassDataContract()
        {
            Debug.Assert(s_typeOfScriptObject != null);
            return new ClassDataContract(s_typeOfScriptObject);
        }

        internal static bool TypeOfScriptObject_IsAssignableFrom(Type type) =>
            s_typeOfScriptObject != null && s_typeOfScriptObject.IsAssignableFrom(type);

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
        public static partial Regex FullSRSInternalsVisibleRegex();
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
