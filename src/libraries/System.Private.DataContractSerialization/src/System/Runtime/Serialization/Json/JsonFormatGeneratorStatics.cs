// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Xml;
using System.Diagnostics;

namespace System.Runtime.Serialization
{
    public static class JsonFormatGeneratorStatics
    {
        private static PropertyInfo? s_collectionItemNameProperty;

        private static ConstructorInfo? s_extensionDataObjectCtor;

        private static PropertyInfo? s_extensionDataProperty;

        private static MethodInfo? s_getItemContractMethod;

        private static MethodInfo? s_getJsonDataContractMethod;

        private static MethodInfo? s_getJsonMemberIndexMethod;

        private static MethodInfo? s_getRevisedItemContractMethod;

        private static MethodInfo? s_getUninitializedObjectMethod;

        private static MethodInfo? s_ienumeratorGetCurrentMethod;

        private static MethodInfo? s_ienumeratorMoveNextMethod;

        private static MethodInfo? s_isStartElementMethod0;

        private static MethodInfo? s_isStartElementMethod2;

        private static PropertyInfo? s_localNameProperty;

        private static PropertyInfo? s_namespaceProperty;

        private static MethodInfo? s_moveToContentMethod;

        private static PropertyInfo? s_nodeTypeProperty;

        private static MethodInfo? s_onDeserializationMethod;

        private static MethodInfo? s_readJsonValueMethod;

        private static ConstructorInfo? s_serializationExceptionCtor;

        private static Type[]? s_serInfoCtorArgs;

        private static MethodInfo? s_throwDuplicateMemberExceptionMethod;

        private static MethodInfo? s_throwMissingRequiredMembersMethod;

        private static PropertyInfo? s_typeHandleProperty;

        private static PropertyInfo? s_useSimpleDictionaryFormatReadProperty;

        private static PropertyInfo? s_useSimpleDictionaryFormatWriteProperty;

        private static MethodInfo? s_writeAttributeStringMethod;

        private static MethodInfo? s_writeEndElementMethod;

        private static MethodInfo? s_writeJsonISerializableMethod;

        private static MethodInfo? s_writeJsonNameWithMappingMethod;

        private static MethodInfo? s_writeJsonValueMethod;

        private static MethodInfo? s_writeStartElementMethod;

        private static MethodInfo? s_writeStartElementStringMethod;

        private static MethodInfo? s_parseEnumMethod;

        private static MethodInfo? s_getJsonMemberNameMethod;

        public static PropertyInfo CollectionItemNameProperty
        {
            get
            {
                if (s_collectionItemNameProperty is null)
                {
                    s_collectionItemNameProperty = typeof(XmlObjectSerializerWriteContextComplexJson).GetProperty("CollectionItemName", Globals.ScanAllMembers);
                    Debug.Assert(s_collectionItemNameProperty is not null);
                }

                return s_collectionItemNameProperty;
            }
        }

        public static ConstructorInfo ExtensionDataObjectCtor => s_extensionDataObjectCtor ??
                                                                 (s_extensionDataObjectCtor =
                                                                     typeof(ExtensionDataObject).GetConstructor(Globals.ScanAllMembers, null, Array.Empty<Type>(), null))!;

        public static PropertyInfo ExtensionDataProperty => s_extensionDataProperty ??
                                                            (s_extensionDataProperty = typeof(IExtensibleDataObject).GetProperty("ExtensionData")!);

