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

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal DataContractSet(DataContractSet dataContractSet)
        {
            ArgumentNullException.ThrowIfNull(dataContractSet);

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

        private Dictionary<XmlQualifiedName, DataContract> Contracts =>
            _contracts ??= new Dictionary<XmlQualifiedName, DataContract>();

        private Dictionary<DataContract, object> ProcessedContracts =>
            _processedContracts ??= new Dictionary<DataContract, object>();

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
            DataContract? dataContractInSet;
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
        internal static DataContract GetDataContract(Type clrType)
        {
            DataContract? dataContract = DataContract.GetBuiltInDataContract(clrType);
            if (dataContract != null)
                return dataContract;

            Type dcType = clrType;
            dataContract = DataContract.GetDataContract(dcType);
            return dataContract;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static DataContract GetMemberTypeDataContract(DataMember dataMember)
        {
            Type dataMemberType = dataMember.MemberType;
            if (dataMember.IsGetOnlyCollection)
            {
                return DataContract.GetGetOnlyCollectionDataContract(DataContract.GetId(dataMemberType.TypeHandle), dataMemberType.TypeHandle, dataMemberType);
            }
            else
            {
                return GetDataContract(dataMemberType);
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static DataContract GetItemTypeDataContract(CollectionDataContract collectionContract)
        {
            if (collectionContract.ItemType != null)
                return GetDataContract(collectionContract.ItemType);
            return collectionContract.ItemContract;
        }

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
    }
}
