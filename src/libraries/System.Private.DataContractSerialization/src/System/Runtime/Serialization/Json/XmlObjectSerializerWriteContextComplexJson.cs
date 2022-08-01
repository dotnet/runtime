// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Reflection;
using System.Collections;
using System.IO;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.Serialization.Json
{
    internal sealed class XmlObjectSerializerWriteContextComplexJson : XmlObjectSerializerWriteContextComplex
    {
        private readonly EmitTypeInformation _emitXsiType;
        private bool _perCallXsiTypeAlreadyEmitted;
        private readonly bool _useSimpleDictionaryFormat;

        internal static XmlObjectSerializerWriteContextComplexJson CreateContext(DataContractJsonSerializer serializer, DataContract rootTypeDataContract)
        {
            return new XmlObjectSerializerWriteContextComplexJson(serializer, rootTypeDataContract);
        }

        internal XmlObjectSerializerWriteContextComplexJson(DataContractJsonSerializer serializer, DataContract rootTypeDataContract)
            : base(serializer, serializer.MaxItemsInObjectGraph, new StreamingContext(StreamingContextStates.All), serializer.IgnoreExtensionDataObject)
        {
            _emitXsiType = serializer.EmitTypeInformation;
            this.rootTypeDataContract = rootTypeDataContract;
            this.serializerKnownTypeList = serializer.knownTypeList;
            this.serializeReadOnlyTypes = serializer.SerializeReadOnlyTypes;
            _useSimpleDictionaryFormat = serializer.UseSimpleDictionaryFormat;
        }

        internal IList<Type>? SerializerKnownTypeList
        {
            get
            {
                return this.serializerKnownTypeList;
            }
        }

        public bool UseSimpleDictionaryFormat
        {
            get
            {
                return _useSimpleDictionaryFormat;
            }
        }

        internal override void WriteArraySize(XmlWriterDelegator xmlWriter, int size)
        {
            //Noop
        }

        protected override void WriteTypeInfo(XmlWriterDelegator writer, string dataContractName, string? dataContractNamespace)
        {
            if (_emitXsiType != EmitTypeInformation.Never)
            {
                if (string.IsNullOrEmpty(dataContractNamespace))
                {
                    WriteTypeInfo(writer, dataContractName);
                }
                else
                {
                    WriteTypeInfo(writer, string.Concat(dataContractName, JsonGlobals.NameValueSeparatorString, TruncateDefaultDataContractNamespace(dataContractNamespace)));
                }
            }
        }

        internal static string TruncateDefaultDataContractNamespace(string dataContractNamespace)
        {
            if (!string.IsNullOrEmpty(dataContractNamespace))
            {
                if (dataContractNamespace[0] == '#')
                {
                    return string.Concat("\\", dataContractNamespace);
                }
                else if (dataContractNamespace[0] == '\\')
                {
                    return string.Concat("\\", dataContractNamespace);
                }
                else if (dataContractNamespace.StartsWith(Globals.DataContractXsdBaseNamespace, StringComparison.Ordinal))
                {
                    return string.Concat("#", dataContractNamespace.AsSpan(JsonGlobals.DataContractXsdBaseNamespaceLength));
                }
            }

            return dataContractNamespace;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected override bool WriteTypeInfo(XmlWriterDelegator writer, DataContract contract, DataContract declaredContract)
        {
            if (!((object.ReferenceEquals(contract.Name, declaredContract.Name) &&
                   object.ReferenceEquals(contract.Namespace, declaredContract.Namespace)) ||
                 (contract.Name.Value == declaredContract.Name.Value &&
                 contract.Namespace.Value == declaredContract.Namespace.Value)) &&
                 (contract.UnderlyingType != Globals.TypeOfObjectArray) &&
                 (_emitXsiType != EmitTypeInformation.Never))
            {
                // We always deserialize collections assigned to System.Object as object[]
                // Because of its common and JSON-specific nature,
                //    we don't want to validate known type information for object[]

                // Don't validate known type information when emitXsiType == Never because
                // known types are not used without type information in the JSON

                if (RequiresJsonTypeInfo(contract))
                {
                    _perCallXsiTypeAlreadyEmitted = true;
                    WriteTypeInfo(writer, contract.Name.Value, contract.Namespace.Value);
                }
                else
                {
                    // check if the declared type is System.Enum and throw because
                    // __type information cannot be written for enums since it results in invalid JSON.
                    // Without __type, the resulting JSON cannot be deserialized since a number cannot be directly assigned to System.Enum.
                    if (declaredContract.UnderlyingType == typeof(Enum))
                    {
                        throw new SerializationException(SR.Format(SR.EnumTypeNotSupportedByDataContractJsonSerializer, declaredContract.UnderlyingType));
                    }
                }

                // Return true regardless of whether we actually wrote __type information
                // E.g. We don't write __type information for enums, but we still want verifyKnownType
                //      to be true for them.
                return true;
            }
            return false;
        }

        private static bool RequiresJsonTypeInfo(DataContract contract)
        {
            return (contract is ClassDataContract);
        }

        private static void WriteTypeInfo(XmlWriterDelegator writer, string typeInformation)
        {
            writer.WriteAttributeString(null, JsonGlobals.serverTypeString, null, typeInformation);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected override void WriteDataContractValue(DataContract dataContract, XmlWriterDelegator xmlWriter, object obj, RuntimeTypeHandle declaredTypeHandle)
        {
            JsonDataContract jsonDataContract = JsonDataContract.GetJsonDataContract(dataContract);
            if (_emitXsiType == EmitTypeInformation.Always && !_perCallXsiTypeAlreadyEmitted && RequiresJsonTypeInfo(dataContract))
            {
                WriteTypeInfo(xmlWriter, jsonDataContract.TypeName!);
            }
            _perCallXsiTypeAlreadyEmitted = false;
            DataContractJsonSerializer.WriteJsonValue(jsonDataContract, xmlWriter, obj, this, declaredTypeHandle);
        }

        protected override void WriteNull(XmlWriterDelegator xmlWriter)
        {
            DataContractJsonSerializer.WriteJsonNull(xmlWriter);
        }

        internal static XmlDictionaryString CollectionItemName
        {
            get { return JsonGlobals.itemDictionaryString; }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected override void SerializeWithXsiType(XmlWriterDelegator xmlWriter, object obj, RuntimeTypeHandle objectTypeHandle, Type? objectType, int declaredTypeID, RuntimeTypeHandle declaredTypeHandle, Type declaredType)
        {
            DataContract dataContract;
            bool verifyKnownType = false;
            bool isDeclaredTypeInterface = declaredType.IsInterface;

            if (isDeclaredTypeInterface && CollectionDataContract.IsCollectionInterface(declaredType))
            {
                dataContract = GetDataContract(declaredTypeHandle, declaredType);
            }
            else if (declaredType.IsArray) // If declared type is array do not write __serverType. Instead write__serverType for each item
            {
                dataContract = GetDataContract(declaredTypeHandle, declaredType);
            }
            else
            {
                dataContract = GetDataContract(objectTypeHandle, objectType);
                DataContract declaredTypeContract = (declaredTypeID >= 0)
                    ? GetDataContract(declaredTypeID, declaredTypeHandle)
                    : GetDataContract(declaredTypeHandle, declaredType);
                verifyKnownType = WriteTypeInfo(xmlWriter, dataContract, declaredTypeContract);
                HandleCollectionAssignedToObject(declaredType, ref dataContract, ref obj, ref verifyKnownType);
            }

            if (isDeclaredTypeInterface)
            {
                VerifyObjectCompatibilityWithInterface(dataContract, obj, declaredType);
            }
            SerializeAndVerifyType(dataContract, xmlWriter, obj, verifyKnownType, declaredType.TypeHandle, declaredType);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private void HandleCollectionAssignedToObject(Type declaredType, ref DataContract dataContract, ref object obj, ref bool verifyKnownType)
        {
            if ((declaredType != dataContract.UnderlyingType) && (dataContract is CollectionDataContract))
            {
                if (verifyKnownType)
                {
                    VerifyType(dataContract, declaredType);
                    verifyKnownType = false;
                }
                if (((CollectionDataContract)dataContract).Kind == CollectionKind.Dictionary)
                {
                    // Convert non-generic dictionary to generic dictionary
                    IDictionary dictionaryObj = (obj as IDictionary)!;
                    Dictionary<object, object?> genericDictionaryObj = new Dictionary<object, object?>(dictionaryObj.Count);
                    // Manual use of IDictionaryEnumerator instead of foreach to avoid DictionaryEntry box allocations.
                    IDictionaryEnumerator e = dictionaryObj.GetEnumerator();
                    try
                    {
                        while (e.MoveNext())
                        {
                            DictionaryEntry entry = e.Entry;
                            genericDictionaryObj.Add(entry.Key, entry.Value);
                        }
                    }
                    finally
                    {
                        (e as IDisposable)?.Dispose();
                    }
                    obj = genericDictionaryObj;
                }
                dataContract = GetDataContract(Globals.TypeOfIEnumerable);
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override void SerializeWithXsiTypeAtTopLevel(DataContract dataContract, XmlWriterDelegator xmlWriter, object obj, RuntimeTypeHandle originalDeclaredTypeHandle, Type graphType)
        {
            bool verifyKnownType = false;
            Type declaredType = rootTypeDataContract!.UnderlyingType;
            bool isDeclaredTypeInterface = declaredType.IsInterface;

            if (!(isDeclaredTypeInterface && CollectionDataContract.IsCollectionInterface(declaredType))
                && !declaredType.IsArray)//Array covariance is not supported in XSD. If declared type is array do not write xsi:type. Instead write xsi:type for each item
            {
                verifyKnownType = WriteTypeInfo(xmlWriter, dataContract, rootTypeDataContract);
                HandleCollectionAssignedToObject(declaredType, ref dataContract, ref obj, ref verifyKnownType);
            }

            if (isDeclaredTypeInterface)
            {
                VerifyObjectCompatibilityWithInterface(dataContract, obj, declaredType);
            }
            SerializeAndVerifyType(dataContract, xmlWriter, obj, verifyKnownType, declaredType.TypeHandle, declaredType);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private void VerifyType(DataContract dataContract, Type declaredType)
        {
            bool knownTypesAddedInCurrentScope = false;
            if (dataContract.KnownDataContracts != null)
            {
                scopedKnownTypes.Push(dataContract.KnownDataContracts);
                knownTypesAddedInCurrentScope = true;
            }

            if (!IsKnownType(dataContract, declaredType))
            {
                throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.DcTypeNotFoundOnSerialize, DataContract.GetClrTypeFullName(dataContract.UnderlyingType), dataContract.StableName.Name, dataContract.StableName.Namespace));
            }

            if (knownTypesAddedInCurrentScope)
            {
                scopedKnownTypes.Pop();
            }
        }

        internal static void WriteJsonNameWithMapping(XmlWriterDelegator xmlWriter, XmlDictionaryString[] memberNames, int index)
        {
            xmlWriter.WriteStartElement("a", JsonGlobals.itemString, JsonGlobals.itemString);
            xmlWriter.WriteAttributeString(null, JsonGlobals.itemString, null, memberNames[index].Value);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override void WriteExtensionDataTypeInfo(XmlWriterDelegator xmlWriter, IDataNode dataNode)
        {
            Type dataType = dataNode.DataType;
            if (dataType == Globals.TypeOfClassDataNode ||
                dataType == Globals.TypeOfISerializableDataNode)
            {
                xmlWriter.WriteAttributeString(null, JsonGlobals.typeString, null, JsonGlobals.objectString);
                base.WriteExtensionDataTypeInfo(xmlWriter, dataNode);
            }
            else if (dataType == Globals.TypeOfCollectionDataNode)
            {
                xmlWriter.WriteAttributeString(null, JsonGlobals.typeString, null, JsonGlobals.arrayString);
                // Don't write __type for collections
            }
            else if (dataType == Globals.TypeOfXmlDataNode)
            {
                // Don't write type or __type for XML types because we serialize them to strings
            }
            else if ((dataType == Globals.TypeOfObject) && (dataNode.Value != null))
            {
                DataContract dc = GetDataContract(dataNode.Value.GetType());
                if (RequiresJsonTypeInfo(dc))
                {
                    base.WriteExtensionDataTypeInfo(xmlWriter, dataNode);
                }
            }
        }

        internal static void VerifyObjectCompatibilityWithInterface(DataContract contract, object graph, Type declaredType)
        {
            Type contractType = contract.GetType();
            if ((contractType == typeof(XmlDataContract)) && !Globals.TypeOfIXmlSerializable.IsAssignableFrom(declaredType))
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.XmlObjectAssignedToIncompatibleInterface, graph.GetType(), declaredType)));
            }

            if ((contractType == typeof(CollectionDataContract)) && !CollectionDataContract.IsCollectionInterface(declaredType))
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.CollectionAssignedToIncompatibleInterface, graph.GetType(), declaredType)));
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal void WriteJsonISerializable(XmlWriterDelegator xmlWriter, ISerializable obj)
        {
            Type objType = obj.GetType();
            var serInfo = new SerializationInfo(objType, XmlObjectSerializer.FormatterConverter);
            GetObjectData(obj, serInfo, GetStreamingContext());
            if (DataContract.GetClrTypeFullName(objType) != serInfo.FullTypeName)
            {
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.ChangingFullTypeNameNotSupported, serInfo.FullTypeName, DataContract.GetClrTypeFullName(objType))));
            }
            else
            {
                base.WriteSerializationInfo(xmlWriter, objType, serInfo);
            }
        }

        [return: NotNullIfNotNull(nameof(oldItemContract))]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static DataContract? GetRevisedItemContract(DataContract oldItemContract)
        {
            if ((oldItemContract != null) &&
                oldItemContract.UnderlyingType.IsGenericType &&
                (oldItemContract.UnderlyingType.GetGenericTypeDefinition() == Globals.TypeOfKeyValue))
            {
                return DataContract.GetDataContract(oldItemContract.UnderlyingType);
            }
            return oldItemContract;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override DataContract GetDataContract(RuntimeTypeHandle typeHandle, Type? type)
        {
            DataContract dataContract = base.GetDataContract(typeHandle, type);
            DataContractJsonSerializer.CheckIfTypeIsReference(dataContract);
            return dataContract;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override DataContract GetDataContractSkipValidation(int typeId, RuntimeTypeHandle typeHandle, Type? type)
        {
            DataContract dataContract = base.GetDataContractSkipValidation(typeId, typeHandle, type);
            DataContractJsonSerializer.CheckIfTypeIsReference(dataContract);
            return dataContract;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override DataContract GetDataContract(int id, RuntimeTypeHandle typeHandle)
        {
            DataContract dataContract = base.GetDataContract(id, typeHandle);
            DataContractJsonSerializer.CheckIfTypeIsReference(dataContract);
            return dataContract;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected override DataContract? ResolveDataContractFromRootDataContract(XmlQualifiedName typeQName)
        {
            return XmlObjectSerializerWriteContextComplexJson.ResolveJsonDataContractFromRootDataContract(this, typeQName, rootTypeDataContract!);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static DataContract? ResolveJsonDataContractFromRootDataContract(XmlObjectSerializerContext context, XmlQualifiedName typeQName, DataContract rootTypeDataContract)
        {
            if (rootTypeDataContract.StableName == typeQName)
                return rootTypeDataContract;

            CollectionDataContract? collectionContract = rootTypeDataContract as CollectionDataContract;
            while (collectionContract != null)
            {
                DataContract itemContract;
                if (collectionContract.ItemType.IsGenericType
                    && collectionContract.ItemType.GetGenericTypeDefinition() == typeof(KeyValue<,>))
                {
                    itemContract = context.GetDataContract(Globals.TypeOfKeyValuePair.MakeGenericType(collectionContract.ItemType.GetGenericArguments()));
                }
                else
                {
                    itemContract = context.GetDataContract(context.GetSurrogatedType(collectionContract.ItemType));
                }
                if (itemContract.StableName == typeQName)
                {
                    return itemContract;
                }
                collectionContract = itemContract as CollectionDataContract;
            }
            return null;
        }
    }
}
