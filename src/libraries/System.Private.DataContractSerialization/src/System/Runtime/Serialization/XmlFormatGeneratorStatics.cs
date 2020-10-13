// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Xml;
using System.Collections;
using System.Diagnostics;

namespace System.Runtime.Serialization
{
    internal static class XmlFormatGeneratorStatics
    {
        private static MethodInfo? s_writeStartElementMethod2;
        internal static MethodInfo WriteStartElementMethod2
        {
            get
            {
                if (s_writeStartElementMethod2 is null)
                {
                    s_writeStartElementMethod2 = typeof(XmlWriterDelegator).GetMethod("WriteStartElement", Globals.ScanAllMembers, null, new Type[] { typeof(XmlDictionaryString), typeof(XmlDictionaryString) }, null);
                    Debug.Assert(s_writeStartElementMethod2 is not null);
                }
                return s_writeStartElementMethod2;
            }
        }

        private static MethodInfo? s_writeStartElementMethod3;
        internal static MethodInfo WriteStartElementMethod3
        {
            get
            {
                if (s_writeStartElementMethod3 is null)
                {
                    s_writeStartElementMethod3 = typeof(XmlWriterDelegator).GetMethod("WriteStartElement", Globals.ScanAllMembers, null, new Type[] { typeof(string), typeof(XmlDictionaryString), typeof(XmlDictionaryString) }, null);
                    Debug.Assert(s_writeStartElementMethod3 is not null);
                }
                return s_writeStartElementMethod3;
            }
        }

        private static MethodInfo? s_writeEndElementMethod;
        internal static MethodInfo WriteEndElementMethod
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

        private static MethodInfo? s_writeNamespaceDeclMethod;
        internal static MethodInfo WriteNamespaceDeclMethod
        {
            get
            {
                if (s_writeNamespaceDeclMethod is null)
                {
                    s_writeNamespaceDeclMethod = typeof(XmlWriterDelegator).GetMethod("WriteNamespaceDecl", Globals.ScanAllMembers, null, new Type[] { typeof(XmlDictionaryString) }, null);
                    Debug.Assert(s_writeNamespaceDeclMethod is not null);
                }
                return s_writeNamespaceDeclMethod;
            }
        }

        private static PropertyInfo? s_extensionDataProperty;
        internal static PropertyInfo ExtensionDataProperty => s_extensionDataProperty ??
                                                              (s_extensionDataProperty = typeof(IExtensibleDataObject).GetProperty("ExtensionData")!);

        private static ConstructorInfo? s_dictionaryEnumeratorCtor;
        internal static ConstructorInfo DictionaryEnumeratorCtor
        {
            get
            {
                if (s_dictionaryEnumeratorCtor is null)
                {
                    s_dictionaryEnumeratorCtor = typeof(CollectionDataContract.DictionaryEnumerator).GetConstructor(Globals.ScanAllMembers, null, new Type[] { Globals.TypeOfIDictionaryEnumerator }, null);
                    Debug.Assert(s_dictionaryEnumeratorCtor is not null);
                }
                return s_dictionaryEnumeratorCtor;
            }
        }

        private static MethodInfo? s_ienumeratorMoveNextMethod;
        internal static MethodInfo MoveNextMethod
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

        private static MethodInfo? s_ienumeratorGetCurrentMethod;
        internal static MethodInfo GetCurrentMethod
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

        private static MethodInfo? s_getItemContractMethod;
        internal static MethodInfo GetItemContractMethod
        {
            get
            {
                if (s_getItemContractMethod is null)
                {
                    s_getItemContractMethod = typeof(CollectionDataContract).GetProperty("ItemContract", Globals.ScanAllMembers)!.GetMethod;
                    Debug.Assert(s_getItemContractMethod is not null);
                }
                return s_getItemContractMethod;
            }
        }

        private static MethodInfo? s_isStartElementMethod2;
        internal static MethodInfo IsStartElementMethod2
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

        private static MethodInfo? s_isStartElementMethod0;
        internal static MethodInfo IsStartElementMethod0
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

        private static MethodInfo? s_getUninitializedObjectMethod;
        internal static MethodInfo GetUninitializedObjectMethod
        {
            get
            {
                if (s_getUninitializedObjectMethod is null)
                {
                    s_getUninitializedObjectMethod = typeof(XmlFormatReaderGenerator).GetMethod("UnsafeGetUninitializedObject", Globals.ScanAllMembers, null, new Type[] { typeof(int) }, null);
                    Debug.Assert(s_getUninitializedObjectMethod is not null);
                }
                return s_getUninitializedObjectMethod;
            }
        }

        private static MethodInfo? s_onDeserializationMethod;
        internal static MethodInfo OnDeserializationMethod
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