        public static MethodInfo GetCurrentMethod
        {
            get
            {
                if (s_ienumeratorGetCurrentMethod is null)
                {
                    s_ienumeratorGetCurrentMethod = typeof(IEnumerator).GetProperty("Current")!.GetGetMethod();
                    Debug.Assert(s_ienumeratorGetCurrentMethod is not null);
                }
                return s_ienumeratorGetCurrentMethod;
            }
        }
        public static MethodInfo GetItemContractMethod
        {
            get
            {
                if (s_getItemContractMethod is null)
                {
                    s_getItemContractMethod = typeof(CollectionDataContract).GetProperty("ItemContract", Globals.ScanAllMembers)!.GetGetMethod(nonPublic: true);
                    Debug.Assert(s_getItemContractMethod is not null);
                }
                return s_getItemContractMethod;
            }
        }
        public static MethodInfo GetJsonDataContractMethod
        {
            get
            {
                if (s_getJsonDataContractMethod is null)
                {
                    s_getJsonDataContractMethod = typeof(JsonDataContract).GetMethod("GetJsonDataContract", Globals.ScanAllMembers);
                    Debug.Assert(s_getJsonDataContractMethod is not null);
                }
                return s_getJsonDataContractMethod;
            }
        }
        public static MethodInfo GetJsonMemberIndexMethod
        {
            get
            {
                if (s_getJsonMemberIndexMethod is null)
                {
                    s_getJsonMemberIndexMethod = typeof(XmlObjectSerializerReadContextComplexJson).GetMethod("GetJsonMemberIndex", Globals.ScanAllMembers);
                    Debug.Assert(s_getJsonMemberIndexMethod is not null);
                }
                return s_getJsonMemberIndexMethod;
            }
        }
        public static MethodInfo GetRevisedItemContractMethod
        {
            get
            {
                if (s_getRevisedItemContractMethod is null)
                {
                    s_getRevisedItemContractMethod = typeof(XmlObjectSerializerWriteContextComplexJson).GetMethod("GetRevisedItemContract", Globals.ScanAllMembers);
                    Debug.Assert(s_getRevisedItemContractMethod is not null);
                }
                return s_getRevisedItemContractMethod;
            }
        }
        public static MethodInfo GetUninitializedObjectMethod
        {
            get
            {
                if (s_getUninitializedObjectMethod is null)
                {
                    s_getUninitializedObjectMethod = typeof(XmlFormatReaderGenerator).GetMethod("UnsafeGetUninitializedObject", Globals.ScanAllMembers, null, new Type[] { typeof(Type) }, null);
                    Debug.Assert(s_getUninitializedObjectMethod is not null);
                }
                return s_getUninitializedObjectMethod;
            }
        }

