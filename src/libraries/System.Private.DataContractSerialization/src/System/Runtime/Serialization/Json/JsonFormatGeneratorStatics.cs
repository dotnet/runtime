// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Reflection;
using System.Xml;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization.Json;

namespace System.Runtime.Serialization
{
    public static class JsonFormatGeneratorStatics
    {
        private static MethodInfo? s_boxPointer;

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

        private static MethodInfo? s_unboxPointer;

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

        public static MethodInfo BoxPointer
        {
            get
            {
                if (s_boxPointer == null)
                {
                    s_boxPointer = typeof(Pointer).GetMethod("Box");
                    Debug.Assert(s_boxPointer != null);
                }
                return s_boxPointer;
            }
        }

        public static PropertyInfo CollectionItemNameProperty
        {
            get
            {
                if (s_collectionItemNameProperty == null)
                {
                    s_collectionItemNameProperty = typeof(XmlObjectSerializerWriteContextComplexJson).GetProperty("CollectionItemName", Globals.ScanAllMembers);
                    Debug.Assert(s_collectionItemNameProperty != null);
                }

                return s_collectionItemNameProperty;
            }
        }

        public static ConstructorInfo ExtensionDataObjectCtor => s_extensionDataObjectCtor ??
                                                                 (s_extensionDataObjectCtor =
                                                                     typeof(ExtensionDataObject).GetConstructor(Globals.ScanAllMembers, Type.EmptyTypes))!;

        public static PropertyInfo ExtensionDataProperty => s_extensionDataProperty ??= typeof(IExtensibleDataObject).GetProperty("ExtensionData")!;

        public static MethodInfo GetCurrentMethod
        {
            get
            {
                if (s_ienumeratorGetCurrentMethod == null)
                {
                    s_ienumeratorGetCurrentMethod = typeof(IEnumerator).GetProperty("Current")!.GetGetMethod();
                    Debug.Assert(s_ienumeratorGetCurrentMethod != null);
                }
                return s_ienumeratorGetCurrentMethod;
            }
        }
        public static MethodInfo GetItemContractMethod
        {
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get
            {
                if (s_getItemContractMethod == null)
                {
                    s_getItemContractMethod = typeof(CollectionDataContract).GetProperty("ItemContract", Globals.ScanAllMembers)!.GetGetMethod(nonPublic: true);
                    Debug.Assert(s_getItemContractMethod != null);
                }
                return s_getItemContractMethod;
            }
        }
        public static MethodInfo GetJsonDataContractMethod
        {
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get
            {
                if (s_getJsonDataContractMethod == null)
                {
                    s_getJsonDataContractMethod = typeof(JsonDataContract).GetMethod("GetJsonDataContract", Globals.ScanAllMembers);
                    Debug.Assert(s_getJsonDataContractMethod != null);
                }
                return s_getJsonDataContractMethod;
            }
        }
        public static MethodInfo GetJsonMemberIndexMethod
        {
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get
            {
                if (s_getJsonMemberIndexMethod == null)
                {
                    s_getJsonMemberIndexMethod = typeof(XmlObjectSerializerReadContextComplexJson).GetMethod("GetJsonMemberIndex", Globals.ScanAllMembers);
                    Debug.Assert(s_getJsonMemberIndexMethod != null);
                }
                return s_getJsonMemberIndexMethod;
            }
        }
        public static MethodInfo GetRevisedItemContractMethod
        {
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get
            {
                if (s_getRevisedItemContractMethod == null)
                {
                    s_getRevisedItemContractMethod = typeof(XmlObjectSerializerWriteContextComplexJson).GetMethod("GetRevisedItemContract", Globals.ScanAllMembers);
                    Debug.Assert(s_getRevisedItemContractMethod != null);
                }
                return s_getRevisedItemContractMethod;
            }
        }
        public static MethodInfo GetUninitializedObjectMethod
        {
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get
            {
                if (s_getUninitializedObjectMethod == null)
                {
                    s_getUninitializedObjectMethod = typeof(XmlFormatReaderGenerator).GetMethod("UnsafeGetUninitializedObject", Globals.ScanAllMembers, new Type[] { typeof(Type) });
                    Debug.Assert(s_getUninitializedObjectMethod != null);
                }
                return s_getUninitializedObjectMethod;
            }
        }