        private static PropertyInfo? s_nodeTypeProperty;
        internal static PropertyInfo NodeTypeProperty
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

        private static ConstructorInfo? s_extensionDataObjectCtor;
        internal static ConstructorInfo ExtensionDataObjectCtor => s_extensionDataObjectCtor ??
                                                                   (s_extensionDataObjectCtor =
                                                                       typeof(ExtensionDataObject).GetConstructor(Globals.ScanAllMembers, null, Array.Empty<Type>(), null)!);

        private static ConstructorInfo? s_hashtableCtor;
        internal static ConstructorInfo HashtableCtor
        {
            get
            {
                if (s_hashtableCtor is null)
                {
                    s_hashtableCtor = Globals.TypeOfHashtable.GetConstructor(Globals.ScanAllMembers, null, Array.Empty<Type>(), null);
                    Debug.Assert(s_hashtableCtor is not null);
                }
                return s_hashtableCtor;
            }
        }

        private static MethodInfo? s_getStreamingContextMethod;
        internal static MethodInfo GetStreamingContextMethod
        {
            get
            {
                if (s_getStreamingContextMethod is null)
                {
                    s_getStreamingContextMethod = typeof(XmlObjectSerializerContext).GetMethod("GetStreamingContext", Globals.ScanAllMembers);
                    Debug.Assert(s_getStreamingContextMethod is not null);
                }
                return s_getStreamingContextMethod;
            }
        }

        private static MethodInfo? s_getCollectionMemberMethod;
        internal static MethodInfo GetCollectionMemberMethod
        {
            get
            {
                if (s_getCollectionMemberMethod is null)
                {
                    s_getCollectionMemberMethod = typeof(XmlObjectSerializerReadContext).GetMethod("GetCollectionMember", Globals.ScanAllMembers);
                    Debug.Assert(s_getCollectionMemberMethod is not null);
                }
                return s_getCollectionMemberMethod;
            }
        }

        private static MethodInfo? s_storeCollectionMemberInfoMethod;
        internal static MethodInfo StoreCollectionMemberInfoMethod
        {
            get
            {
                if (s_storeCollectionMemberInfoMethod is null)
                {
                    s_storeCollectionMemberInfoMethod = typeof(XmlObjectSerializerReadContext).GetMethod("StoreCollectionMemberInfo", Globals.ScanAllMembers, null, new Type[] { typeof(object) }, null);
                    Debug.Assert(s_storeCollectionMemberInfoMethod is not null);
                }
                return s_storeCollectionMemberInfoMethod;
            }
        }

        private static MethodInfo? s_resetCollectionMemberInfoMethod;
        internal static MethodInfo ResetCollectionMemberInfoMethod
        {
            get
            {
                if (s_resetCollectionMemberInfoMethod is null)
                {
                    s_resetCollectionMemberInfoMethod = typeof(XmlObjectSerializerReadContext).GetMethod("ResetCollectionMemberInfo", Globals.ScanAllMembers, null, Array.Empty<Type>(), null);
                    Debug.Assert(s_resetCollectionMemberInfoMethod is not null);
                }
                return s_resetCollectionMemberInfoMethod;
            }
        }

        private static MethodInfo? s_storeIsGetOnlyCollectionMethod;
        internal static MethodInfo StoreIsGetOnlyCollectionMethod
        {
            get
            {
                if (s_storeIsGetOnlyCollectionMethod is null)
                {
                    s_storeIsGetOnlyCollectionMethod = typeof(XmlObjectSerializerWriteContext).GetMethod("StoreIsGetOnlyCollection", Globals.ScanAllMembers);
                    Debug.Assert(s_storeIsGetOnlyCollectionMethod is not null);
                }
                return s_storeIsGetOnlyCollectionMethod;
            }
        }

        private static MethodInfo? s_resetIsGetOnlyCollection;
        internal static MethodInfo ResetIsGetOnlyCollectionMethod
        {
            get
            {
                if (s_resetIsGetOnlyCollection is null)
                {
                    s_resetIsGetOnlyCollection = typeof(XmlObjectSerializerWriteContext).GetMethod("ResetIsGetOnlyCollection", Globals.ScanAllMembers);
                    Debug.Assert(s_resetIsGetOnlyCollection is not null);
                }
                return s_resetIsGetOnlyCollection;
            }
        }

