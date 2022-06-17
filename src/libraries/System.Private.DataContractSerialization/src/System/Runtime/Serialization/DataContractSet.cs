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
#if smolloy_add_schema_import
        private readonly ICollection<Type>? _referencedTypes;
        private readonly ICollection<Type>? _referencedCollectionTypes;
#else
        // NOTE TODO smolloy - The difference here is just nullability. BUT... the only way these private fields ever get set is in a copy constructor. Which means there is no way to
        // actually construct a DataContractSet with meaningful values here. The might as well just go away.
        private readonly ICollection<Type> _referencedTypes;
        private readonly ICollection<Type> _referencedCollectionTypes;
#endif
#if smolloy_add_ext_surrogate
        private ISerializationExtendedSurrogateProvider? _serializationExtendedSurrogateProvider;
        private Hashtable? _surrogateDataTable;
#endif

#if smolloy_add_schema_import
#if smolloy_add_ext_surrogate
        internal DataContractSet(ISerializationExtendedSurrogateProvider? dataContractSurrogate, ICollection<Type>? referencedTypes, ICollection<Type>? referencedCollectionTypes)
        {
            _serializationExtendedSurrogateProvider = dataContractSurrogate;
#else
        internal DataContractSet(ISerializationSurrogateProvider? dataContractSurrogate, ICollection<Type>? referencedTypes, ICollection<Type>? referencedCollectionTypes)
        {
#endif
            _referencedTypes = referencedTypes;
            _referencedCollectionTypes = referencedCollectionTypes;
        }
#endif

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal DataContractSet(DataContractSet dataContractSet)
        {
            ArgumentNullException.ThrowIfNull(dataContractSet);

#if smolloy_add_ext_surrogate
            _serializationExtendedSurrogateProvider = dataContractSet._serializationExtendedSurrogateProvider;
#endif

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

#if smolloy_add_ext_surrogate
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

#if smolloy_add_schema_import
        private DataContractDictionary? _knownTypesForObject;
        internal DataContractDictionary? KnownTypesForObject
        {
            get { return _knownTypesForObject; }
            set { _knownTypesForObject = value; }
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
#if smolloy_add_ext_surrogate
                        if (_serializationExtendedSurrogateProvider != null && dataMember.MemberInfo != null)
                        {
                            object? customData = DataContractSurrogateCaller.GetCustomDataToExport(
                                                    _serializationExtendedSurrogateProvider,
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


#if smolloy_add_schema_import   // NOTE TODO smolloy - GetStableName() is added for schema import. It can be static without the use of surrogates... but I don't think we can have schema import without the extended surrogate support? So maybe the static/instance option here isn't needed?
#if smolloy_add_ext_surrogate
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal XmlQualifiedName GetStableName(Type clrType)
        {
            if (_serializationExtendedSurrogateProvider != null)
            {
                Type dcType = DataContractSurrogateCaller.GetDataContractType(_serializationExtendedSurrogateProvider, clrType);
                return DataContract.GetStableName(dcType);
            }
            return DataContract.GetStableName(clrType);
        }
#else
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static XmlQualifiedName GetStableName(Type clrType)
        {
            return DataContract.GetStableName(clrType);
        }
#endif
#endif

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
#if smolloy_add_ext_surrogate
        internal DataContract GetDataContract(Type clrType)
        {
            if (_serializationExtendedSurrogateProvider == null)
                return DataContract.GetDataContract(clrType);
#else
        internal static DataContract GetDataContract(Type clrType)
        {
#endif
            DataContract? dataContract = DataContract.GetBuiltInDataContract(clrType);
            if (dataContract != null)
                return dataContract;

#if smolloy_add_ext_surrogate
            Type dcType = DataContractSurrogateCaller.GetDataContractType(_serializationExtendedSurrogateProvider, clrType);
            dataContract = DataContract.GetDataContract(dcType);
            if (!SurrogateDataTable.Contains(dataContract))
            {
                object? customData = DataContractSurrogateCaller.GetCustomDataToExport(_serializationExtendedSurrogateProvider, clrType, dcType);
                if (customData != null)
                    SurrogateDataTable.Add(dataContract, customData);
            }
#else
            Type dcType = clrType;
            dataContract = DataContract.GetDataContract(dcType);
#endif
            return dataContract;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal
#if !smolloy_add_ext_surrogate
            static
#endif
            DataContract GetMemberTypeDataContract(DataMember dataMember)
        {
            Type dataMemberType = dataMember.MemberType;
            if (dataMember.IsGetOnlyCollection)
            {
#if smolloy_add_ext_surrogate
                if (_serializationExtendedSurrogateProvider != null)
                {
                    Type dcType = DataContractSurrogateCaller.GetDataContractType(_serializationExtendedSurrogateProvider, dataMemberType);
                    if (dcType != dataMemberType)
                    {
                        throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.SurrogatesWithGetOnlyCollectionsNotSupported,
                            DataContract.GetClrTypeFullName(dataMemberType),
                            (dataMember.MemberInfo.DeclaringType != null) ? DataContract.GetClrTypeFullName(dataMember.MemberInfo.DeclaringType) : dataMember.MemberInfo.DeclaringType,
                            dataMember.MemberInfo.Name)));
                    }
                }
#endif
                return DataContract.GetGetOnlyCollectionDataContract(DataContract.GetId(dataMemberType.TypeHandle), dataMemberType.TypeHandle, dataMemberType);
            }
            else
            {
                return GetDataContract(dataMemberType);
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal
#if !smolloy_add_ext_surrogate
            static
#endif
            DataContract GetItemTypeDataContract(CollectionDataContract collectionContract)
        {
            if (collectionContract.ItemType != null)
                return GetDataContract(collectionContract.ItemType);
            return collectionContract.ItemContract;
        }

#if smolloy_add_ext_surrogate
        internal ISerializationExtendedSurrogateProvider? SerializationExtendedSurrogateProvider
        {
            get { return _serializationExtendedSurrogateProvider; }
        }

        internal object? GetSurrogateData(object key)
        {
            return SurrogateDataTable[key];
        }

        internal void SetSurrogateData(object key, object? surrogateData)
        {
            SurrogateDataTable[key] = surrogateData;
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

#if smolloy_add_schema_import
        // NOTE TODO smolloy - Many of these methods existed in CoreFx already... albeit with slightly different nullable notations. But they were unused before schema import.
        // If we drop schema import, we could drop these entirely as well.
        internal DataContract? this[XmlQualifiedName key]
        {
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get
            {
                DataContract? dataContract = DataContract.GetBuiltInDataContract(key.Name, key.Namespace);
                if (dataContract == null)
                {
                    Contracts.TryGetValue(key, out dataContract);
                }
                return dataContract;
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal bool Remove(XmlQualifiedName key)
        {
            if (DataContract.GetBuiltInDataContract(key.Name, key.Namespace) != null)
                return false;
            return Contracts.Remove(key);
        }

        internal ContractCodeDomInfo? GetContractCodeDomInfo(DataContract dataContract)
        {
            object? info;
            if (ProcessedContracts.TryGetValue(dataContract, out info))
                return (ContractCodeDomInfo)info;
            return null;
        }

        internal void SetContractCodeDomInfo(DataContract dataContract, ContractCodeDomInfo info)
        {
            ProcessedContracts.Add(dataContract, info);
        }

        private Dictionary<XmlQualifiedName, object>? _referencedTypesDictionary;
        private Dictionary<XmlQualifiedName, object>? _referencedCollectionTypesDictionary;

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private Dictionary<XmlQualifiedName, object> GetReferencedTypes()
        {
            if (_referencedTypesDictionary == null)
            {
                _referencedTypesDictionary = new Dictionary<XmlQualifiedName, object>();
                //Always include Nullable as referenced type
                //Do not allow surrogating Nullable<T>
                _referencedTypesDictionary.Add(DataContract.GetStableName(Globals.TypeOfNullable), Globals.TypeOfNullable);
                if (_referencedTypes != null)
                {
                    foreach (Type type in _referencedTypes)
                    {
                        if (type == null)
                            throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.Format(SR.ReferencedTypesCannotContainNull)));

                        AddReferencedType(_referencedTypesDictionary, type);
                    }
                }
            }
            return _referencedTypesDictionary;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private Dictionary<XmlQualifiedName, object> GetReferencedCollectionTypes()
        {
            if (_referencedCollectionTypesDictionary == null)
            {
                _referencedCollectionTypesDictionary = new Dictionary<XmlQualifiedName, object>();
                if (_referencedCollectionTypes != null)
                {
                    foreach (Type type in _referencedCollectionTypes)
                    {
                        if (type == null)
                            throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.Format(SR.ReferencedCollectionTypesCannotContainNull)));
                        AddReferencedType(_referencedCollectionTypesDictionary, type);
                    }
                }
                XmlQualifiedName genericDictionaryName = DataContract.GetStableName(Globals.TypeOfDictionaryGeneric);
                if (!_referencedCollectionTypesDictionary.ContainsKey(genericDictionaryName) && GetReferencedTypes().ContainsKey(genericDictionaryName))
                    AddReferencedType(_referencedCollectionTypesDictionary, Globals.TypeOfDictionaryGeneric);
            }
            return _referencedCollectionTypesDictionary;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private
#if !smolloy_add_ext_surrogate
            static
#endif
            void AddReferencedType(Dictionary<XmlQualifiedName, object> referencedTypes, Type type)
        {
            if (IsTypeReferenceable(type))
            {
                XmlQualifiedName stableName;
                try
                {
                    stableName = GetStableName(type);
                }
                catch (InvalidDataContractException)
                {
                    // Type not referenceable if we can't get a stable name.
                    return;
                }
                catch (InvalidOperationException)
                {
                    // Type not referenceable if we can't get a stable name.
                    return;
                }

                if (referencedTypes.TryGetValue(stableName, out object? value))
                {
                    if (value is Type referencedType)
                    {
                        if (referencedType != type)
                        {
                            referencedTypes.Remove(stableName);
                            List<Type> types = new List<Type>();
                            types.Add(referencedType);
                            types.Add(type);
                            referencedTypes.Add(stableName, types);
                        }
                    }
                    else
                    {
                        List<Type> types = (List<Type>)value;
                        if (!types.Contains(type))
                            types.Add(type);
                    }
                }
                else
                    referencedTypes.Add(stableName, type);
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal bool TryGetReferencedType(XmlQualifiedName stableName, DataContract? dataContract, [NotNullWhen(true)] out Type? type)
        {
            return TryGetReferencedType(stableName, dataContract, false/*useReferencedCollectionTypes*/, out type);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal bool TryGetReferencedCollectionType(XmlQualifiedName stableName, DataContract? dataContract, [NotNullWhen(true)] out Type? type)
        {
            return TryGetReferencedType(stableName, dataContract, true/*useReferencedCollectionTypes*/, out type);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private bool TryGetReferencedType(XmlQualifiedName stableName, DataContract? dataContract, bool useReferencedCollectionTypes, [NotNullWhen(true)] out Type? type)
        {
            Dictionary<XmlQualifiedName, object> referencedTypes = useReferencedCollectionTypes ? GetReferencedCollectionTypes() : GetReferencedTypes();
            if (referencedTypes.TryGetValue(stableName, out object? value))
            {
                type = value as Type;
                if (type != null)
                {
                    return true;
                }
                else
                {
                    // Throw ambiguous type match exception
                    List<Type> types = (List<Type>)value;
                    StringBuilder errorMessage = new StringBuilder();
                    bool containsGenericType = false;
                    for (int i = 0; i < types.Count; i++)
                    {
                        Type conflictingType = types[i];
                        if (!containsGenericType)
                            containsGenericType = conflictingType.IsGenericTypeDefinition;
                        errorMessage.AppendFormat("{0}\"{1}\" ", Environment.NewLine, conflictingType.AssemblyQualifiedName);
                        if (dataContract != null)
                        {
                            DataContract other = GetDataContract(conflictingType);
                            errorMessage.Append(SR.Format(((other != null && other.Equals(dataContract)) ? SR.ReferencedTypeMatchingMessage : SR.ReferencedTypeNotMatchingMessage)));
                        }
                    }
                    if (containsGenericType)
                    {
                        throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.Format(
                            (useReferencedCollectionTypes ? SR.AmbiguousReferencedCollectionTypes1 : SR.AmbiguousReferencedTypes1),
                            errorMessage.ToString())));
                    }
                    else
                    {
                        throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.Format(
                            (useReferencedCollectionTypes ? SR.AmbiguousReferencedCollectionTypes3 : SR.AmbiguousReferencedTypes3),
                            XmlConvert.DecodeName(stableName.Name),
                            stableName.Namespace,
                            errorMessage.ToString())));
                    }
                }
            }
            type = null;
            return false;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private static bool IsTypeReferenceable(Type type)
        {
            try
            {
                return (type.IsSerializable ||
                        type.IsDefined(Globals.TypeOfDataContractAttribute, false) ||
                        (Globals.TypeOfIXmlSerializable.IsAssignableFrom(type) && !type.IsGenericTypeDefinition) ||
                        CollectionDataContract.IsCollection(type, out _) ||
                        ClassDataContract.IsNonAttributedTypeValidForSerialization(type));
            }
            catch (Exception)
            {
                // An exception can be thrown in the designer when a project has a runtime binding redirection for a referenced assembly or a reference dependent assembly.
                // Type.IsDefined is known to throw System.IO.FileLoadException.
                // ClassDataContract.IsNonAttributedTypeValidForSerialization is known to throw System.IO.FileNotFoundException.
                // We guard against all non-critical exceptions.
            }

            return false;
        }
#endif
    }
}