        public static MethodInfo IsStartElementMethod0
        {
            get
            {
                if (s_isStartElementMethod0 is null)
                {
                    s_isStartElementMethod0 = typeof(XmlReaderDelegator).GetMethod("IsStartElement", Globals.ScanAllMembers, null, Array.Empty<Type>(), null);
                    Debug.Assert(s_isStartElementMethod0 is not null);
                }
                return s_isStartElementMethod0;
            }
        }
        public static MethodInfo IsStartElementMethod2
        {
            get
            {
                if (s_isStartElementMethod2 is null)
                {
                    s_isStartElementMethod2 = typeof(XmlReaderDelegator).GetMethod("IsStartElement", Globals.ScanAllMembers, null, new Type[] { typeof(XmlDictionaryString), typeof(XmlDictionaryString) }, null);
                    Debug.Assert(s_isStartElementMethod2 is not null);
                }
                return s_isStartElementMethod2;
            }
        }
        public static PropertyInfo LocalNameProperty
        {
            get
            {
                if (s_localNameProperty is null)
                {
                    s_localNameProperty = typeof(XmlReaderDelegator).GetProperty("LocalName", Globals.ScanAllMembers);
                    Debug.Assert(s_localNameProperty is not null);
                }
                return s_localNameProperty;
            }
        }
        public static PropertyInfo NamespaceProperty
        {
            get
            {
                if (s_namespaceProperty is null)
                {
                    s_namespaceProperty = typeof(XmlReaderDelegator).GetProperty("NamespaceProperty", Globals.ScanAllMembers);
                    Debug.Assert(s_namespaceProperty is not null);
                }
                return s_namespaceProperty;
            }
        }
        public static MethodInfo MoveNextMethod
        {
            get
            {
                if (s_ienumeratorMoveNextMethod is null)
                {
                    s_ienumeratorMoveNextMethod = typeof(IEnumerator).GetMethod("MoveNext");
                    Debug.Assert(s_ienumeratorMoveNextMethod is not null);
                }
                return s_ienumeratorMoveNextMethod;
            }
        }
        public static MethodInfo MoveToContentMethod
        {
            get
            {
                if (s_moveToContentMethod is null)
                {
                    s_moveToContentMethod = typeof(XmlReaderDelegator).GetMethod("MoveToContent", Globals.ScanAllMembers);
                    Debug.Assert(s_moveToContentMethod is not null);
                }
                return s_moveToContentMethod;
            }
        }
        public static PropertyInfo NodeTypeProperty
        {
            get
            {
                if (s_nodeTypeProperty is null)
                {
                    s_nodeTypeProperty = typeof(XmlReaderDelegator).GetProperty("NodeType", Globals.ScanAllMembers);
                    Debug.Assert(s_nodeTypeProperty is not null);
                }
                return s_nodeTypeProperty;
            }
        }
        public static MethodInfo OnDeserializationMethod
        {
            get
            {
                if (s_onDeserializationMethod is null)
                {
                    s_onDeserializationMethod = typeof(IDeserializationCallback).GetMethod("OnDeserialization");
                    Debug.Assert(s_onDeserializationMethod is not null);
                }
                return s_onDeserializationMethod;
            }
        }
        public static MethodInfo ReadJsonValueMethod
        {
            get
            {
                if (s_readJsonValueMethod is null)
                {
                    s_readJsonValueMethod = typeof(DataContractJsonSerializer).GetMethod("ReadJsonValue", Globals.ScanAllMembers);
                    Debug.Assert(s_readJsonValueMethod is not null);
                }
                return s_readJsonValueMethod;
            }
        }
        public static ConstructorInfo SerializationExceptionCtor
        {
            get
            {
                if (s_serializationExceptionCtor is null)
                {
                    s_serializationExceptionCtor = typeof(SerializationException).GetConstructor(new Type[] { typeof(string) });
                    Debug.Assert(s_serializationExceptionCtor is not null);
                }
                return s_serializationExceptionCtor;
            }
        }
        public static Type[] SerInfoCtorArgs
        {
            get
            {
                if (s_serInfoCtorArgs is null)
                {
                    s_serInfoCtorArgs = new Type[] { typeof(SerializationInfo), typeof(StreamingContext) };
                }
                return s_serInfoCtorArgs;
            }
        }
        public static MethodInfo ThrowDuplicateMemberExceptionMethod
        {
            get
            {
                if (s_throwDuplicateMemberExceptionMethod is null)
                {
                    s_throwDuplicateMemberExceptionMethod = typeof(XmlObjectSerializerReadContextComplexJson).GetMethod("ThrowDuplicateMemberException", Globals.ScanAllMembers);
                    Debug.Assert(s_throwDuplicateMemberExceptionMethod is not null);
                }
                return s_throwDuplicateMemberExceptionMethod;
            }
        }
        public static MethodInfo ThrowMissingRequiredMembersMethod
        {
            get
            {
                if (s_throwMissingRequiredMembersMethod is null)
                {
                    s_throwMissingRequiredMembersMethod = typeof(XmlObjectSerializerReadContextComplexJson).GetMethod("ThrowMissingRequiredMembers", Globals.ScanAllMembers);
                    Debug.Assert(s_throwMissingRequiredMembersMethod is not null);
                }
                return s_throwMissingRequiredMembersMethod;
            }
        }
        public static PropertyInfo TypeHandleProperty
        {
            get
            {
                if (s_typeHandleProperty is null)
                {
                    s_typeHandleProperty = typeof(Type).GetProperty("TypeHandle");
                    Debug.Assert(s_typeHandleProperty is not null);
                }
                return s_typeHandleProperty;
            }
        }
        public static PropertyInfo UseSimpleDictionaryFormatReadProperty
        {
            get
            {
                if (s_useSimpleDictionaryFormatReadProperty is null)
                {
                    s_useSimpleDictionaryFormatReadProperty = typeof(XmlObjectSerializerReadContextComplexJson).GetProperty("UseSimpleDictionaryFormat", Globals.ScanAllMembers);
                    Debug.Assert(s_useSimpleDictionaryFormatReadProperty is not null);
                }
                return s_useSimpleDictionaryFormatReadProperty;
            }
        }
        public static PropertyInfo UseSimpleDictionaryFormatWriteProperty
        {
            get
            {
                if (s_useSimpleDictionaryFormatWriteProperty is null)
                {
                    s_useSimpleDictionaryFormatWriteProperty = typeof(XmlObjectSerializerWriteContextComplexJson).GetProperty("UseSimpleDictionaryFormat", Globals.ScanAllMembers);
                    Debug.Assert(s_useSimpleDictionaryFormatWriteProperty is not null);
                }
                return s_useSimpleDictionaryFormatWriteProperty;
            }
        }
        public static MethodInfo WriteAttributeStringMethod
        {
            get
            {
                if (s_writeAttributeStringMethod is null)
                {
                    s_writeAttributeStringMethod = typeof(XmlWriterDelegator).GetMethod("WriteAttributeString", Globals.ScanAllMembers, null, new Type[] { typeof(string), typeof(string), typeof(string), typeof(string) }, null);
                    Debug.Assert(s_writeAttributeStringMethod is not null);
                }
                return s_writeAttributeStringMethod;
            }
        }
        public static MethodInfo WriteEndElementMethod
        {
            get
            {
                if (s_writeEndElementMethod is null)
                {
                    s_writeEndElementMethod = typeof(XmlWriterDelegator).GetMethod("WriteEndElement", Globals.ScanAllMembers, null, Array.Empty<Type>(), null);
                    Debug.Assert(s_writeEndElementMethod is not null);
                }
                return s_writeEndElementMethod;
            }
        }
        public static MethodInfo WriteJsonISerializableMethod
        {
            get
            {
                if (s_writeJsonISerializableMethod is null)
                {
                    s_writeJsonISerializableMethod = typeof(XmlObjectSerializerWriteContextComplexJson).GetMethod("WriteJsonISerializable", Globals.ScanAllMembers);
                    Debug.Assert(s_writeJsonISerializableMethod is not null);
                }
                return s_writeJsonISerializableMethod;
            }
        }
        public static MethodInfo WriteJsonNameWithMappingMethod
        {
            get
            {
                if (s_writeJsonNameWithMappingMethod is null)
                {
                    s_writeJsonNameWithMappingMethod = typeof(XmlObjectSerializerWriteContextComplexJson).GetMethod("WriteJsonNameWithMapping", Globals.ScanAllMembers);
                    Debug.Assert(s_writeJsonNameWithMappingMethod is not null);
                }
                return s_writeJsonNameWithMappingMethod;
            }
        }
        public static MethodInfo WriteJsonValueMethod
        {
            get
            {
                if (s_writeJsonValueMethod is null)
                {
                    s_writeJsonValueMethod = typeof(DataContractJsonSerializer).GetMethod("WriteJsonValue", Globals.ScanAllMembers);
                    Debug.Assert(s_writeJsonValueMethod is not null);
                }
                return s_writeJsonValueMethod;
            }
        }
        public static MethodInfo WriteStartElementMethod
        {
            get
            {
                if (s_writeStartElementMethod is null)
                {
                    s_writeStartElementMethod = typeof(XmlWriterDelegator).GetMethod("WriteStartElement", Globals.ScanAllMembers, null, new Type[] { typeof(XmlDictionaryString), typeof(XmlDictionaryString) }, null);
                    Debug.Assert(s_writeStartElementMethod is not null);
                }
                return s_writeStartElementMethod;
            }
        }