        public static MethodInfo IsStartElementMethod0
        {
            get
            {
                if (s_isStartElementMethod0 == null)
                {
                    s_isStartElementMethod0 = typeof(XmlReaderDelegator).GetMethod("IsStartElement", Globals.ScanAllMembers, Type.EmptyTypes);
                    Debug.Assert(s_isStartElementMethod0 != null);
                }
                return s_isStartElementMethod0;
            }
        }
        public static MethodInfo IsStartElementMethod2
        {
            get
            {
                if (s_isStartElementMethod2 == null)
                {
                    s_isStartElementMethod2 = typeof(XmlReaderDelegator).GetMethod("IsStartElement", Globals.ScanAllMembers, new Type[] { typeof(XmlDictionaryString), typeof(XmlDictionaryString) });
                    Debug.Assert(s_isStartElementMethod2 != null);
                }
                return s_isStartElementMethod2;
            }
        }
        public static PropertyInfo LocalNameProperty
        {
            get
            {
                if (s_localNameProperty == null)
                {
                    s_localNameProperty = typeof(XmlReaderDelegator).GetProperty("LocalName", Globals.ScanAllMembers);
                    Debug.Assert(s_localNameProperty != null);
                }
                return s_localNameProperty;
            }
        }
        public static PropertyInfo NamespaceProperty
        {
            get
            {
                if (s_namespaceProperty == null)
                {
                    s_namespaceProperty = typeof(XmlReaderDelegator).GetProperty("NamespaceProperty", Globals.ScanAllMembers);
                    Debug.Assert(s_namespaceProperty != null);
                }
                return s_namespaceProperty;
            }
        }
        public static MethodInfo MoveNextMethod
        {
            get
            {
                if (s_ienumeratorMoveNextMethod == null)
                {
                    s_ienumeratorMoveNextMethod = typeof(IEnumerator).GetMethod("MoveNext");
                    Debug.Assert(s_ienumeratorMoveNextMethod != null);
                }
                return s_ienumeratorMoveNextMethod;
            }
        }
        public static MethodInfo MoveToContentMethod
        {
            get
            {
                if (s_moveToContentMethod == null)
                {
                    s_moveToContentMethod = typeof(XmlReaderDelegator).GetMethod("MoveToContent", Globals.ScanAllMembers);
                    Debug.Assert(s_moveToContentMethod != null);
                }
                return s_moveToContentMethod;
            }
        }
        public static PropertyInfo NodeTypeProperty
        {
            get
            {
                if (s_nodeTypeProperty == null)
                {
                    s_nodeTypeProperty = typeof(XmlReaderDelegator).GetProperty("NodeType", Globals.ScanAllMembers);
                    Debug.Assert(s_nodeTypeProperty != null);
                }
                return s_nodeTypeProperty;
            }
        }
        public static MethodInfo OnDeserializationMethod
        {
            get
            {
                if (s_onDeserializationMethod == null)
                {
                    s_onDeserializationMethod = typeof(IDeserializationCallback).GetMethod("OnDeserialization");
                    Debug.Assert(s_onDeserializationMethod != null);
                }
                return s_onDeserializationMethod;
            }
        }