        private static MethodInfo? s_throwNullValueReturnedForGetOnlyCollectionExceptionMethod;
        internal static MethodInfo ThrowNullValueReturnedForGetOnlyCollectionExceptionMethod
        {
            get
            {
                if (s_throwNullValueReturnedForGetOnlyCollectionExceptionMethod is null)
                {
                    s_throwNullValueReturnedForGetOnlyCollectionExceptionMethod = typeof(XmlObjectSerializerReadContext).GetMethod("ThrowNullValueReturnedForGetOnlyCollectionException", Globals.ScanAllMembers);
                    Debug.Assert(s_throwNullValueReturnedForGetOnlyCollectionExceptionMethod is not null);
                }
                return s_throwNullValueReturnedForGetOnlyCollectionExceptionMethod;
            }
        }

        private static MethodInfo? s_throwArrayExceededSizeExceptionMethod;
        internal static MethodInfo ThrowArrayExceededSizeExceptionMethod
        {
            get
            {
                if (s_throwArrayExceededSizeExceptionMethod is null)
                {
                    s_throwArrayExceededSizeExceptionMethod = typeof(XmlObjectSerializerReadContext).GetMethod("ThrowArrayExceededSizeException", Globals.ScanAllMembers);
                    Debug.Assert(s_throwArrayExceededSizeExceptionMethod is not null);
                }
                return s_throwArrayExceededSizeExceptionMethod;
            }
        }

        private static MethodInfo? s_incrementItemCountMethod;
        internal static MethodInfo IncrementItemCountMethod
        {
            get
            {
                if (s_incrementItemCountMethod is null)
                {
                    s_incrementItemCountMethod = typeof(XmlObjectSerializerContext).GetMethod("IncrementItemCount", Globals.ScanAllMembers);
                    Debug.Assert(s_incrementItemCountMethod is not null);
                }
                return s_incrementItemCountMethod;
            }
        }

        private static MethodInfo? s_internalDeserializeMethod;
        internal static MethodInfo InternalDeserializeMethod
        {
            get
            {
                if (s_internalDeserializeMethod is null)
                {
                    s_internalDeserializeMethod = typeof(XmlObjectSerializerReadContext).GetMethod("InternalDeserialize", Globals.ScanAllMembers, null, new Type[] { typeof(XmlReaderDelegator), typeof(int), typeof(RuntimeTypeHandle), typeof(string), typeof(string) }, null);
                    Debug.Assert(s_internalDeserializeMethod is not null);
                }
                return s_internalDeserializeMethod;
            }
        }

        private static MethodInfo? s_moveToNextElementMethod;
        internal static MethodInfo MoveToNextElementMethod
        {
            get
            {
                if (s_moveToNextElementMethod is null)
                {
                    s_moveToNextElementMethod = typeof(XmlObjectSerializerReadContext).GetMethod("MoveToNextElement", Globals.ScanAllMembers);
                    Debug.Assert(s_moveToNextElementMethod is not null);
                }
                return s_moveToNextElementMethod;
            }
        }

        private static MethodInfo? s_getMemberIndexMethod;
        internal static MethodInfo GetMemberIndexMethod
        {
            get
            {
                if (s_getMemberIndexMethod is null)
                {
                    s_getMemberIndexMethod = typeof(XmlObjectSerializerReadContext).GetMethod("GetMemberIndex", Globals.ScanAllMembers);
                    Debug.Assert(s_getMemberIndexMethod is not null);
                }
                return s_getMemberIndexMethod;
            }
        }

        private static MethodInfo? s_getMemberIndexWithRequiredMembersMethod;
        internal static MethodInfo GetMemberIndexWithRequiredMembersMethod
        {
            get
            {
                if (s_getMemberIndexWithRequiredMembersMethod is null)
                {
                    s_getMemberIndexWithRequiredMembersMethod = typeof(XmlObjectSerializerReadContext).GetMethod("GetMemberIndexWithRequiredMembers", Globals.ScanAllMembers);
                    Debug.Assert(s_getMemberIndexWithRequiredMembersMethod is not null);
                }
                return s_getMemberIndexWithRequiredMembersMethod;
            }
        }

        private static MethodInfo? s_throwRequiredMemberMissingExceptionMethod;
        internal static MethodInfo ThrowRequiredMemberMissingExceptionMethod
        {
            get
            {
                if (s_throwRequiredMemberMissingExceptionMethod is null)
                {
                    s_throwRequiredMemberMissingExceptionMethod = typeof(XmlObjectSerializerReadContext).GetMethod("ThrowRequiredMemberMissingException", Globals.ScanAllMembers);
                    Debug.Assert(s_throwRequiredMemberMissingExceptionMethod is not null);
                }
                return s_throwRequiredMemberMissingExceptionMethod;
            }
        }

        private static MethodInfo? s_skipUnknownElementMethod;
        internal static MethodInfo SkipUnknownElementMethod
        {
            get
            {
                if (s_skipUnknownElementMethod is null)
                {
                    s_skipUnknownElementMethod = typeof(XmlObjectSerializerReadContext).GetMethod("SkipUnknownElement", Globals.ScanAllMembers);
                    Debug.Assert(s_skipUnknownElementMethod is not null);
                }
                return s_skipUnknownElementMethod;
            }
        }