        public static MethodInfo WriteStartElementStringMethod
        {
            get
            {
                if (s_writeStartElementStringMethod is null)
                {
                    s_writeStartElementStringMethod = typeof(XmlWriterDelegator).GetMethod("WriteStartElement", Globals.ScanAllMembers, null, new Type[] { typeof(string), typeof(string) }, null);
                    Debug.Assert(s_writeStartElementStringMethod is not null);
                }
                return s_writeStartElementStringMethod;
            }
        }

        public static MethodInfo ParseEnumMethod
        {
            get
            {
                if (s_parseEnumMethod is null)
                {
                    s_parseEnumMethod = typeof(Enum).GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(Type), typeof(string) }, null);
                    Debug.Assert(s_parseEnumMethod is not null);
                }
                return s_parseEnumMethod;
            }
        }

        public static MethodInfo GetJsonMemberNameMethod
        {
            get
            {
                if (s_getJsonMemberNameMethod is null)
                {
                    s_getJsonMemberNameMethod = typeof(XmlObjectSerializerReadContextComplexJson).GetMethod("GetJsonMemberName", Globals.ScanAllMembers, null, new Type[] { typeof(XmlReaderDelegator) }, null);
                    Debug.Assert(s_getJsonMemberNameMethod is not null);
                }
                return s_getJsonMemberNameMethod;
            }
        }
    }
}
