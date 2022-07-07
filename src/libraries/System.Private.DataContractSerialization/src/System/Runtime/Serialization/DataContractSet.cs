// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Xml.Schema;

using DataContractDictionary = System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, System.Runtime.Serialization.DataContract>;

namespace System.Runtime.Serialization
{
    public sealed class DataContractSet
    {
        private DataContractDictionary? _contracts;
        private Dictionary<DataContract, object>? _processedContracts;
        private readonly ISerializationExtendedSurrogateProvider? _extendedSurrogateProvider;
        private Hashtable? _surrogateDataTable;
        private DataContractDictionary? _knownTypesForObject;
        private readonly ICollection<Type>? _referencedTypes;
        private readonly ICollection<Type>? _referencedCollectionTypes;

        public DataContractSet(ISerializationExtendedSurrogateProvider? dataContractExtendedSurrogate, ICollection<Type>? referencedTypes, ICollection<Type>? referencedCollectionTypes)
        {
            _referencedTypes = referencedTypes;
            _referencedCollectionTypes = referencedCollectionTypes;
            _extendedSurrogateProvider = dataContractExtendedSurrogate;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public DataContractSet(DataContractSet dataContractSet)
        {
            ArgumentNullException.ThrowIfNull(dataContractSet);

            _referencedTypes = dataContractSet._referencedTypes;
            _referencedCollectionTypes = dataContractSet._referencedCollectionTypes;
            _extendedSurrogateProvider = dataContractSet._extendedSurrogateProvider;

            foreach (KeyValuePair<XmlQualifiedName, DataContract> pair in dataContractSet.Contracts)
            {
                InternalAdd(pair.Key, pair.Value);
            }

            if (dataContractSet._processedContracts != null)
            {
                foreach (KeyValuePair<DataContract, object> pair in dataContractSet._processedContracts)
                {
                    ProcessedContracts.Add(pair.Key, pair.Value);
                }
            }
        }

        internal DataContractDictionary Contracts =>
            _contracts ??= new DataContractDictionary();

        public Dictionary<DataContract, object> ProcessedContracts =>
            _processedContracts ??= new Dictionary<DataContract, object>();

        private Hashtable SurrogateDataTable => _surrogateDataTable ??= new Hashtable();

        public DataContractDictionary? KnownTypesForObject
        {
            get => _knownTypesForObject;
            internal set => _knownTypesForObject = value;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public void Add(Type type)
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
        internal void Add(XmlQualifiedName name, DataContract dataContract)
        {
            if (dataContract.IsBuiltInDataContract)
                return;
            if (dataContract is DataContract dataContractInternal)
                InternalAdd(name, dataContractInternal);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal void InternalAdd(XmlQualifiedName name, DataContract dataContract)
        {
            if (Contracts.TryGetValue(name, out DataContract? dataContractInSet))
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

                if (dataContract is ClassDataContract classDC)
                {
                    AddClassDataContract(classDC);
                }
                else if (dataContract is CollectionDataContract collectionDC)
                {
                    AddCollectionDataContract(collectionDC);
                }
                else if (dataContract is XmlDataContract xmlDC)
                {
                    AddXmlDataContract(xmlDC);
                }
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private void AddClassDataContract(ClassDataContract classDataContract)
        {
            if (classDataContract.BaseClassContract != null)
            {
                Add(classDataContract.BaseClassContract.StableName, classDataContract.BaseClassContract);
            }
            if (!classDataContract.IsISerializable)
            {
                if (classDataContract.Members != null)
                {
                    for (int i = 0; i < classDataContract.Members.Count; i++)
                    {
                        DataMember dataMember = classDataContract.Members[i];
                        DataContract memberDataContract = GetMemberTypeDataContract(dataMember);

                        if (_extendedSurrogateProvider != null && dataMember.MemberInfo != null)
                        {
                            object? customData = DataContractSurrogateCaller.GetCustomDataToExport(
                                                    _extendedSurrogateProvider,
                                                    dataMember.MemberInfo,
                                                    memberDataContract.UnderlyingType);
                            if (customData != null)
                                SurrogateDataTable.Add(dataMember, customData);
                        }

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
        internal XmlQualifiedName GetStableName(Type clrType)
        {
            if (_extendedSurrogateProvider != null)
            {
                Type dcType = DataContractSurrogateCaller.GetDataContractType(_extendedSurrogateProvider, clrType);
                return DataContract.GetStableName(dcType);
            }
            return DataContract.GetStableName(clrType);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public DataContract GetDataContract(Type type)
        {
            if (_extendedSurrogateProvider == null)
                return DataContract.GetDataContract(type);

            DataContract? dataContract = DataContract.GetBuiltInDataContract(type);
            if (dataContract != null)
                return dataContract;

            Type dcType = DataContractSurrogateCaller.GetDataContractType(_extendedSurrogateProvider, type);
            dataContract = DataContract.GetDataContract(dcType);
            if (!SurrogateDataTable.Contains(dataContract))
            {
                object? customData = DataContractSurrogateCaller.GetCustomDataToExport(_extendedSurrogateProvider, type, dcType);
                if (customData != null)
                    SurrogateDataTable.Add(dataContract, customData);
            }

            return dataContract;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public DataContract? GetDataContract(XmlQualifiedName key)
        {
            DataContract? dataContract = DataContract.GetBuiltInDataContract(key.Name, key.Namespace);
            if (dataContract == null)
            {
                Contracts.TryGetValue(key, out dataContract);
            }
            return dataContract;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal DataContract GetMemberTypeDataContract(DataMember dataMember)
        {
            Type dataMemberType = dataMember.MemberType;
            if (dataMember.IsGetOnlyCollection)
            {
                if (_extendedSurrogateProvider != null)
                {
                    Type dcType = DataContractSurrogateCaller.GetDataContractType(_extendedSurrogateProvider, dataMemberType);
                    if (dcType != dataMemberType)
                    {
                        throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.SurrogatesWithGetOnlyCollectionsNotSupported,
                            DataContract.GetClrTypeFullName(dataMemberType),
                            (dataMember.MemberInfo.DeclaringType != null) ? DataContract.GetClrTypeFullName(dataMember.MemberInfo.DeclaringType) : dataMember.MemberInfo.DeclaringType,
                            dataMember.MemberInfo.Name)));
                    }
                }
                return DataContract.GetGetOnlyCollectionDataContract(DataContract.GetId(dataMemberType.TypeHandle), dataMemberType.TypeHandle, dataMemberType);
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

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal bool Remove(XmlQualifiedName key)
        {
            if (DataContract.GetBuiltInDataContract(key.Name, key.Namespace) != null)
                return false;
            return Contracts.Remove(key);
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
        private void AddReferencedType(Dictionary<XmlQualifiedName, object> referencedTypes, Type type)
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

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public bool TryGetReferencedType(XmlQualifiedName stableName, DataContract? dataContract, [NotNullWhen(true)] out Type? type)
        {
            if (dataContract == null)
            {
                if (TryGetReferencedCollectionType(stableName, null, out type))
                    return true;
                if (TryGetReferencedSingleType(stableName, null, out type))
                {
                    // enforce that collection types only be specified via ReferencedCollectionTypes
                    if (CollectionDataContract.IsCollection(type))
                    {
                        type = null;
                        return false;
                    }
                    return true;
                }
                return false;
            }
            else if (dataContract is CollectionDataContract)
                return TryGetReferencedCollectionType(stableName, dataContract, out type);
            else
            {
                if (dataContract is XmlDataContract xmlDataContract && xmlDataContract.IsAnonymous)
                {
                    stableName = SchemaImporter.ImportActualType(xmlDataContract.XsdType?.Annotation, stableName, dataContract.StableName);
                }
                return TryGetReferencedSingleType(stableName, dataContract, out type);
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal bool TryGetReferencedSingleType(XmlQualifiedName stableName, DataContract? dataContract, [NotNullWhen(true)] out Type? type)
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

        internal ISerializationExtendedSurrogateProvider? SerializationExtendedSurrogateProvider => _extendedSurrogateProvider;

        internal object? GetSurrogateData(object key)
        {
            return SurrogateDataTable[key];
        }

        internal void SetSurrogateData(object key, object? surrogateData)
        {
            SurrogateDataTable[key] = surrogateData;
        }

        internal bool IsContractProcessed(DataContract dataContract)
        {
            return ProcessedContracts.ContainsKey(dataContract);
        }

        internal void SetContractProcessed(DataContract dataContract)
        {
            ProcessedContracts.Add(dataContract, dataContract);
        }

        public IEnumerator<KeyValuePair<XmlQualifiedName, DataContract>> GetEnumerator()
        {
            return Contracts.GetEnumerator();
        }

        public static void CompileSchemaSet(XmlSchemaSet schemaSet)
        {
            SchemaImporter.CompileSchemaSet(schemaSet);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public void ExportSchemaSet(XmlSchemaSet schemaSet)
        {
            SchemaExporter exporter = new SchemaExporter(schemaSet, this);
            exporter.Export();
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public void ImportSchemaSet(XmlSchemaSet schemaSet, ICollection<XmlQualifiedName>? typeNames, ICollection<XmlSchemaElement> elements, XmlQualifiedName[] elementTypeNames /*filled on return*/, bool importXmlDataType)
        {
            SchemaImporter importer = new SchemaImporter(schemaSet, typeNames, elements, elementTypeNames, this, importXmlDataType);
            importer.Import();
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public Type? GetReferencedTypeOnImport(DataContract dataContract)
        {
            ISerializationExtendedSurrogateProvider? dataContractSurrogate = SerializationExtendedSurrogateProvider;
            Type? type = null;
            if (dataContractSurrogate != null)
            {
                type = DataContractSurrogateCaller.GetReferencedTypeOnImport(
                        dataContractSurrogate,
                        dataContract.StableName.Name,
                        dataContract.StableName.Namespace,
                        GetSurrogateData(dataContract));
            }
            return type;
        }

        public bool TryGetSurrogateData(object key, out object? value)
        {
            if (SerializationExtendedSurrogateProvider != null)
            {
                value = GetSurrogateData(key);
                return true;
            }

            value = null;
            return false;
        }
    }
}