        private static MethodInfo? s_readIfNullOrRefMethod;
        internal static MethodInfo ReadIfNullOrRefMethod
        {
            get
            {
                if (s_readIfNullOrRefMethod is null)
                {
                    s_readIfNullOrRefMethod = typeof(XmlObjectSerializerReadContext).GetMethod("ReadIfNullOrRef", Globals.ScanAllMembers, null, new Type[] { typeof(XmlReaderDelegator), typeof(Type), typeof(bool) }, null);
                    Debug.Assert(s_readIfNullOrRefMethod is not null);
                }
                return s_readIfNullOrRefMethod;
            }
        }

        private static MethodInfo? s_readAttributesMethod;
        internal static MethodInfo ReadAttributesMethod
        {
            get
            {
                if (s_readAttributesMethod is null)
                {
                    s_readAttributesMethod = typeof(XmlObjectSerializerReadContext).GetMethod("ReadAttributes", Globals.ScanAllMembers);
                    Debug.Assert(s_readAttributesMethod is not null);
                }
                return s_readAttributesMethod;
            }
        }

        private static MethodInfo? s_resetAttributesMethod;
        internal static MethodInfo ResetAttributesMethod
        {
            get
            {
                if (s_resetAttributesMethod is null)
                {
                    s_resetAttributesMethod = typeof(XmlObjectSerializerReadContext).GetMethod("ResetAttributes", Globals.ScanAllMembers);
                    Debug.Assert(s_resetAttributesMethod is not null);
                }
                return s_resetAttributesMethod;
            }
        }

        private static MethodInfo? s_getObjectIdMethod;
        internal static MethodInfo GetObjectIdMethod
        {
            get
            {
                if (s_getObjectIdMethod is null)
                {
                    s_getObjectIdMethod = typeof(XmlObjectSerializerReadContext).GetMethod("GetObjectId", Globals.ScanAllMembers);
                    Debug.Assert(s_getObjectIdMethod is not null);
                }
                return s_getObjectIdMethod;
            }
        }

        private static MethodInfo? s_getArraySizeMethod;
        internal static MethodInfo GetArraySizeMethod
        {
            get
            {
                if (s_getArraySizeMethod is null)
                {
                    s_getArraySizeMethod = typeof(XmlObjectSerializerReadContext).GetMethod("GetArraySize", Globals.ScanAllMembers);
                    Debug.Assert(s_getArraySizeMethod is not null);
                }
                return s_getArraySizeMethod;
            }
        }

        private static MethodInfo? s_addNewObjectMethod;
        internal static MethodInfo AddNewObjectMethod
        {
            get
            {
                if (s_addNewObjectMethod is null)
                {
                    s_addNewObjectMethod = typeof(XmlObjectSerializerReadContext).GetMethod("AddNewObject", Globals.ScanAllMembers);
                    Debug.Assert(s_addNewObjectMethod is not null);
                }
                return s_addNewObjectMethod;
            }
        }

        private static MethodInfo? s_addNewObjectWithIdMethod;
        internal static MethodInfo AddNewObjectWithIdMethod
        {
            get
            {
                if (s_addNewObjectWithIdMethod is null)
                {
                    s_addNewObjectWithIdMethod = typeof(XmlObjectSerializerReadContext).GetMethod("AddNewObjectWithId", Globals.ScanAllMembers);
                    Debug.Assert(s_addNewObjectWithIdMethod is not null);
                }
                return s_addNewObjectWithIdMethod;
            }
        }

        private static MethodInfo? s_getExistingObjectMethod;
        internal static MethodInfo GetExistingObjectMethod
        {
            get
            {
                if (s_getExistingObjectMethod is null)
                {
                    s_getExistingObjectMethod = typeof(XmlObjectSerializerReadContext).GetMethod("GetExistingObject", Globals.ScanAllMembers);
                    Debug.Assert(s_getExistingObjectMethod is not null);
                }
                return s_getExistingObjectMethod;
            }
        }

        private static MethodInfo? s_getRealObjectMethod;
        internal static MethodInfo GetRealObjectMethod
        {
            get
            {
                if (s_getRealObjectMethod is null)
                {
                    s_getRealObjectMethod = typeof(XmlObjectSerializerReadContext).GetMethod("GetRealObject", Globals.ScanAllMembers);
                    Debug.Assert(s_getRealObjectMethod is not null);
                }
                return s_getRealObjectMethod;
            }
        }

