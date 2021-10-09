// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using DataContractDictionary = System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, System.Runtime.Serialization.DataContract>;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.Serialization
{
    internal sealed class DataContractSet
    {
        private Dictionary<XmlQualifiedName, DataContract>? _contracts;
        private Dictionary<DataContract, object>? _processedContracts;
        private readonly ICollection<Type> _referencedTypes;
        private readonly ICollection<Type> _referencedCollectionTypes;

#if SUPPORT_SURROGATE
        private IDataContractSurrogate _dataContractSurrogate;
        private Hashtable _surrogateDataTable;

        internal DataContractSet(IDataContractSurrogate dataContractSurrogate) : this(dataContractSurrogate, null, null) { }

        internal DataContractSet(IDataContractSurrogate dataContractSurrogate, ICollection<Type> referencedTypes, ICollection<Type> referencedCollectionTypes)
        {
            _dataContractSurrogate = dataContractSurrogate;
            _referencedTypes = referencedTypes;
            _referencedCollectionTypes = referencedCollectionTypes;
        }
#endif

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal DataContractSet(DataContractSet dataContractSet)
        {
            if (dataContractSet == null)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(dataContractSet)));

            //this.dataContractSurrogate = dataContractSet.dataContractSurrogate;
            _referencedTypes = dataContractSet._referencedTypes;
            _referencedCollectionTypes = dataContractSet._referencedCollectionTypes;

            foreach (KeyValuePair<XmlQualifiedName, DataContract> pair in dataContractSet)
            {
                Add(pair.Key, pair.Value);
            }

            if (dataContractSet._processedContracts != null)
            {
                foreach (KeyValuePair<DataContract, object> pair in dataContractSet._processedContracts)
                {
                    ProcessedContracts.Add(pair.Key, pair.Value);
                }
            }
        }

        private Dictionary<XmlQualifiedName, DataContract> Contracts
        {
            get
            {
                if (_contracts == null)
                {
                    _contracts = new Dictionary<XmlQualifiedName, DataContract>();
                }
                return _contracts;
            }
        }

        private Dictionary<DataContract, object> ProcessedContracts
        {
            get
            {
                if (_processedContracts == null)
                {
                    _processedContracts = new Dictionary<DataContract, object>();
                }
                return _processedContracts;
            }
        }
#if SUPPORT_SURROGATE
        private Hashtable SurrogateDataTable
        {
            get
            {
                if (_surrogateDataTable == null)
                    _surrogateDataTable = new Hashtable();
                return _surrogateDataTable;
            }
        }
