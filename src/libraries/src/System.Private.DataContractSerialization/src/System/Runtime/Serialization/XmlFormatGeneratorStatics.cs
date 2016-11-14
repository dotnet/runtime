// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Security;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;


namespace System.Runtime.Serialization
{
    /// <SecurityNote>
    /// Critical - Class holds static instances used for code generation during serialization. 
    ///            Static fields are marked SecurityCritical or readonly to prevent
    ///            data from being modified or leaked to other components in appdomain.
    /// Safe - All get-only properties marked safe since they only need to be protected for write.
    /// </SecurityNote>
    internal static class XmlFormatGeneratorStatics
    {
        [SecurityCritical]
        private static MethodInfo s_writeStartElementMethod2;
        internal static MethodInfo WriteStartElementMethod2
        {
            [SecuritySafeCritical]
            get
            {
                if (s_writeStartElementMethod2 == null)
                {
                    s_writeStartElementMethod2 = typeof(XmlWriterDelegator).GetMethod("WriteStartElement", Globals.ScanAllMembers, new Type[] { typeof(XmlDictionaryString), typeof(XmlDictionaryString) });
                    Debug.Assert(s_writeStartElementMethod2 != null);
                }
                return s_writeStartElementMethod2;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_writeStartElementMethod3;
        internal static MethodInfo WriteStartElementMethod3
        {
            [SecuritySafeCritical]
            get
            {
                if (s_writeStartElementMethod3 == null)
                {
                    s_writeStartElementMethod3 = typeof(XmlWriterDelegator).GetMethod("WriteStartElement", Globals.ScanAllMembers, new Type[] { typeof(string), typeof(XmlDictionaryString), typeof(XmlDictionaryString) });
                    Debug.Assert(s_writeStartElementMethod3 != null);
                }
                return s_writeStartElementMethod3;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_writeEndElementMethod;
        internal static MethodInfo WriteEndElementMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_writeEndElementMethod == null)
                {
                    s_writeEndElementMethod = typeof(XmlWriterDelegator).GetMethod("WriteEndElement", Globals.ScanAllMembers, Array.Empty<Type>());
                    Debug.Assert(s_writeEndElementMethod != null);
                }
                return s_writeEndElementMethod;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_writeNamespaceDeclMethod;
        internal static MethodInfo WriteNamespaceDeclMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_writeNamespaceDeclMethod == null)
                {
                    s_writeNamespaceDeclMethod = typeof(XmlWriterDelegator).GetMethod("WriteNamespaceDecl", Globals.ScanAllMembers, new Type[] { typeof(XmlDictionaryString) });
                    Debug.Assert(s_writeNamespaceDeclMethod != null);
                }
                return s_writeNamespaceDeclMethod;
            }
        }