        private static MethodInfo? s_ensureArraySizeMethod;
        internal static MethodInfo EnsureArraySizeMethod
        {
            get
            {
                if (s_ensureArraySizeMethod is null)
                {
                    s_ensureArraySizeMethod = typeof(XmlObjectSerializerReadContext).GetMethod("EnsureArraySize", Globals.ScanAllMembers);
                    Debug.Assert(s_ensureArraySizeMethod is not null);
                }
                return s_ensureArraySizeMethod;
            }
        }

        private static MethodInfo? s_trimArraySizeMethod;
        internal static MethodInfo TrimArraySizeMethod
        {
            get
            {
                if (s_trimArraySizeMethod is null)
                {
                    s_trimArraySizeMethod = typeof(XmlObjectSerializerReadContext).GetMethod("TrimArraySize", Globals.ScanAllMembers);
                    Debug.Assert(s_trimArraySizeMethod is not null);
                }
                return s_trimArraySizeMethod;
            }
        }

        private static MethodInfo? s_checkEndOfArrayMethod;
        internal static MethodInfo CheckEndOfArrayMethod
        {
            get
            {
                if (s_checkEndOfArrayMethod is null)
                {
                    s_checkEndOfArrayMethod = typeof(XmlObjectSerializerReadContext).GetMethod("CheckEndOfArray", Globals.ScanAllMembers);
                    Debug.Assert(s_checkEndOfArrayMethod is not null);
                }
                return s_checkEndOfArrayMethod;
            }
        }

        private static MethodInfo? s_getArrayLengthMethod;
        internal static MethodInfo GetArrayLengthMethod
        {
            get
            {
                if (s_getArrayLengthMethod is null)
                {
                    s_getArrayLengthMethod = Globals.TypeOfArray.GetProperty("Length")!.GetMethod;
                    Debug.Assert(s_getArrayLengthMethod is not null);
                }
                return s_getArrayLengthMethod;
            }
        }

        private static MethodInfo? s_createSerializationExceptionMethod;
        internal static MethodInfo CreateSerializationExceptionMethod
        {
            get
            {
                if (s_createSerializationExceptionMethod is null)
                {
                    s_createSerializationExceptionMethod = typeof(XmlObjectSerializerReadContext).GetMethod("CreateSerializationException", Globals.ScanAllMembers, null, new Type[] { typeof(string) }, null);
                    Debug.Assert(s_createSerializationExceptionMethod is not null);
                }
                return s_createSerializationExceptionMethod;
            }
        }

        private static MethodInfo? s_readSerializationInfoMethod;
        internal static MethodInfo ReadSerializationInfoMethod
        {
            get
            {
                if (s_readSerializationInfoMethod is null)
                {
                    s_readSerializationInfoMethod = typeof(XmlObjectSerializerReadContext).GetMethod("ReadSerializationInfo", Globals.ScanAllMembers);
                    Debug.Assert(s_readSerializationInfoMethod is not null);
                }
                return s_readSerializationInfoMethod;
            }
        }

        private static MethodInfo? s_createUnexpectedStateExceptionMethod;
        internal static MethodInfo CreateUnexpectedStateExceptionMethod
        {
            get
            {
                if (s_createUnexpectedStateExceptionMethod is null)
                {
                    s_createUnexpectedStateExceptionMethod = typeof(XmlObjectSerializerReadContext).GetMethod("CreateUnexpectedStateException", Globals.ScanAllMembers, null, new Type[] { typeof(XmlNodeType), typeof(XmlReaderDelegator) }, null);
                    Debug.Assert(s_createUnexpectedStateExceptionMethod is not null);
                }
                return s_createUnexpectedStateExceptionMethod;
            }
        }

        private static MethodInfo? s_internalSerializeReferenceMethod;
        internal static MethodInfo InternalSerializeReferenceMethod
        {
            get
            {
                if (s_internalSerializeReferenceMethod is null)
                {
                    s_internalSerializeReferenceMethod = typeof(XmlObjectSerializerWriteContext).GetMethod("InternalSerializeReference", Globals.ScanAllMembers);
                    Debug.Assert(s_internalSerializeReferenceMethod is not null);
                }
                return s_internalSerializeReferenceMethod;
            }
        }

        private static MethodInfo? s_internalSerializeMethod;
        internal static MethodInfo InternalSerializeMethod
        {
            get
            {
                if (s_internalSerializeMethod is null)
                {
                    s_internalSerializeMethod = typeof(XmlObjectSerializerWriteContext).GetMethod("InternalSerialize", Globals.ScanAllMembers);
                    Debug.Assert(s_internalSerializeMethod is not null);
                }
                return s_internalSerializeMethod;
            }
        }