        public static MethodInfo ReadJsonValueMethod
        {
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get
            {
                if (s_readJsonValueMethod == null)
                {
                    s_readJsonValueMethod = typeof(DataContractJsonSerializer).GetMethod("ReadJsonValue", Globals.ScanAllMembers);
                    Debug.Assert(s_readJsonValueMethod != null);
                }
                return s_readJsonValueMethod;
            }
        }
        public static ConstructorInfo SerializationExceptionCtor
        {
            get
            {
                if (s_serializationExceptionCtor == null)
                {
                    s_serializationExceptionCtor = typeof(SerializationException).GetConstructor(new Type[] { typeof(string) });
                    Debug.Assert(s_serializationExceptionCtor != null);
                }
                return s_serializationExceptionCtor;
            }
        }
        public static Type[] SerInfoCtorArgs => s_serInfoCtorArgs ??= new Type[] { typeof(SerializationInfo), typeof(StreamingContext) };
        public static MethodInfo ThrowDuplicateMemberExceptionMethod
        {
            get
            {
                if (s_throwDuplicateMemberExceptionMethod == null)
                {
                    s_throwDuplicateMemberExceptionMethod = typeof(XmlObjectSerializerReadContextComplexJson).GetMethod("ThrowDuplicateMemberException", Globals.ScanAllMembers);
                    Debug.Assert(s_throwDuplicateMemberExceptionMethod != null);
                }
                return s_throwDuplicateMemberExceptionMethod;
            }
        }
        public static MethodInfo ThrowMissingRequiredMembersMethod
        {
            get
            {
                if (s_throwMissingRequiredMembersMethod == null)
                {
                    s_throwMissingRequiredMembersMethod = typeof(XmlObjectSerializerReadContextComplexJson).GetMethod("ThrowMissingRequiredMembers", Globals.ScanAllMembers);
                    Debug.Assert(s_throwMissingRequiredMembersMethod != null);
                }
                return s_throwMissingRequiredMembersMethod;
            }
        }
        public static PropertyInfo TypeHandleProperty
        {
            get
            {
                if (s_typeHandleProperty == null)
                {
                    s_typeHandleProperty = typeof(Type).GetProperty("TypeHandle");
                    Debug.Assert(s_typeHandleProperty != null);
                }
                return s_typeHandleProperty;
            }
        }
        public static MethodInfo UnboxPointer
        {
            get
            {
                if (s_unboxPointer == null)
                {
                    s_unboxPointer = typeof(Pointer).GetMethod("Unbox");
                    Debug.Assert(s_unboxPointer != null);
                }
                return s_unboxPointer;
            }
        }
        public static PropertyInfo UseSimpleDictionaryFormatReadProperty
        {
            get
            {
                if (s_useSimpleDictionaryFormatReadProperty == null)
                {
                    s_useSimpleDictionaryFormatReadProperty = typeof(XmlObjectSerializerReadContextComplexJson).GetProperty("UseSimpleDictionaryFormat", Globals.ScanAllMembers);
                    Debug.Assert(s_useSimpleDictionaryFormatReadProperty != null);
                }
                return s_useSimpleDictionaryFormatReadProperty;
            }
        }
        public static PropertyInfo UseSimpleDictionaryFormatWriteProperty
        {
            get
            {
                if (s_useSimpleDictionaryFormatWriteProperty == null)
                {
                    s_useSimpleDictionaryFormatWriteProperty = typeof(XmlObjectSerializerWriteContextComplexJson).GetProperty("UseSimpleDictionaryFormat", Globals.ScanAllMembers);
                    Debug.Assert(s_useSimpleDictionaryFormatWriteProperty != null);
                }
                return s_useSimpleDictionaryFormatWriteProperty;
            }
        }
        public static MethodInfo WriteAttributeStringMethod
        {
            get
            {
                if (s_writeAttributeStringMethod == null)
                {
                    s_writeAttributeStringMethod = typeof(XmlWriterDelegator).GetMethod("WriteAttributeString", Globals.ScanAllMembers, new Type[] { typeof(string), typeof(string), typeof(string), typeof(string) });
                    Debug.Assert(s_writeAttributeStringMethod != null);
                }
                return s_writeAttributeStringMethod;
            }
        }
        public static MethodInfo WriteEndElementMethod
        {
            get
            {
                if (s_writeEndElementMethod == null)
                {
                    s_writeEndElementMethod = typeof(XmlWriterDelegator).GetMethod("WriteEndElement", Globals.ScanAllMembers, Type.EmptyTypes);
                    Debug.Assert(s_writeEndElementMethod != null);
                }
                return s_writeEndElementMethod;
            }
        }
        public static MethodInfo WriteJsonISerializableMethod
        {
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get
            {
                if (s_writeJsonISerializableMethod == null)
                {
                    s_writeJsonISerializableMethod = typeof(XmlObjectSerializerWriteContextComplexJson).GetMethod("WriteJsonISerializable", Globals.ScanAllMembers);
                    Debug.Assert(s_writeJsonISerializableMethod != null);
                }
                return s_writeJsonISerializableMethod;
            }
        }
        public static MethodInfo WriteJsonNameWithMappingMethod
        {
            get
            {
                if (s_writeJsonNameWithMappingMethod == null)
                {
                    s_writeJsonNameWithMappingMethod = typeof(XmlObjectSerializerWriteContextComplexJson).GetMethod("WriteJsonNameWithMapping", Globals.ScanAllMembers);
                    Debug.Assert(s_writeJsonNameWithMappingMethod != null);
                }
                return s_writeJsonNameWithMappingMethod;
            }
        }
        public static MethodInfo WriteJsonValueMethod
        {
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get
            {
                if (s_writeJsonValueMethod == null)
                {
                    s_writeJsonValueMethod = typeof(DataContractJsonSerializer).GetMethod("WriteJsonValue", Globals.ScanAllMembers);
                    Debug.Assert(s_writeJsonValueMethod != null);
                }
                return s_writeJsonValueMethod;
            }
        }
        public static MethodInfo WriteStartElementMethod
        {
            get
            {
                if (s_writeStartElementMethod == null)
                {
                    s_writeStartElementMethod = typeof(XmlWriterDelegator).GetMethod("WriteStartElement", Globals.ScanAllMembers, new Type[] { typeof(XmlDictionaryString), typeof(XmlDictionaryString) });
                    Debug.Assert(s_writeStartElementMethod != null);
                }
                return s_writeStartElementMethod;
            }
        }

        public static MethodInfo WriteStartElementStringMethod
        {
            get
            {
                if (s_writeStartElementStringMethod == null)
                {
                    s_writeStartElementStringMethod = typeof(XmlWriterDelegator).GetMethod("WriteStartElement", Globals.ScanAllMembers, new Type[] { typeof(string), typeof(string) });
                    Debug.Assert(s_writeStartElementStringMethod != null);
                }
                return s_writeStartElementStringMethod;
            }
        }

        public static MethodInfo ParseEnumMethod
        {
            get
            {
                if (s_parseEnumMethod == null)
                {
                    s_parseEnumMethod = typeof(Enum).GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, new Type[] { typeof(Type), typeof(string) });
                    Debug.Assert(s_parseEnumMethod != null);
                }
                return s_parseEnumMethod;
            }
        }

        public static MethodInfo GetJsonMemberNameMethod
        {
            get
            {
                if (s_getJsonMemberNameMethod == null)
                {
                    s_getJsonMemberNameMethod = typeof(XmlObjectSerializerReadContextComplexJson).GetMethod("GetJsonMemberName", Globals.ScanAllMembers, new Type[] { typeof(XmlReaderDelegator) });
                    Debug.Assert(s_getJsonMemberNameMethod != null);
                }
                return s_getJsonMemberNameMethod;
            }
        }
    }
}
