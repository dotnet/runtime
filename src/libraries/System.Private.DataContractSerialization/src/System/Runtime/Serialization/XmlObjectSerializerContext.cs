// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;
        using System.Security;
    using System.Xml;
    using DataContractDictionary = System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, DataContract>;

    internal class XmlObjectSerializerContext
    {
        protected XmlObjectSerializer serializer;
        protected DataContract? rootTypeDataContract;
        internal ScopedKnownTypes scopedKnownTypes;
        protected DataContractDictionary? serializerKnownDataContracts;
        private bool _isSerializerKnownDataContractsSetExplicit;
        protected IList<Type>? serializerKnownTypeList;
        private int _itemCount;
        private readonly int _maxItemsInObjectGraph;
        private readonly StreamingContext _streamingContext;
        private readonly bool _ignoreExtensionDataObject;
        private readonly DataContractResolver? _dataContractResolver;
        private KnownTypeDataContractResolver? _knownTypeResolver;

        internal XmlObjectSerializerContext(XmlObjectSerializer serializer, int maxItemsInObjectGraph, StreamingContext streamingContext, bool ignoreExtensionDataObject,
                                            DataContractResolver? dataContractResolver)
        {
            this.serializer = serializer;
            _itemCount = 1;
            _maxItemsInObjectGraph = maxItemsInObjectGraph;
            _streamingContext = streamingContext;
            _ignoreExtensionDataObject = ignoreExtensionDataObject;
            _dataContractResolver = dataContractResolver;
        }

        internal XmlObjectSerializerContext(XmlObjectSerializer serializer, int maxItemsInObjectGraph, StreamingContext streamingContext, bool ignoreExtensionDataObject)
            : this(serializer, maxItemsInObjectGraph, streamingContext, ignoreExtensionDataObject, null)
        {
        }

        internal XmlObjectSerializerContext(DataContractSerializer serializer, DataContract rootTypeDataContract, DataContractResolver? dataContractResolver)
            : this(serializer,
            serializer.MaxItemsInObjectGraph,
            default(StreamingContext),
            serializer.IgnoreExtensionDataObject,
            dataContractResolver
            )
        {
            this.rootTypeDataContract = rootTypeDataContract;
            this.serializerKnownTypeList = serializer.knownTypeList;
        }


        internal virtual SerializationMode Mode
        {
            get { return SerializationMode.SharedContract; }
        }

        internal virtual bool IsGetOnlyCollection
        {
            get { return false; }
            set { }
        }


        internal StreamingContext GetStreamingContext()
        {
            return _streamingContext;
        }

        internal void IncrementItemCount(int count)
        {
            if (count > _maxItemsInObjectGraph - _itemCount)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.ExceededMaxItemsQuota, _maxItemsInObjectGraph)));
            _itemCount += count;
        }

        internal int RemainingItemCount
        {
            get { return _maxItemsInObjectGraph - _itemCount; }
        }

        internal bool IgnoreExtensionDataObject
        {
            get { return _ignoreExtensionDataObject; }
        }

        protected DataContractResolver? DataContractResolver
        {
            get { return _dataContractResolver; }
        }

        protected KnownTypeDataContractResolver KnownTypeResolver
        {
            get
            {
                if (_knownTypeResolver == null)
                {
                    _knownTypeResolver = new KnownTypeDataContractResolver(this);
                }
                return _knownTypeResolver;
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal DataContract GetDataContract(Type type)
        {
            return GetDataContract(type.TypeHandle, type);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal virtual DataContract GetDataContract(RuntimeTypeHandle typeHandle, Type? type)
        {
            if (IsGetOnlyCollection)
            {
                return DataContract.GetGetOnlyCollectionDataContract(DataContract.GetId(typeHandle), typeHandle, type, Mode);
            }
            else
            {
                return DataContract.GetDataContract(typeHandle, type, Mode);
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal virtual DataContract GetDataContractSkipValidation(int typeId, RuntimeTypeHandle typeHandle, Type? type)
        {
            if (IsGetOnlyCollection)
            {
                return DataContract.GetGetOnlyCollectionDataContractSkipValidation(typeId, typeHandle, type);
            }
            else
            {
                return DataContract.GetDataContractSkipValidation(typeId, typeHandle, type);
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal virtual DataContract GetDataContract(int id, RuntimeTypeHandle typeHandle)
        {
            if (IsGetOnlyCollection)
            {
                return DataContract.GetGetOnlyCollectionDataContract(id, typeHandle, null /*type*/, Mode);
            }
            else
            {
                return DataContract.GetDataContract(id, typeHandle, Mode);
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal virtual void CheckIfTypeSerializable(Type memberType, bool isMemberTypeSerializable)
        {
            if (!isMemberTypeSerializable)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.TypeNotSerializable, memberType)));
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal virtual Type GetSurrogatedType(Type type)
        {
            return type;
        }
        internal virtual DataContractDictionary? SerializerKnownDataContracts
        {
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get
            {
                // This field must be initialized during construction by serializers using data contracts.
                if (!_isSerializerKnownDataContractsSetExplicit)
                {
                    this.serializerKnownDataContracts = serializer.KnownDataContracts;
                    _isSerializerKnownDataContractsSetExplicit = true;
                }
                return this.serializerKnownDataContracts;
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private DataContract? GetDataContractFromSerializerKnownTypes(XmlQualifiedName qname)
        {
            DataContractDictionary? serializerKnownDataContracts = this.SerializerKnownDataContracts;
            if (serializerKnownDataContracts == null)
                return null;
            DataContract? outDataContract;
            return serializerKnownDataContracts.TryGetValue(qname, out outDataContract) ? outDataContract : null;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static DataContractDictionary? GetDataContractsForKnownTypes(IList<Type> knownTypeList)
        {
            if (knownTypeList == null) return null;
            DataContractDictionary dataContracts = new DataContractDictionary();
            Dictionary<Type, Type> typesChecked = new Dictionary<Type, Type>();
            for (int i = 0; i < knownTypeList.Count; i++)
            {
                Type knownType = knownTypeList[i];
                if (knownType == null)
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(SR.Format(SR.NullKnownType, "knownTypes")));

                DataContract.CheckAndAdd(knownType, typesChecked, ref dataContracts);
            }
            return dataContracts;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal bool IsKnownType(DataContract dataContract, DataContractDictionary? knownDataContracts, Type? declaredType)
        {
            bool knownTypesAddedInCurrentScope = false;
            if (knownDataContracts != null)
            {
                scopedKnownTypes.Push(knownDataContracts);
                knownTypesAddedInCurrentScope = true;
            }

            bool isKnownType = IsKnownType(dataContract, declaredType);

            if (knownTypesAddedInCurrentScope)
            {
                scopedKnownTypes.Pop();
            }
            return isKnownType;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal bool IsKnownType(DataContract dataContract, Type? declaredType)
        {
            DataContract? knownContract = ResolveDataContractFromKnownTypes(dataContract.StableName.Name, dataContract.StableName.Namespace, null /*memberTypeContract*/, declaredType);
            return knownContract != null && knownContract.UnderlyingType == dataContract.UnderlyingType;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal Type? ResolveNameFromKnownTypes(XmlQualifiedName typeName)
        {
            DataContract? dataContract = ResolveDataContractFromKnownTypes(typeName);
            return dataContract == null ? null : dataContract.UnderlyingType;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private DataContract? ResolveDataContractFromKnownTypes(XmlQualifiedName typeName)
        {
            DataContract? dataContract = PrimitiveDataContract.GetPrimitiveDataContract(typeName.Name, typeName.Namespace);
            if (dataContract == null)
            {
                dataContract = scopedKnownTypes.GetDataContract(typeName);
                if (dataContract == null)
                {
                    dataContract = GetDataContractFromSerializerKnownTypes(typeName);
                }
            }
            return dataContract;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected DataContract? ResolveDataContractFromKnownTypes(string typeName, string? typeNs, DataContract? memberTypeContract, Type? declaredType)
        {
            XmlQualifiedName qname = new XmlQualifiedName(typeName, typeNs);
            DataContract? dataContract;
            if (_dataContractResolver == null)
            {
                dataContract = ResolveDataContractFromKnownTypes(qname);
            }
            else
            {
                Type? dataContractType = _dataContractResolver.ResolveName(typeName, typeNs, declaredType, KnownTypeResolver);
                dataContract = dataContractType == null ? null : GetDataContract(dataContractType);
            }
            if (dataContract == null)
            {
                if (memberTypeContract != null
                    && !memberTypeContract.UnderlyingType.IsInterface
                    && memberTypeContract.StableName == qname)
                {
                    dataContract = memberTypeContract;
                }
                if (dataContract == null && rootTypeDataContract != null)
                {
                    if (rootTypeDataContract.StableName == qname)
                        dataContract = rootTypeDataContract;
                    else
                    {
                        CollectionDataContract? collectionContract = rootTypeDataContract as CollectionDataContract;
                        while (collectionContract != null)
                        {
                            DataContract itemContract = GetDataContract(GetSurrogatedType(collectionContract.ItemType));
                            if (itemContract.StableName == qname)
                            {
                                dataContract = itemContract;
                                break;
                            }
                            collectionContract = itemContract as CollectionDataContract;
                        }
                    }
                }
            }
            return dataContract;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal void PushKnownTypes(DataContract dc)
        {
            if (dc != null && dc.KnownDataContracts != null)
            {
                scopedKnownTypes.Push(dc.KnownDataContracts);
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal void PopKnownTypes(DataContract dc)
        {
            if (dc != null && dc.KnownDataContracts != null)
            {
                scopedKnownTypes.Pop();
            }
        }
    }
}