#endif

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal void Add(Type type)
        {
            DataContract dataContract = GetDataContract(type);
            EnsureTypeNotGeneric(dataContract.UnderlyingType);
            Add(dataContract);
        }

        internal static void EnsureTypeNotGeneric(Type type)
        {
            if (type.ContainsGenericParameters)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.GenericTypeNotExportable, type)));
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private void Add(DataContract dataContract)
        {
            Add(dataContract.StableName, dataContract);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public void Add(XmlQualifiedName name, DataContract dataContract)
        {
            if (dataContract.IsBuiltInDataContract)
                return;
            InternalAdd(name, dataContract);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal void InternalAdd(XmlQualifiedName name, DataContract dataContract)
        {
            DataContract? dataContractInSet = null;
            if (Contracts.TryGetValue(name, out dataContractInSet))
            {
                if (!dataContractInSet.Equals(dataContract))
                {
                    if (dataContract.UnderlyingType == null || dataContractInSet.UnderlyingType == null)
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.Format(SR.DupContractInDataContractSet, dataContract.StableName.Name, dataContract.StableName.Namespace)));
                    else
                    {
                        bool typeNamesEqual = (DataContract.GetClrTypeFullName(dataContract.UnderlyingType) == DataContract.GetClrTypeFullName(dataContractInSet.UnderlyingType));
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.Format(SR.DupTypeContractInDataContractSet, (typeNamesEqual ? dataContract.UnderlyingType.AssemblyQualifiedName : DataContract.GetClrTypeFullName(dataContract.UnderlyingType)), (typeNamesEqual ? dataContractInSet.UnderlyingType.AssemblyQualifiedName : DataContract.GetClrTypeFullName(dataContractInSet.UnderlyingType)), dataContract.StableName.Name, dataContract.StableName.Namespace)));
                    }
                }
            }
            else
            {
                Contracts.Add(name, dataContract);

                if (dataContract is ClassDataContract)
                {
                    AddClassDataContract((ClassDataContract)dataContract);
                }
                else if (dataContract is CollectionDataContract)
                {
                    AddCollectionDataContract((CollectionDataContract)dataContract);
                }
                else if (dataContract is XmlDataContract)
                {
                    AddXmlDataContract((XmlDataContract)dataContract);
                }
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private void AddClassDataContract(ClassDataContract classDataContract)
        {
            if (classDataContract.BaseContract != null)
            {
                Add(classDataContract.BaseContract.StableName, classDataContract.BaseContract);
            }
            if (!classDataContract.IsISerializable)
            {
                if (classDataContract.Members != null)
                {
                    for (int i = 0; i < classDataContract.Members.Count; i++)
                    {
                        DataMember dataMember = classDataContract.Members[i];
                        DataContract memberDataContract = GetMemberTypeDataContract(dataMember);
#if SUPPORT_SURROGATE
                        if (_dataContractSurrogate != null && dataMember.MemberInfo != null)
                        {
                            object customData = DataContractSurrogateCaller.GetCustomDataToExport(
                                                   _dataContractSurrogate,
                                                   dataMember.MemberInfo,
                                                   memberDataContract.UnderlyingType);
                            if (customData != null)
                                SurrogateDataTable.Add(dataMember, customData);
                        }
#endif
                        Add(memberDataContract.StableName, memberDataContract);
                    }
                }
            }
            AddKnownDataContracts(classDataContract.KnownDataContracts);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private void AddCollectionDataContract(CollectionDataContract collectionDataContract)
        {
            if (collectionDataContract.IsDictionary)
            {
                ClassDataContract keyValueContract = (collectionDataContract.ItemContract as ClassDataContract)!;
                AddClassDataContract(keyValueContract);
            }
            else
            {
                DataContract itemContract = GetItemTypeDataContract(collectionDataContract);
                if (itemContract != null)
                    Add(itemContract.StableName, itemContract);
            }
            AddKnownDataContracts(collectionDataContract.KnownDataContracts);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private void AddXmlDataContract(XmlDataContract xmlDataContract)
        {
            AddKnownDataContracts(xmlDataContract.KnownDataContracts);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private void AddKnownDataContracts(DataContractDictionary? knownDataContracts)
        {
            if (knownDataContracts != null)
            {
                foreach (DataContract knownDataContract in knownDataContracts.Values)
                {
                    Add(knownDataContract);
                }
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal DataContract GetDataContract(Type clrType)
        {
#if SUPPORT_SURROGATE
            if (_dataContractSurrogate == null)
                return DataContract.GetDataContract(clrType);
#endif
            DataContract? dataContract = DataContract.GetBuiltInDataContract(clrType);
            if (dataContract != null)
                return dataContract;

#if SUPPORT_SURROGATE
            Type dcType = DataContractSurrogateCaller.GetDataContractType(_dataContractSurrogate, clrType);
            if (clrType.IsValueType != dcType.IsValueType)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.GetString(SR.ValueTypeMismatchInSurrogatedType, dcType, clrType)));
#endif
            Type dcType = clrType;
            dataContract = DataContract.GetDataContract(dcType);
#if SUPPORT_SURROGATE
            if (!SurrogateDataTable.Contains(dataContract))
            {
                object customData = DataContractSurrogateCaller.GetCustomDataToExport(
                                      _dataContractSurrogate, clrType, dcType);
                if (customData != null)
                    SurrogateDataTable.Add(dataContract, customData);
            }
#endif
            return dataContract;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal DataContract GetMemberTypeDataContract(DataMember dataMember)
        {
            Type dataMemberType = dataMember.MemberType;
            if (dataMember.IsGetOnlyCollection)
            {
#if SUPPORT_SURROGATE
                    if (_dataContractSurrogate != null)
                    {
                        Type dcType = DataContractSurrogateCaller.GetDataContractType(_dataContractSurrogate, dataMemberType);
                        if (dcType != dataMemberType)
                        {
                            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.GetString(SR.SurrogatesWithGetOnlyCollectionsNotSupported,
                                DataContract.GetClrTypeFullName(dataMemberType), DataContract.GetClrTypeFullName(dataMember.MemberInfo.DeclaringType), dataMember.MemberInfo.Name)));
                        }
                    }
#endif
                return DataContract.GetGetOnlyCollectionDataContract(DataContract.GetId(dataMemberType.TypeHandle), dataMemberType.TypeHandle, dataMemberType, SerializationMode.SharedContract);
            }
            else
            {
                return GetDataContract(dataMemberType);
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal DataContract GetItemTypeDataContract(CollectionDataContract collectionContract)
        {
            if (collectionContract.ItemType != null)
                return GetDataContract(collectionContract.ItemType);
            return collectionContract.ItemContract;
        }

#if SUPPORT_SURROGATE
        internal object GetSurrogateData(object key)
        {
            return SurrogateDataTable[key];
        }

        internal void SetSurrogateData(object key, object surrogateData)
        {
            SurrogateDataTable[key] = surrogateData;
        }

        public IDataContractSurrogate DataContractSurrogate
        {
            get { return _dataContractSurrogate; }
        }
#endif

        public IEnumerator<KeyValuePair<XmlQualifiedName, DataContract>> GetEnumerator()
        {
            return Contracts.GetEnumerator();
        }

        internal bool IsContractProcessed(DataContract dataContract)
        {
            return ProcessedContracts.ContainsKey(dataContract);
        }

        internal void SetContractProcessed(DataContract dataContract)
        {
            ProcessedContracts.Add(dataContract, dataContract);
        }

#if SUPPORT_SURROGATE
        internal ContractCodeDomInfo GetContractCodeDomInfo(DataContract dataContract)
        {
            object info;
            if (ProcessedContracts.TryGetValue(dataContract, out info))
                return (ContractCodeDomInfo)info;
            return null;
        }

        internal void SetContractCodeDomInfo(DataContract dataContract, ContractCodeDomInfo info)
        {
            ProcessedContracts.Add(dataContract, info);
        }
#endif
    }
}
