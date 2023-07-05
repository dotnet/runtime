// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Serialization.DataContracts;

namespace System.Runtime.Serialization
{
    internal class XmlObjectSerializerReadContextComplex : XmlObjectSerializerReadContext
    {
        private readonly bool _preserveObjectReferences;
        private readonly ISerializationSurrogateProvider? _serializationSurrogateProvider;

        internal XmlObjectSerializerReadContextComplex(DataContractSerializer serializer, DataContract rootTypeDataContract, DataContractResolver? dataContractResolver)
            : base(serializer, rootTypeDataContract, dataContractResolver)
        {
            _preserveObjectReferences = serializer.PreserveObjectReferences;
            _serializationSurrogateProvider = serializer.SerializationSurrogateProvider;
        }

        internal XmlObjectSerializerReadContextComplex(XmlObjectSerializer serializer, int maxItemsInObjectGraph, StreamingContext streamingContext, bool ignoreExtensionDataObject)
            : base(serializer, maxItemsInObjectGraph, streamingContext, ignoreExtensionDataObject)
        {
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override object? InternalDeserialize(XmlReaderDelegator xmlReader, int declaredTypeID, RuntimeTypeHandle declaredTypeHandle, string name, string ns)
        {
            if (_serializationSurrogateProvider == null)
                return base.InternalDeserialize(xmlReader, declaredTypeID, declaredTypeHandle, name, ns);
            else
                return InternalDeserializeWithSurrogate(xmlReader, Type.GetTypeFromHandle(declaredTypeHandle)!, null /*surrogateDataContract*/, name, ns);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override object? InternalDeserialize(XmlReaderDelegator xmlReader, Type declaredType, string? name, string? ns)
        {
            if (_serializationSurrogateProvider == null)
                return base.InternalDeserialize(xmlReader, declaredType, name, ns);
            else
                return InternalDeserializeWithSurrogate(xmlReader, declaredType, null /*surrogateDataContract*/, name, ns);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override object? InternalDeserialize(XmlReaderDelegator xmlReader, Type declaredType, DataContract? dataContract, string? name, string? ns)
        {
            if (_serializationSurrogateProvider == null)
                return base.InternalDeserialize(xmlReader, declaredType, dataContract, name, ns);
            else
                return InternalDeserializeWithSurrogate(xmlReader, declaredType, dataContract, name, ns);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private object? InternalDeserializeWithSurrogate(XmlReaderDelegator xmlReader, Type declaredType, DataContract? surrogateDataContract, string? name, string? ns)
        {
            Debug.Assert(_serializationSurrogateProvider != null);

            DataContract dataContract = surrogateDataContract ??
                GetDataContract(DataContractSurrogateCaller.GetDataContractType(_serializationSurrogateProvider, declaredType));
            if (this.IsGetOnlyCollection && dataContract.UnderlyingType != declaredType)
            {
                throw new InvalidDataContractException(SR.Format(SR.SurrogatesWithGetOnlyCollectionsNotSupportedSerDeser, DataContract.GetClrTypeFullName(declaredType)));
            }
            ReadAttributes(xmlReader);
            string objectId = GetObjectId();
            object? oldObj = InternalDeserialize(xmlReader, name, ns, declaredType, ref dataContract);
            object? obj = DataContractSurrogateCaller.GetDeserializedObject(_serializationSurrogateProvider, oldObj, dataContract.UnderlyingType, declaredType);
            ReplaceDeserializedObject(objectId, oldObj, obj);

            return obj;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override void CheckIfTypeSerializable(Type memberType, bool isMemberTypeSerializable)
        {
            if (_serializationSurrogateProvider != null)
            {
                while (memberType.IsArray)
                    memberType = memberType.GetElementType()!;
                memberType = DataContractSurrogateCaller.GetDataContractType(_serializationSurrogateProvider, memberType);
                if (!DataContract.IsTypeSerializable(memberType))
                    throw new InvalidDataContractException(SR.Format(SR.TypeNotSerializable, memberType));
                return;
            }

            base.CheckIfTypeSerializable(memberType, isMemberTypeSerializable);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override Type GetSurrogatedType(Type type)
        {
            if (_serializationSurrogateProvider == null)
            {
                return base.GetSurrogatedType(type);
            }
            else
            {
                type = DataContract.UnwrapNullableType(type);
                Type surrogateType = DataContractSerializer.GetSurrogatedType(_serializationSurrogateProvider, type);
                if (this.IsGetOnlyCollection && surrogateType != type)
                {
                    throw new InvalidDataContractException(SR.Format(SR.SurrogatesWithGetOnlyCollectionsNotSupportedSerDeser,
                        DataContract.GetClrTypeFullName(type)));
                }
                else
                {
                    return surrogateType;
                }
            }
        }

        internal override int GetArraySize()
        {
            Debug.Assert(attributes != null);

            return _preserveObjectReferences ? attributes.ArraySZSize : -1;
        }
    }
}