        [SecurityCritical]
        private static ConstructorInfo s_dictionaryEnumeratorCtor;
        internal static ConstructorInfo DictionaryEnumeratorCtor
        {
            [SecuritySafeCritical]
            get
            {
                if (s_dictionaryEnumeratorCtor == null)
                    s_dictionaryEnumeratorCtor = Globals.TypeOfDictionaryEnumerator.GetConstructor(Globals.ScanAllMembers, new Type[] { Globals.TypeOfIDictionaryEnumerator });
                return s_dictionaryEnumeratorCtor;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_ienumeratorMoveNextMethod;
        internal static MethodInfo MoveNextMethod
        {
            [SecuritySafeCritical]
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

        [SecurityCritical]
        private static MethodInfo s_ienumeratorGetCurrentMethod;
        internal static MethodInfo GetCurrentMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_ienumeratorGetCurrentMethod == null)
                {
                    s_ienumeratorGetCurrentMethod = typeof(IEnumerator).GetProperty("Current").GetMethod;
                    Debug.Assert(s_ienumeratorGetCurrentMethod != null);
                }
                return s_ienumeratorGetCurrentMethod;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_getItemContractMethod;
        internal static MethodInfo GetItemContractMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_getItemContractMethod == null)
                {
                    s_getItemContractMethod = typeof(CollectionDataContract).GetProperty("ItemContract", Globals.ScanAllMembers).GetMethod;
                    Debug.Assert(s_getItemContractMethod != null);
                }
                return s_getItemContractMethod;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_isStartElementMethod2;
        internal static MethodInfo IsStartElementMethod2
        {
            [SecuritySafeCritical]
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

        [SecurityCritical]
        private static MethodInfo s_isStartElementMethod0;
        internal static MethodInfo IsStartElementMethod0
        {
            [SecuritySafeCritical]
            get
            {
                if (s_isStartElementMethod0 == null)
                {
                    s_isStartElementMethod0 = typeof(XmlReaderDelegator).GetMethod("IsStartElement", Globals.ScanAllMembers, Array.Empty<Type>());
                    Debug.Assert(s_isStartElementMethod0 != null);
                }
                return s_isStartElementMethod0;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_getUninitializedObjectMethod;
        internal static MethodInfo GetUninitializedObjectMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_getUninitializedObjectMethod == null)
                {
                    s_getUninitializedObjectMethod = typeof(XmlFormatReaderGenerator).GetMethod("UnsafeGetUninitializedObject", Globals.ScanAllMembers, new Type[] { typeof(int) });
                    Debug.Assert(s_getUninitializedObjectMethod != null);
                }
                return s_getUninitializedObjectMethod;
            }
        }

        [SecurityCritical]
        private static PropertyInfo s_nodeTypeProperty;
        internal static PropertyInfo NodeTypeProperty
        {
            [SecuritySafeCritical]
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

        [SecurityCritical]
        private static ConstructorInfo s_hashtableCtor;
        internal static ConstructorInfo HashtableCtor
        {
            [SecuritySafeCritical]
            get
            {
                if (s_hashtableCtor == null)
                    s_hashtableCtor = Globals.TypeOfHashtable.GetConstructor(Globals.ScanAllMembers, Array.Empty<Type>());
                return s_hashtableCtor;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_getStreamingContextMethod;
        internal static MethodInfo GetStreamingContextMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_getStreamingContextMethod == null)
                {
                    s_getStreamingContextMethod = typeof(XmlObjectSerializerContext).GetMethod("GetStreamingContext", Globals.ScanAllMembers);
                    Debug.Assert(s_getStreamingContextMethod != null);
                }
                return s_getStreamingContextMethod;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_getCollectionMemberMethod;
        internal static MethodInfo GetCollectionMemberMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_getCollectionMemberMethod == null)
                {
                    s_getCollectionMemberMethod = typeof(XmlObjectSerializerReadContext).GetMethod("GetCollectionMember", Globals.ScanAllMembers);
                    Debug.Assert(s_getCollectionMemberMethod != null);
                }
                return s_getCollectionMemberMethod;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_storeCollectionMemberInfoMethod;
        internal static MethodInfo StoreCollectionMemberInfoMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_storeCollectionMemberInfoMethod == null)
                {
                    s_storeCollectionMemberInfoMethod = typeof(XmlObjectSerializerReadContext).GetMethod("StoreCollectionMemberInfo", Globals.ScanAllMembers, new Type[] { typeof(object) });
                    Debug.Assert(s_storeCollectionMemberInfoMethod != null);
                }
                return s_storeCollectionMemberInfoMethod;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_storeIsGetOnlyCollectionMethod;
        internal static MethodInfo StoreIsGetOnlyCollectionMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_storeIsGetOnlyCollectionMethod == null)
                {
                    s_storeIsGetOnlyCollectionMethod = typeof(XmlObjectSerializerWriteContext).GetMethod("StoreIsGetOnlyCollection", Globals.ScanAllMembers);
                    Debug.Assert(s_storeIsGetOnlyCollectionMethod != null);
                }
                return s_storeIsGetOnlyCollectionMethod;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_throwNullValueReturnedForGetOnlyCollectionExceptionMethod;
        internal static MethodInfo ThrowNullValueReturnedForGetOnlyCollectionExceptionMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_throwNullValueReturnedForGetOnlyCollectionExceptionMethod == null)
                {
                    s_throwNullValueReturnedForGetOnlyCollectionExceptionMethod = typeof(XmlObjectSerializerReadContext).GetMethod("ThrowNullValueReturnedForGetOnlyCollectionException", Globals.ScanAllMembers);
                    Debug.Assert(s_throwNullValueReturnedForGetOnlyCollectionExceptionMethod != null);
                }
                return s_throwNullValueReturnedForGetOnlyCollectionExceptionMethod;
            }
        }

        private static MethodInfo s_throwArrayExceededSizeExceptionMethod;
        internal static MethodInfo ThrowArrayExceededSizeExceptionMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_throwArrayExceededSizeExceptionMethod == null)
                {
                    s_throwArrayExceededSizeExceptionMethod = typeof(XmlObjectSerializerReadContext).GetMethod("ThrowArrayExceededSizeException", Globals.ScanAllMembers);
                    Debug.Assert(s_throwArrayExceededSizeExceptionMethod != null);
                }
                return s_throwArrayExceededSizeExceptionMethod;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_incrementItemCountMethod;
        internal static MethodInfo IncrementItemCountMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_incrementItemCountMethod == null)
                {
                    s_incrementItemCountMethod = typeof(XmlObjectSerializerContext).GetMethod("IncrementItemCount", Globals.ScanAllMembers);
                    Debug.Assert(s_incrementItemCountMethod != null);
                }
                return s_incrementItemCountMethod;
            }
        }


        [SecurityCritical]
        private static MethodInfo s_internalDeserializeMethod;
        internal static MethodInfo InternalDeserializeMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_internalDeserializeMethod == null)
                {
                    s_internalDeserializeMethod = typeof(XmlObjectSerializerReadContext).GetMethod("InternalDeserialize", Globals.ScanAllMembers, new Type[] { typeof(XmlReaderDelegator), typeof(int), typeof(RuntimeTypeHandle), typeof(string), typeof(string) });
                    Debug.Assert(s_internalDeserializeMethod != null);
                }
                return s_internalDeserializeMethod;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_moveToNextElementMethod;
        internal static MethodInfo MoveToNextElementMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_moveToNextElementMethod == null)
                {
                    s_moveToNextElementMethod = typeof(XmlObjectSerializerReadContext).GetMethod("MoveToNextElement", Globals.ScanAllMembers);
                    Debug.Assert(s_moveToNextElementMethod != null);
                }
                return s_moveToNextElementMethod;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_getMemberIndexMethod;
        internal static MethodInfo GetMemberIndexMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_getMemberIndexMethod == null)
                {
                    s_getMemberIndexMethod = typeof(XmlObjectSerializerReadContext).GetMethod("GetMemberIndex", Globals.ScanAllMembers);
                    Debug.Assert(s_getMemberIndexMethod != null);
                }
                return s_getMemberIndexMethod;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_getMemberIndexWithRequiredMembersMethod;
        internal static MethodInfo GetMemberIndexWithRequiredMembersMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_getMemberIndexWithRequiredMembersMethod == null)
                {
                    s_getMemberIndexWithRequiredMembersMethod = typeof(XmlObjectSerializerReadContext).GetMethod("GetMemberIndexWithRequiredMembers", Globals.ScanAllMembers);
                    Debug.Assert(s_getMemberIndexWithRequiredMembersMethod != null);
                }
                return s_getMemberIndexWithRequiredMembersMethod;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_throwRequiredMemberMissingExceptionMethod;
        internal static MethodInfo ThrowRequiredMemberMissingExceptionMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_throwRequiredMemberMissingExceptionMethod == null)
                {
                    s_throwRequiredMemberMissingExceptionMethod = typeof(XmlObjectSerializerReadContext).GetMethod("ThrowRequiredMemberMissingException", Globals.ScanAllMembers);
                    Debug.Assert(s_throwRequiredMemberMissingExceptionMethod != null);
                }
                return s_throwRequiredMemberMissingExceptionMethod;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_skipUnknownElementMethod;
        internal static MethodInfo SkipUnknownElementMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_skipUnknownElementMethod == null)
                {
                    s_skipUnknownElementMethod = typeof(XmlObjectSerializerReadContext).GetMethod("SkipUnknownElement", Globals.ScanAllMembers);
                    Debug.Assert(s_skipUnknownElementMethod != null);
                }
                return s_skipUnknownElementMethod;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_readIfNullOrRefMethod;
        internal static MethodInfo ReadIfNullOrRefMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_readIfNullOrRefMethod == null)
                {
                    s_readIfNullOrRefMethod = typeof(XmlObjectSerializerReadContext).GetMethod("ReadIfNullOrRef", Globals.ScanAllMembers, new Type[] { typeof(XmlReaderDelegator), typeof(Type), typeof(bool) });
                    Debug.Assert(s_readIfNullOrRefMethod != null);
                }
                return s_readIfNullOrRefMethod;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_readAttributesMethod;
        internal static MethodInfo ReadAttributesMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_readAttributesMethod == null)
                {
                    s_readAttributesMethod = typeof(XmlObjectSerializerReadContext).GetMethod("ReadAttributes", Globals.ScanAllMembers);
                    Debug.Assert(s_readAttributesMethod != null);
                }
                return s_readAttributesMethod;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_resetAttributesMethod;
        internal static MethodInfo ResetAttributesMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_resetAttributesMethod == null)
                {
                    s_resetAttributesMethod = typeof(XmlObjectSerializerReadContext).GetMethod("ResetAttributes", Globals.ScanAllMembers);
                    Debug.Assert(s_resetAttributesMethod != null);
                }
                return s_resetAttributesMethod;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_getObjectIdMethod;
        internal static MethodInfo GetObjectIdMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_getObjectIdMethod == null)
                {
                    s_getObjectIdMethod = typeof(XmlObjectSerializerReadContext).GetMethod("GetObjectId", Globals.ScanAllMembers);
                    Debug.Assert(s_getObjectIdMethod != null);
                }
                return s_getObjectIdMethod;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_getArraySizeMethod;
        internal static MethodInfo GetArraySizeMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_getArraySizeMethod == null)
                {
                    s_getArraySizeMethod = typeof(XmlObjectSerializerReadContext).GetMethod("GetArraySize", Globals.ScanAllMembers);
                    Debug.Assert(s_getArraySizeMethod != null);
                }
                return s_getArraySizeMethod;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_addNewObjectMethod;
        internal static MethodInfo AddNewObjectMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_addNewObjectMethod == null)
                {
                    s_addNewObjectMethod = typeof(XmlObjectSerializerReadContext).GetMethod("AddNewObject", Globals.ScanAllMembers);
                    Debug.Assert(s_addNewObjectMethod != null);
                }
                return s_addNewObjectMethod;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_addNewObjectWithIdMethod;
        internal static MethodInfo AddNewObjectWithIdMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_addNewObjectWithIdMethod == null)
                {
                    s_addNewObjectWithIdMethod = typeof(XmlObjectSerializerReadContext).GetMethod("AddNewObjectWithId", Globals.ScanAllMembers);
                    Debug.Assert(s_addNewObjectWithIdMethod != null);
                }
                return s_addNewObjectWithIdMethod;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_getExistingObjectMethod;
        internal static MethodInfo GetExistingObjectMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_getExistingObjectMethod == null)
                {
                    s_getExistingObjectMethod = typeof(XmlObjectSerializerReadContext).GetMethod("GetExistingObject", Globals.ScanAllMembers);
                    Debug.Assert(s_getExistingObjectMethod != null);
                }
                return s_getExistingObjectMethod;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_ensureArraySizeMethod;
        internal static MethodInfo EnsureArraySizeMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_ensureArraySizeMethod == null)
                {
                    s_ensureArraySizeMethod = typeof(XmlObjectSerializerReadContext).GetMethod("EnsureArraySize", Globals.ScanAllMembers);
                    Debug.Assert(s_ensureArraySizeMethod != null);
                }
                return s_ensureArraySizeMethod;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_trimArraySizeMethod;
        internal static MethodInfo TrimArraySizeMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_trimArraySizeMethod == null)
                {
                    s_trimArraySizeMethod = typeof(XmlObjectSerializerReadContext).GetMethod("TrimArraySize", Globals.ScanAllMembers);
                    Debug.Assert(s_trimArraySizeMethod != null);
                }
                return s_trimArraySizeMethod;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_checkEndOfArrayMethod;
        internal static MethodInfo CheckEndOfArrayMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_checkEndOfArrayMethod == null)
                {
                    s_checkEndOfArrayMethod = typeof(XmlObjectSerializerReadContext).GetMethod("CheckEndOfArray", Globals.ScanAllMembers);
                    Debug.Assert(s_checkEndOfArrayMethod != null);
                }
                return s_checkEndOfArrayMethod;
            }
        }


        [SecurityCritical]
        private static MethodInfo s_getArrayLengthMethod;
        internal static MethodInfo GetArrayLengthMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_getArrayLengthMethod == null)
                {
                    s_getArrayLengthMethod = Globals.TypeOfArray.GetProperty("Length").GetMethod;
                    Debug.Assert(s_getArrayLengthMethod != null);
                }
                return s_getArrayLengthMethod;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_createSerializationExceptionMethod;
        internal static MethodInfo CreateSerializationExceptionMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_createSerializationExceptionMethod == null)
                {
                    s_createSerializationExceptionMethod = typeof(XmlObjectSerializerReadContext).GetMethod("CreateSerializationException", Globals.ScanAllMembers, new Type[] { typeof(string) });
                    Debug.Assert(s_createSerializationExceptionMethod != null);
                }
                return s_createSerializationExceptionMethod;
            }
        }

        private static MethodInfo s_readSerializationInfoMethod;
        internal static MethodInfo ReadSerializationInfoMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_readSerializationInfoMethod == null)
                    s_readSerializationInfoMethod = typeof(XmlObjectSerializerReadContext).GetMethod("ReadSerializationInfo", Globals.ScanAllMembers);
                return s_readSerializationInfoMethod;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_createUnexpectedStateExceptionMethod;
        internal static MethodInfo CreateUnexpectedStateExceptionMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_createUnexpectedStateExceptionMethod == null)
                {
                    s_createUnexpectedStateExceptionMethod = typeof(XmlObjectSerializerReadContext).GetMethod("CreateUnexpectedStateException", Globals.ScanAllMembers, new Type[] { typeof(XmlNodeType), typeof(XmlReaderDelegator) });
                    Debug.Assert(s_createUnexpectedStateExceptionMethod != null);
                }
                return s_createUnexpectedStateExceptionMethod;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_internalSerializeReferenceMethod;
        internal static MethodInfo InternalSerializeReferenceMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_internalSerializeReferenceMethod == null)
                {
                    s_internalSerializeReferenceMethod = typeof(XmlObjectSerializerWriteContext).GetMethod("InternalSerializeReference", Globals.ScanAllMembers);
                    Debug.Assert(s_internalSerializeReferenceMethod != null);
                }
                return s_internalSerializeReferenceMethod;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_internalSerializeMethod;
        internal static MethodInfo InternalSerializeMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_internalSerializeMethod == null)
                {
                    s_internalSerializeMethod = typeof(XmlObjectSerializerWriteContext).GetMethod("InternalSerialize", Globals.ScanAllMembers);
                    Debug.Assert(s_internalSerializeMethod != null);
                }
                return s_internalSerializeMethod;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_writeNullMethod;
        internal static MethodInfo WriteNullMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_writeNullMethod == null)
                {
                    s_writeNullMethod = typeof(XmlObjectSerializerWriteContext).GetMethod("WriteNull", Globals.ScanAllMembers, new Type[] { typeof(XmlWriterDelegator), typeof(Type), typeof(bool) });
                    Debug.Assert(s_writeNullMethod != null);
                }
                return s_writeNullMethod;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_incrementArrayCountMethod;
        internal static MethodInfo IncrementArrayCountMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_incrementArrayCountMethod == null)
                {
                    s_incrementArrayCountMethod = typeof(XmlObjectSerializerWriteContext).GetMethod("IncrementArrayCount", Globals.ScanAllMembers);
                    Debug.Assert(s_incrementArrayCountMethod != null);
                }
                return s_incrementArrayCountMethod;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_incrementCollectionCountMethod;
        internal static MethodInfo IncrementCollectionCountMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_incrementCollectionCountMethod == null)
                {
                    s_incrementCollectionCountMethod = typeof(XmlObjectSerializerWriteContext).GetMethod("IncrementCollectionCount", Globals.ScanAllMembers, new Type[] { typeof(XmlWriterDelegator), typeof(ICollection) });
                    Debug.Assert(s_incrementCollectionCountMethod != null);
                }
                return s_incrementCollectionCountMethod;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_incrementCollectionCountGenericMethod;
        internal static MethodInfo IncrementCollectionCountGenericMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_incrementCollectionCountGenericMethod == null)
                {
                    s_incrementCollectionCountGenericMethod = typeof(XmlObjectSerializerWriteContext).GetMethod("IncrementCollectionCountGeneric", Globals.ScanAllMembers);
                    Debug.Assert(s_incrementCollectionCountGenericMethod != null);
                }
                return s_incrementCollectionCountGenericMethod;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_getDefaultValueMethod;
        internal static MethodInfo GetDefaultValueMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_getDefaultValueMethod == null)
                {
                    s_getDefaultValueMethod = typeof(XmlObjectSerializerWriteContext).GetMethod(nameof(XmlObjectSerializerWriteContext.GetDefaultValue), Globals.ScanAllMembers);
                    Debug.Assert(s_getDefaultValueMethod != null);
                }
                return s_getDefaultValueMethod;
            }
        }

        internal static object GetDefaultValue(Type type)
        {
            return GetDefaultValueMethod.MakeGenericMethod(type).Invoke(null, Array.Empty<object>());
        }

        [SecurityCritical]
        private static MethodInfo s_getNullableValueMethod;
        internal static MethodInfo GetNullableValueMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_getNullableValueMethod == null)
                {
                    s_getNullableValueMethod = typeof(XmlObjectSerializerWriteContext).GetMethod("GetNullableValue", Globals.ScanAllMembers);
                    Debug.Assert(s_getNullableValueMethod != null);
                }
                return s_getNullableValueMethod;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_throwRequiredMemberMustBeEmittedMethod;
        internal static MethodInfo ThrowRequiredMemberMustBeEmittedMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_throwRequiredMemberMustBeEmittedMethod == null)
                {
                    s_throwRequiredMemberMustBeEmittedMethod = typeof(XmlObjectSerializerWriteContext).GetMethod("ThrowRequiredMemberMustBeEmitted", Globals.ScanAllMembers);
                    Debug.Assert(s_throwRequiredMemberMustBeEmittedMethod != null);
                }
                return s_throwRequiredMemberMustBeEmittedMethod;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_getHasValueMethod;
        internal static MethodInfo GetHasValueMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_getHasValueMethod == null)
                {
                    s_getHasValueMethod = typeof(XmlObjectSerializerWriteContext).GetMethod("GetHasValue", Globals.ScanAllMembers);
                    Debug.Assert(s_getHasValueMethod != null);
                }
                return s_getHasValueMethod;
            }
        }

        private static MethodInfo s_writeISerializableMethod;
        internal static MethodInfo WriteISerializableMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_writeISerializableMethod == null)
                    s_writeISerializableMethod = typeof(XmlObjectSerializerWriteContext).GetMethod("WriteISerializable", Globals.ScanAllMembers);
                return s_writeISerializableMethod;
            }
        }


        [SecurityCritical]
        private static MethodInfo s_isMemberTypeSameAsMemberValue;
        internal static MethodInfo IsMemberTypeSameAsMemberValue
        {
            [SecuritySafeCritical]
            get
            {
                if (s_isMemberTypeSameAsMemberValue == null)
                {
                    s_isMemberTypeSameAsMemberValue = typeof(XmlObjectSerializerWriteContext).GetMethod("IsMemberTypeSameAsMemberValue", Globals.ScanAllMembers, new Type[] { typeof(object), typeof(Type) });
                    Debug.Assert(s_isMemberTypeSameAsMemberValue != null);
                }
                return s_isMemberTypeSameAsMemberValue;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_writeXmlValueMethod;
        internal static MethodInfo WriteXmlValueMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_writeXmlValueMethod == null)
                {
                    s_writeXmlValueMethod = typeof(DataContract).GetMethod("WriteXmlValue", Globals.ScanAllMembers);
                    Debug.Assert(s_writeXmlValueMethod != null);
                }
                return s_writeXmlValueMethod;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_readXmlValueMethod;
        internal static MethodInfo ReadXmlValueMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_readXmlValueMethod == null)
                {
                    s_readXmlValueMethod = typeof(DataContract).GetMethod("ReadXmlValue", Globals.ScanAllMembers);
                    Debug.Assert(s_readXmlValueMethod != null);
                }
                return s_readXmlValueMethod;
            }
        }

        [SecurityCritical]
        private static PropertyInfo s_namespaceProperty;
        internal static PropertyInfo NamespaceProperty
        {
            [SecuritySafeCritical]
            get
            {
                if (s_namespaceProperty == null)
                {
                    s_namespaceProperty = typeof(DataContract).GetProperty("Namespace", Globals.ScanAllMembers);
                    Debug.Assert(s_namespaceProperty != null);
                }
                return s_namespaceProperty;
            }
        }

        [SecurityCritical]
        private static FieldInfo s_contractNamespacesField;
        internal static FieldInfo ContractNamespacesField
        {
            [SecuritySafeCritical]
            get
            {
                if (s_contractNamespacesField == null)
                {
                    s_contractNamespacesField = typeof(ClassDataContract).GetField("ContractNamespaces", Globals.ScanAllMembers);
                    Debug.Assert(s_contractNamespacesField != null);
                }
                return s_contractNamespacesField;
            }
        }

        [SecurityCritical]
        private static FieldInfo s_memberNamesField;
        internal static FieldInfo MemberNamesField
        {
            [SecuritySafeCritical]
            get
            {
                if (s_memberNamesField == null)
                {
                    s_memberNamesField = typeof(ClassDataContract).GetField("MemberNames", Globals.ScanAllMembers);
                    Debug.Assert(s_memberNamesField != null);
                }
                return s_memberNamesField;
            }
        }

        [SecurityCritical]
        private static PropertyInfo s_childElementNamespacesProperty;
        internal static PropertyInfo ChildElementNamespacesProperty
        {
            [SecuritySafeCritical]
            get
            {
                if (s_childElementNamespacesProperty == null)
                {
                    s_childElementNamespacesProperty = typeof(ClassDataContract).GetProperty("ChildElementNamespaces", Globals.ScanAllMembers);
                    Debug.Assert(s_childElementNamespacesProperty != null);
                }
                return s_childElementNamespacesProperty;
            }
        }

        [SecurityCritical]
        private static PropertyInfo s_collectionItemNameProperty;
        internal static PropertyInfo CollectionItemNameProperty
        {
            [SecuritySafeCritical]
            get
            {
                if (s_collectionItemNameProperty == null)
                {
                    s_collectionItemNameProperty = typeof(CollectionDataContract).GetProperty("CollectionItemName", Globals.ScanAllMembers);
                    Debug.Assert(s_collectionItemNameProperty != null);
                }
                return s_collectionItemNameProperty;
            }
        }

        [SecurityCritical]
        private static PropertyInfo s_childElementNamespaceProperty;
        internal static PropertyInfo ChildElementNamespaceProperty
        {
            [SecuritySafeCritical]
            get
            {
                if (s_childElementNamespaceProperty == null)
                {
                    s_childElementNamespaceProperty = typeof(CollectionDataContract).GetProperty("ChildElementNamespace", Globals.ScanAllMembers);
                    Debug.Assert(s_childElementNamespaceProperty != null);
                }
                return s_childElementNamespaceProperty;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_getDateTimeOffsetMethod;
        internal static MethodInfo GetDateTimeOffsetMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_getDateTimeOffsetMethod == null)
                {
                    s_getDateTimeOffsetMethod = typeof(DateTimeOffsetAdapter).GetMethod("GetDateTimeOffset", Globals.ScanAllMembers);
                    Debug.Assert(s_getDateTimeOffsetMethod != null);
                }
                return s_getDateTimeOffsetMethod;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_getDateTimeOffsetAdapterMethod;
        internal static MethodInfo GetDateTimeOffsetAdapterMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_getDateTimeOffsetAdapterMethod == null)
                {
                    s_getDateTimeOffsetAdapterMethod = typeof(DateTimeOffsetAdapter).GetMethod("GetDateTimeOffsetAdapter", Globals.ScanAllMembers);
                    Debug.Assert(s_getDateTimeOffsetAdapterMethod != null);
                }
                return s_getDateTimeOffsetAdapterMethod;
            }
        }

