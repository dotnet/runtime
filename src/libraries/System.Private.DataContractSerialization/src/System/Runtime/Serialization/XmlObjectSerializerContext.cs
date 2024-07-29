// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Serialization.DataContracts;
using System.Security;
using System.Xml;

using DataContractDictionary = System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, System.Runtime.Serialization.DataContracts.DataContract>;

namespace System.Runtime.Serialization
{
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
#pragma warning disable SYSLIB0050 // StreamingContext ctor is obsolete
            new StreamingContext(StreamingContextStates.All),
#pragma warning restore SYSLIB0050
            serializer.IgnoreExtensionDataObject,
            dataContractResolver
            )
        {
            this.rootTypeDataContract = rootTypeDataContract;
            this.serializerKnownTypeList = serializer._knownTypeList;
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
                throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.ExceededMaxItemsQuota, _maxItemsInObjectGraph));
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

        protected KnownTypeDataContractResolver KnownTypeResolver =>
            _knownTypeResolver ??= new KnownTypeDataContractResolver(this);

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal DataContract GetDataContract(Type type)
        {
            return GetDataContract(type.TypeHandle, type);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal virtual DataContract GetDataContract(RuntimeTypeHandle typeHandle, Type? type)
        {
            if (IsGetOnlyCollection)
            {
                return DataContract.GetGetOnlyCollectionDataContract(DataContract.GetId(typeHandle), typeHandle, type);
            }
            else
            {
                return DataContract.GetDataContract(typeHandle);
            }
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
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

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal virtual DataContract GetDataContract(int id, RuntimeTypeHandle typeHandle)
        {
            if (IsGetOnlyCollection)
            {
                return DataContract.GetGetOnlyCollectionDataContract(id, typeHandle, null /*type*/);
            }
            else
            {
                return DataContract.GetDataContract(id, typeHandle);
            }
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal virtual void CheckIfTypeSerializable(Type memberType, bool isMemberTypeSerializable)
        {
            if (!isMemberTypeSerializable)
                throw new InvalidDataContractException(SR.Format(SR.TypeNotSerializable, memberType));
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal virtual Type GetSurrogatedType(Type type)
        {
            return type;
        }
        internal virtual DataContractDictionary? SerializerKnownDataContracts
        {
            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
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

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private DataContract? GetDataContractFromSerializerKnownTypes(XmlQualifiedName qname)
        {
            DataContractDictionary? serializerKnownDataContracts = this.SerializerKnownDataContracts;
            if (serializerKnownDataContracts == null)
                return null;
            DataContract? outDataContract;
            return serializerKnownDataContracts.TryGetValue(qname, out outDataContract) ? outDataContract : null;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
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
                    throw new ArgumentException(SR.Format(SR.NullKnownType, "knownTypes"));

                DataContract.CheckAndAdd(knownType, typesChecked, ref dataContracts);
            }
            return dataContracts;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal bool IsKnownType(DataContract dataContract, DataContractDictionary? knownDataContracts, Type? declaredType)
        {
            bool knownTypesAddedInCurrentScope = false;
            if (knownDataContracts?.Count > 0)
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

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal bool IsKnownType(DataContract dataContract, Type? declaredType)
        {
            DataContract? knownContract = ResolveDataContractFromKnownTypes(dataContract.XmlName.Name, dataContract.XmlName.Namespace, null /*memberTypeContract*/, declaredType);
            return knownContract != null && knownContract.UnderlyingType == dataContract.UnderlyingType;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal Type? ResolveNameFromKnownTypes(XmlQualifiedName typeName)
        {
            DataContract? dataContract = ResolveDataContractFromKnownTypes(typeName);
            return dataContract?.OriginalUnderlyingType;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private DataContract? ResolveDataContractFromKnownTypes(XmlQualifiedName typeName) =>
            PrimitiveDataContract.GetPrimitiveDataContract(typeName.Name, typeName.Namespace) ??
            scopedKnownTypes.GetDataContract(typeName) ??
            GetDataContractFromSerializerKnownTypes(typeName);

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
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
                    && memberTypeContract.XmlName == qname)
                {
                    dataContract = memberTypeContract;
                }
                if (dataContract == null && rootTypeDataContract != null)
                {
                    if (rootTypeDataContract.XmlName == qname)
                        dataContract = rootTypeDataContract;
                    else
                        dataContract = ResolveDataContractFromRootDataContract(qname);
                }
            }
            return dataContract;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected virtual DataContract? ResolveDataContractFromRootDataContract(XmlQualifiedName typeQName)
        {
            CollectionDataContract? collectionContract = rootTypeDataContract as CollectionDataContract;
            while (collectionContract != null)
            {
                DataContract itemContract = GetDataContract(GetSurrogatedType(collectionContract.ItemType));
                if (itemContract.XmlName == typeQName)
                {
                    return itemContract;
                }
                collectionContract = itemContract as CollectionDataContract;
            }
            return null;
        }
    }
}
