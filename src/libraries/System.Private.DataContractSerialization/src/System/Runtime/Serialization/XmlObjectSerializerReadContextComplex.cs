// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace System.Runtime.Serialization
{
#if uapaot
    public class XmlObjectSerializerReadContextComplex : XmlObjectSerializerReadContext
#else
    internal class XmlObjectSerializerReadContextComplex : XmlObjectSerializerReadContext
#endif
    {
        private bool _preserveObjectReferences;
        private SerializationMode _mode;
        private ISerializationSurrogateProvider _serializationSurrogateProvider;

        internal XmlObjectSerializerReadContextComplex(DataContractSerializer serializer, DataContract rootTypeDataContract, DataContractResolver dataContractResolver)
            : base(serializer, rootTypeDataContract, dataContractResolver)
        {
            _mode = SerializationMode.SharedContract;
            _preserveObjectReferences = serializer.PreserveObjectReferences;
            _serializationSurrogateProvider = serializer.SerializationSurrogateProvider;
        }

        internal XmlObjectSerializerReadContextComplex(XmlObjectSerializer serializer, int maxItemsInObjectGraph, StreamingContext streamingContext, bool ignoreExtensionDataObject)
            : base(serializer, maxItemsInObjectGraph, streamingContext, ignoreExtensionDataObject)
        {
        }

        internal override SerializationMode Mode
        {
            get { return _mode; }
        }

        internal override object InternalDeserialize(XmlReaderDelegator xmlReader, int declaredTypeID, RuntimeTypeHandle declaredTypeHandle, string name, string ns)
        {
            if (_mode == SerializationMode.SharedContract)
            {
                if (_serializationSurrogateProvider == null)
                    return base.InternalDeserialize(xmlReader, declaredTypeID, declaredTypeHandle, name, ns);
                else
                    return InternalDeserializeWithSurrogate(xmlReader, Type.GetTypeFromHandle(declaredTypeHandle), null /*surrogateDataContract*/, name, ns);
            }
            else
            {
                return InternalDeserializeInSharedTypeMode(xmlReader, declaredTypeID, Type.GetTypeFromHandle(declaredTypeHandle), name, ns);
            }
        }

        internal override object InternalDeserialize(XmlReaderDelegator xmlReader, Type declaredType, string name, string ns)
        {
            if (_mode == SerializationMode.SharedContract)
            {
                if (_serializationSurrogateProvider == null)
                    return base.InternalDeserialize(xmlReader, declaredType, name, ns);
                else
                    return InternalDeserializeWithSurrogate(xmlReader, declaredType, null /*surrogateDataContract*/, name, ns);
            }
            else
            {
                return InternalDeserializeInSharedTypeMode(xmlReader, -1, declaredType, name, ns);
            }
        }

        internal override object InternalDeserialize(XmlReaderDelegator xmlReader, Type declaredType, DataContract dataContract, string name, string ns)
        {
            if (_mode == SerializationMode.SharedContract)
            {
                if (_serializationSurrogateProvider == null)
                    return base.InternalDeserialize(xmlReader, declaredType, dataContract, name, ns);
                else
                    return InternalDeserializeWithSurrogate(xmlReader, declaredType, dataContract, name, ns);
            }
            else
            {
                return InternalDeserializeInSharedTypeMode(xmlReader, -1, declaredType, name, ns);
            }
        }

        private object InternalDeserializeInSharedTypeMode(XmlReaderDelegator xmlReader, int declaredTypeID, Type declaredType, string name, string ns)
        {
            object retObj = null;
            if (TryHandleNullOrRef(xmlReader, declaredType, name, ns, ref retObj))
                return retObj;

            DataContract dataContract;
            string assemblyName = attributes.ClrAssembly;
            string typeName = attributes.ClrType;
            if (assemblyName != null && typeName != null)
            {
                Assembly assembly;
                Type type;
                dataContract = ResolveDataContractInSharedTypeMode(assemblyName, typeName, out assembly, out type);
                if (dataContract == null)
                {
                    if (assembly == null)
                        throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.AssemblyNotFound, assemblyName));
                    if (type == null)
                        throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.ClrTypeNotFound, assembly.FullName, typeName));
                }
                //Array covariance is not supported in XSD. If declared type is array, data is sent in format of base array
                if (declaredType != null && declaredType.IsArray)
                    dataContract = (declaredTypeID < 0) ? GetDataContract(declaredType) : GetDataContract(declaredTypeID, declaredType.TypeHandle);
            }
            else
            {
                if (assemblyName != null)
                    throw XmlObjectSerializer.CreateSerializationException(XmlObjectSerializer.TryAddLineInfo(xmlReader, SR.Format(SR.AttributeNotFound, Globals.SerializationNamespace, Globals.ClrTypeLocalName, xmlReader.NodeType, xmlReader.NamespaceURI, xmlReader.LocalName)));
                else if (typeName != null)
                    throw XmlObjectSerializer.CreateSerializationException(XmlObjectSerializer.TryAddLineInfo(xmlReader, SR.Format(SR.AttributeNotFound, Globals.SerializationNamespace, Globals.ClrAssemblyLocalName, xmlReader.NodeType, xmlReader.NamespaceURI, xmlReader.LocalName)));
                else if (declaredType == null)
                    throw XmlObjectSerializer.CreateSerializationException(XmlObjectSerializer.TryAddLineInfo(xmlReader, SR.Format(SR.AttributeNotFound, Globals.SerializationNamespace, Globals.ClrTypeLocalName, xmlReader.NodeType, xmlReader.NamespaceURI, xmlReader.LocalName)));
                dataContract = (declaredTypeID < 0) ? GetDataContract(declaredType) : GetDataContract(declaredTypeID, declaredType.TypeHandle);
            }
            return ReadDataContractValue(dataContract, xmlReader);
        }

        private object InternalDeserializeWithSurrogate(XmlReaderDelegator xmlReader, Type declaredType, DataContract surrogateDataContract, string name, string ns)
        {
            DataContract dataContract = surrogateDataContract ??
                GetDataContract(DataContractSurrogateCaller.GetDataContractType(_serializationSurrogateProvider, declaredType));
            if (this.IsGetOnlyCollection && dataContract.UnderlyingType != declaredType)
            {
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.SurrogatesWithGetOnlyCollectionsNotSupportedSerDeser, DataContract.GetClrTypeFullName(declaredType))));
            }
            ReadAttributes(xmlReader);
            string objectId = GetObjectId();
            object oldObj = InternalDeserialize(xmlReader, name, ns, ref dataContract);
            object obj = DataContractSurrogateCaller.GetDeserializedObject(_serializationSurrogateProvider, oldObj, dataContract.UnderlyingType, declaredType);
            ReplaceDeserializedObject(objectId, oldObj, obj);

            return obj;
        }

        private Type ResolveDataContractTypeInSharedTypeMode(string assemblyName, string typeName, out Assembly assembly)
        {
            // The method is used only when _mode == SerializationMode.SharedType.
            // _mode is set to SerializationMode.SharedType only when the context is for NetDataContractSerializer.
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_NetDataContractSerializer);
        }

        private DataContract ResolveDataContractInSharedTypeMode(string assemblyName, string typeName, out Assembly assembly, out Type type)
        {
            type = ResolveDataContractTypeInSharedTypeMode(assemblyName, typeName, out assembly);
            if (type != null)
            {
                return GetDataContract(type);
            }

            return null;
        }

        protected override DataContract ResolveDataContractFromTypeName()
        {
            if (_mode == SerializationMode.SharedContract)
            {
                return base.ResolveDataContractFromTypeName();
            }
            else
            {
                if (attributes.ClrAssembly != null && attributes.ClrType != null)
                {
                    Assembly assembly;
                    Type type;
                    return ResolveDataContractInSharedTypeMode(attributes.ClrAssembly, attributes.ClrType, out assembly, out type);
                }
            }
            return null;
        }

        internal override void CheckIfTypeSerializable(Type memberType, bool isMemberTypeSerializable)
        {
            if (_serializationSurrogateProvider != null)
            {
                while (memberType.IsArray)
                    memberType = memberType.GetElementType();
                memberType = DataContractSurrogateCaller.GetDataContractType(_serializationSurrogateProvider, memberType);
                if (!DataContract.IsTypeSerializable(memberType))
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.TypeNotSerializable, memberType)));
                return;
            }

            base.CheckIfTypeSerializable(memberType, isMemberTypeSerializable);
        }

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
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.SurrogatesWithGetOnlyCollectionsNotSupportedSerDeser,
                        DataContract.GetClrTypeFullName(type))));
                }
                else
                {
                    return surrogateType;
                }
            }
        }

#if USE_REFEMIT
        public override int GetArraySize()
#else
        internal override int GetArraySize()
#endif
        {
            return _preserveObjectReferences ? attributes.ArraySZSize : -1;
        }
    }
}