#if !NET_NATIVE
        private static MethodInfo s_getTypeHandleMethod;
        internal static MethodInfo GetTypeHandleMethod
        {
            get
            {
                if (s_getTypeHandleMethod == null)
                {
                    s_getTypeHandleMethod = typeof(Type).GetMethod("get_TypeHandle");
                    Debug.Assert(s_getTypeHandleMethod != null);
                }
                return s_getTypeHandleMethod;
            }
        }

        private static MethodInfo s_getTypeMethod;
        internal static MethodInfo GetTypeMethod
        {
            get
            {
                if (s_getTypeMethod == null)
                {
                    s_getTypeMethod = typeof(object).GetMethod("GetType");
                    Debug.Assert(s_getTypeMethod != null);
                }
                return s_getTypeMethod;
            }
        }

        [SecurityCritical]
        private static MethodInfo s_throwInvalidDataContractExceptionMethod;
        internal static MethodInfo ThrowInvalidDataContractExceptionMethod
        {
            [SecuritySafeCritical]
            get
            {
                if (s_throwInvalidDataContractExceptionMethod == null)
                {
                    s_throwInvalidDataContractExceptionMethod = typeof(DataContract).GetMethod("ThrowInvalidDataContractException", Globals.ScanAllMembers, new Type[] { typeof(string), typeof(Type) });
                    Debug.Assert(s_throwInvalidDataContractExceptionMethod != null);
                }
                return s_throwInvalidDataContractExceptionMethod;
            }
        }

        [SecurityCritical]
        private static PropertyInfo s_serializeReadOnlyTypesProperty;
        internal static PropertyInfo SerializeReadOnlyTypesProperty
        {
            [SecuritySafeCritical]
            get
            {
                if (s_serializeReadOnlyTypesProperty == null)
                {
                    s_serializeReadOnlyTypesProperty = typeof(XmlObjectSerializerWriteContext).GetProperty("SerializeReadOnlyTypes", Globals.ScanAllMembers);
                    Debug.Assert(s_serializeReadOnlyTypesProperty != null);
                }
                return s_serializeReadOnlyTypesProperty;
            }
        }

        [SecurityCritical]
        private static PropertyInfo s_classSerializationExceptionMessageProperty;
        internal static PropertyInfo ClassSerializationExceptionMessageProperty
        {
            [SecuritySafeCritical]
            get
            {
                if (s_classSerializationExceptionMessageProperty == null)
                {
                    s_classSerializationExceptionMessageProperty = typeof(ClassDataContract).GetProperty("SerializationExceptionMessage", Globals.ScanAllMembers);
                    Debug.Assert(s_classSerializationExceptionMessageProperty != null);
                }
                return s_classSerializationExceptionMessageProperty;
            }
        }

        [SecurityCritical]
        private static PropertyInfo s_collectionSerializationExceptionMessageProperty;
        internal static PropertyInfo CollectionSerializationExceptionMessageProperty
        {
            [SecuritySafeCritical]
            get
            {
                if (s_collectionSerializationExceptionMessageProperty == null)
                {
                    s_collectionSerializationExceptionMessageProperty = typeof(CollectionDataContract).GetProperty("SerializationExceptionMessage", Globals.ScanAllMembers);
                    Debug.Assert(s_collectionSerializationExceptionMessageProperty != null);
                }
                return s_collectionSerializationExceptionMessageProperty;
            }
        }
#endif
    }
}