        private static MethodInfo? s_writeNullMethod;
        internal static MethodInfo WriteNullMethod
        {
            get
            {
                if (s_writeNullMethod is null)
                {
                    s_writeNullMethod = typeof(XmlObjectSerializerWriteContext).GetMethod("WriteNull", Globals.ScanAllMembers, null, new Type[] { typeof(XmlWriterDelegator), typeof(Type), typeof(bool) }, null);
                    Debug.Assert(s_writeNullMethod is not null);
                }
                return s_writeNullMethod;
            }
        }

        private static MethodInfo? s_incrementArrayCountMethod;
        internal static MethodInfo IncrementArrayCountMethod
        {
            get
            {
                if (s_incrementArrayCountMethod is null)
                {
                    s_incrementArrayCountMethod = typeof(XmlObjectSerializerWriteContext).GetMethod("IncrementArrayCount", Globals.ScanAllMembers);
                    Debug.Assert(s_incrementArrayCountMethod is not null);
                }
                return s_incrementArrayCountMethod;
            }
        }

        private static MethodInfo? s_incrementCollectionCountMethod;
        internal static MethodInfo IncrementCollectionCountMethod
        {
            get
            {
                if (s_incrementCollectionCountMethod is null)
                {
                    s_incrementCollectionCountMethod = typeof(XmlObjectSerializerWriteContext).GetMethod("IncrementCollectionCount", Globals.ScanAllMembers, null, new Type[] { typeof(XmlWriterDelegator), typeof(ICollection) }, null);
                    Debug.Assert(s_incrementCollectionCountMethod is not null);
                }
                return s_incrementCollectionCountMethod;
            }
        }

        private static MethodInfo? s_incrementCollectionCountGenericMethod;
        internal static MethodInfo IncrementCollectionCountGenericMethod
        {
            get
            {
                if (s_incrementCollectionCountGenericMethod is null)
                {
                    s_incrementCollectionCountGenericMethod = typeof(XmlObjectSerializerWriteContext).GetMethod("IncrementCollectionCountGeneric", Globals.ScanAllMembers);
                    Debug.Assert(s_incrementCollectionCountGenericMethod is not null);
                }
                return s_incrementCollectionCountGenericMethod;
            }
        }

        private static MethodInfo? s_getDefaultValueMethod;
        internal static MethodInfo GetDefaultValueMethod
        {
            get
            {
                if (s_getDefaultValueMethod is null)
                {
                    s_getDefaultValueMethod = typeof(XmlObjectSerializerWriteContext).GetMethod(nameof(XmlObjectSerializerWriteContext.GetDefaultValue), Globals.ScanAllMembers);
                    Debug.Assert(s_getDefaultValueMethod is not null);
                }
                return s_getDefaultValueMethod;
            }
        }

        internal static object? GetDefaultValue(Type type)
        {
            return GetDefaultValueMethod.MakeGenericMethod(type).Invoke(null, Array.Empty<object>());
        }

        private static MethodInfo? s_getNullableValueMethod;
        internal static MethodInfo GetNullableValueMethod
        {
            get
            {
                if (s_getNullableValueMethod is null)
                {
                    s_getNullableValueMethod = typeof(XmlObjectSerializerWriteContext).GetMethod("GetNullableValue", Globals.ScanAllMembers);
                    Debug.Assert(s_getNullableValueMethod is not null);
                }
                return s_getNullableValueMethod;
            }
        }

        private static MethodInfo? s_throwRequiredMemberMustBeEmittedMethod;
        internal static MethodInfo ThrowRequiredMemberMustBeEmittedMethod
        {
            get
            {
                if (s_throwRequiredMemberMustBeEmittedMethod is null)
                {
                    s_throwRequiredMemberMustBeEmittedMethod = typeof(XmlObjectSerializerWriteContext).GetMethod("ThrowRequiredMemberMustBeEmitted", Globals.ScanAllMembers);
                    Debug.Assert(s_throwRequiredMemberMustBeEmittedMethod is not null);
                }
                return s_throwRequiredMemberMustBeEmittedMethod;
            }
        }

        private static MethodInfo? s_getHasValueMethod;
        internal static MethodInfo GetHasValueMethod
        {
            get
            {
                if (s_getHasValueMethod is null)
                {
                    s_getHasValueMethod = typeof(XmlObjectSerializerWriteContext).GetMethod("GetHasValue", Globals.ScanAllMembers);
                    Debug.Assert(s_getHasValueMethod is not null);
                }
                return s_getHasValueMethod;
            }
        }

        private static MethodInfo? s_writeISerializableMethod;
        internal static MethodInfo WriteISerializableMethod
        {
            get
            {
                if (s_writeISerializableMethod is null)
                {
                    s_writeISerializableMethod = typeof(XmlObjectSerializerWriteContext).GetMethod("WriteISerializable", Globals.ScanAllMembers);
                    Debug.Assert(s_writeISerializableMethod is not null);
                }
                return s_writeISerializableMethod;
            }
        }


        private static MethodInfo? s_isMemberTypeSameAsMemberValue;
        internal static MethodInfo IsMemberTypeSameAsMemberValue
        {
            get
            {
                if (s_isMemberTypeSameAsMemberValue is null)
                {
                    s_isMemberTypeSameAsMemberValue = typeof(XmlObjectSerializerWriteContext).GetMethod("IsMemberTypeSameAsMemberValue", Globals.ScanAllMembers, null, new Type[] { typeof(object), typeof(Type) }, null);
                    Debug.Assert(s_isMemberTypeSameAsMemberValue is not null);
                }
                return s_isMemberTypeSameAsMemberValue;
            }
        }

        private static MethodInfo? s_writeExtensionDataMethod;
        internal static MethodInfo WriteExtensionDataMethod => s_writeExtensionDataMethod ??
                                                               (s_writeExtensionDataMethod = typeof(XmlObjectSerializerWriteContext).GetMethod("WriteExtensionData", Globals.ScanAllMembers)!);

        private static MethodInfo? s_writeXmlValueMethod;
        internal static MethodInfo WriteXmlValueMethod
        {
            get
            {
                if (s_writeXmlValueMethod is null)
                {
                    s_writeXmlValueMethod = typeof(DataContract).GetMethod("WriteXmlValue", Globals.ScanAllMembers);
                    Debug.Assert(s_writeXmlValueMethod is not null);
                }
                return s_writeXmlValueMethod;
            }
        }

        private static MethodInfo? s_readXmlValueMethod;
        internal static MethodInfo ReadXmlValueMethod
        {
            get
            {
                if (s_readXmlValueMethod is null)
                {
                    s_readXmlValueMethod = typeof(DataContract).GetMethod("ReadXmlValue", Globals.ScanAllMembers);
                    Debug.Assert(s_readXmlValueMethod is not null);
                }
                return s_readXmlValueMethod;
            }
        }

        private static PropertyInfo? s_namespaceProperty;
        internal static PropertyInfo NamespaceProperty
        {
            get
            {
                if (s_namespaceProperty is null)
                {
                    s_namespaceProperty = typeof(DataContract).GetProperty("Namespace", Globals.ScanAllMembers);
                    Debug.Assert(s_namespaceProperty is not null);
                }
                return s_namespaceProperty;
            }
        }

        private static FieldInfo? s_contractNamespacesField;
        internal static FieldInfo ContractNamespacesField
        {
            get
            {
                if (s_contractNamespacesField is null)
                {
                    s_contractNamespacesField = typeof(ClassDataContract).GetField("ContractNamespaces", Globals.ScanAllMembers);
                    Debug.Assert(s_contractNamespacesField is not null);
                }
                return s_contractNamespacesField;
            }
        }

        private static FieldInfo? s_memberNamesField;
        internal static FieldInfo MemberNamesField
        {
            get
            {
                if (s_memberNamesField is null)
                {
                    s_memberNamesField = typeof(ClassDataContract).GetField("MemberNames", Globals.ScanAllMembers);
                    Debug.Assert(s_memberNamesField is not null);
                }
                return s_memberNamesField;
            }
        }

        private static MethodInfo? s_extensionDataSetExplicitMethodInfo;
        internal static MethodInfo ExtensionDataSetExplicitMethodInfo => s_extensionDataSetExplicitMethodInfo ??
                                                                         (s_extensionDataSetExplicitMethodInfo = typeof(IExtensibleDataObject).GetMethod(Globals.ExtensionDataSetMethod)!);

        private static PropertyInfo? s_childElementNamespacesProperty;
        internal static PropertyInfo ChildElementNamespacesProperty
        {
            get
            {
                if (s_childElementNamespacesProperty is null)
                {
                    s_childElementNamespacesProperty = typeof(ClassDataContract).GetProperty("ChildElementNamespaces", Globals.ScanAllMembers);
                    Debug.Assert(s_childElementNamespacesProperty is not null);
                }
                return s_childElementNamespacesProperty;
            }
        }

        private static PropertyInfo? s_collectionItemNameProperty;
        internal static PropertyInfo CollectionItemNameProperty
        {
            get
            {
                if (s_collectionItemNameProperty is null)
                {
                    s_collectionItemNameProperty = typeof(CollectionDataContract).GetProperty("CollectionItemName", Globals.ScanAllMembers);
                    Debug.Assert(s_collectionItemNameProperty is not null);
                }
                return s_collectionItemNameProperty;
            }
        }

        private static PropertyInfo? s_childElementNamespaceProperty;
        internal static PropertyInfo ChildElementNamespaceProperty
        {
            get
            {
                if (s_childElementNamespaceProperty is null)
                {
                    s_childElementNamespaceProperty = typeof(CollectionDataContract).GetProperty("ChildElementNamespace", Globals.ScanAllMembers);
                    Debug.Assert(s_childElementNamespaceProperty is not null);
                }
                return s_childElementNamespaceProperty;
            }
        }

        private static MethodInfo? s_getDateTimeOffsetMethod;
        internal static MethodInfo GetDateTimeOffsetMethod
        {
            get
            {
                if (s_getDateTimeOffsetMethod is null)
                {
                    s_getDateTimeOffsetMethod = typeof(DateTimeOffsetAdapter).GetMethod("GetDateTimeOffset", Globals.ScanAllMembers);
                    Debug.Assert(s_getDateTimeOffsetMethod is not null);
                }
                return s_getDateTimeOffsetMethod;
            }
        }

        private static MethodInfo? s_getDateTimeOffsetAdapterMethod;
        internal static MethodInfo GetDateTimeOffsetAdapterMethod
        {
            get
            {
                if (s_getDateTimeOffsetAdapterMethod is null)
                {
                    s_getDateTimeOffsetAdapterMethod = typeof(DateTimeOffsetAdapter).GetMethod("GetDateTimeOffsetAdapter", Globals.ScanAllMembers);
                    Debug.Assert(s_getDateTimeOffsetAdapterMethod is not null);
                }
                return s_getDateTimeOffsetAdapterMethod;
            }
        }

        private static MethodInfo? s_getTypeHandleMethod;
        internal static MethodInfo GetTypeHandleMethod
        {
            get
            {
                if (s_getTypeHandleMethod is null)
                {
                    s_getTypeHandleMethod = typeof(Type).GetMethod("get_TypeHandle");
                    Debug.Assert(s_getTypeHandleMethod is not null);
                }
                return s_getTypeHandleMethod;
            }
        }

        private static MethodInfo? s_getTypeMethod;
        internal static MethodInfo GetTypeMethod
        {
            get
            {
                if (s_getTypeMethod is null)
                {
                    s_getTypeMethod = typeof(object).GetMethod("GetType");
                    Debug.Assert(s_getTypeMethod is not null);
                }
                return s_getTypeMethod;
            }
        }

        private static MethodInfo? s_throwInvalidDataContractExceptionMethod;
        internal static MethodInfo ThrowInvalidDataContractExceptionMethod
        {
            get
            {
                if (s_throwInvalidDataContractExceptionMethod is null)
                {
                    s_throwInvalidDataContractExceptionMethod = typeof(DataContract).GetMethod("ThrowInvalidDataContractException", Globals.ScanAllMembers, null, new Type[] { typeof(string), typeof(Type) }, null);
                    Debug.Assert(s_throwInvalidDataContractExceptionMethod is not null);
                }
                return s_throwInvalidDataContractExceptionMethod;
            }
        }

        private static PropertyInfo? s_serializeReadOnlyTypesProperty;
        internal static PropertyInfo SerializeReadOnlyTypesProperty
        {
            get
            {
                if (s_serializeReadOnlyTypesProperty is null)
                {
                    s_serializeReadOnlyTypesProperty = typeof(XmlObjectSerializerWriteContext).GetProperty("SerializeReadOnlyTypes", Globals.ScanAllMembers);
                    Debug.Assert(s_serializeReadOnlyTypesProperty is not null);
                }
                return s_serializeReadOnlyTypesProperty;
            }
        }

        private static PropertyInfo? s_classSerializationExceptionMessageProperty;
        internal static PropertyInfo ClassSerializationExceptionMessageProperty
        {
            get
            {
                if (s_classSerializationExceptionMessageProperty is null)
                {
                    s_classSerializationExceptionMessageProperty = typeof(ClassDataContract).GetProperty("SerializationExceptionMessage", Globals.ScanAllMembers);
                    Debug.Assert(s_classSerializationExceptionMessageProperty is not null);
                }
                return s_classSerializationExceptionMessageProperty;
            }
        }

        private static PropertyInfo? s_collectionSerializationExceptionMessageProperty;
        internal static PropertyInfo CollectionSerializationExceptionMessageProperty
        {
            get
            {
                if (s_collectionSerializationExceptionMessageProperty is null)
                {
                    s_collectionSerializationExceptionMessageProperty = typeof(CollectionDataContract).GetProperty("SerializationExceptionMessage", Globals.ScanAllMembers);
                    Debug.Assert(s_collectionSerializationExceptionMessageProperty is not null);
                }
                return s_collectionSerializationExceptionMessageProperty;
            }
        }
    }
}
