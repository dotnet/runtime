// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Xml;
using System.Xml.Schema;

using DataContractDictionary = System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, System.Runtime.Serialization.DataContracts.DataContract>;

namespace System.Runtime.Serialization.DataContracts
{
    public sealed class DataContractSet
    {
        private DataContractDictionary? _contracts;
        private Dictionary<DataContract, object>? _processedContracts;
        private readonly ISerializationSurrogateProvider? _surrogateProvider;
        private readonly ISerializationSurrogateProvider2? _extendedSurrogateProvider;
        private Hashtable? _surrogateData;
        private DataContractDictionary? _knownTypesForObject;
        private readonly List<Type>? _referencedTypes;
        private readonly List<Type>? _referencedCollectionTypes;

        public DataContractSet(ISerializationSurrogateProvider? dataContractSurrogate, IEnumerable<Type>? referencedTypes, IEnumerable<Type>? referencedCollectionTypes)
        {
            _surrogateProvider = dataContractSurrogate;
            _extendedSurrogateProvider = dataContractSurrogate as ISerializationSurrogateProvider2;
            _referencedTypes = referencedTypes != null ? new List<Type>(referencedTypes) : null;
            _referencedCollectionTypes = referencedCollectionTypes != null ? new List<Type>(referencedCollectionTypes) : null;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
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

        public DataContractDictionary Contracts =>
            _contracts ??= new DataContractDictionary();

        public Dictionary<DataContract, object> ProcessedContracts =>
            _processedContracts ??= new Dictionary<DataContract, object>();

        public Hashtable SurrogateData => _surrogateData ??= new Hashtable();

        public DataContractDictionary? KnownTypesForObject
        {
            get => _knownTypesForObject;
            internal set => _knownTypesForObject = value;
        }

        internal static void EnsureTypeNotGeneric(Type type)
        {
            if (type.ContainsGenericParameters)
                throw new InvalidDataContractException(SR.Format(SR.GenericTypeNotExportable, type));
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal void Add(Type type)
        {
            DataContract dataContract = GetDataContract(type);
            EnsureTypeNotGeneric(dataContract.UnderlyingType);
            Add(dataContract);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private void Add(DataContract dataContract)
        {
            Add(dataContract.XmlName, dataContract);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal void Add(XmlQualifiedName name, DataContract dataContract)
        {
            if (dataContract.IsBuiltInDataContract)
                return;
            InternalAdd(name, dataContract);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal void InternalAdd(XmlQualifiedName name, DataContract dataContract)
        {
            if (Contracts.TryGetValue(name, out DataContract? dataContractInSet))
            {
                if (!dataContractInSet.Equals(dataContract))
                {
                    if (dataContract.UnderlyingType == null || dataContractInSet.UnderlyingType == null)
                        throw new InvalidOperationException(SR.Format(SR.DupContractInDataContractSet, dataContract.XmlName.Name, dataContract.XmlName.Namespace));
                    else
                    {
                        bool typeNamesEqual = (DataContract.GetClrTypeFullName(dataContract.UnderlyingType) == DataContract.GetClrTypeFullName(dataContractInSet.UnderlyingType));
                        throw new InvalidOperationException(SR.Format(SR.DupTypeContractInDataContractSet, (typeNamesEqual ? dataContract.UnderlyingType.AssemblyQualifiedName : DataContract.GetClrTypeFullName(dataContract.UnderlyingType)), (typeNamesEqual ? dataContractInSet.UnderlyingType.AssemblyQualifiedName : DataContract.GetClrTypeFullName(dataContractInSet.UnderlyingType)), dataContract.XmlName.Name, dataContract.XmlName.Namespace));
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

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private void AddClassDataContract(ClassDataContract classDataContract)
        {
            if (classDataContract.BaseClassContract != null)
            {
                Add(classDataContract.BaseClassContract.XmlName, classDataContract.BaseClassContract);
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
                                SurrogateData.Add(dataMember, customData);
                        }

                        Add(memberDataContract.XmlName, memberDataContract);
                    }
                }
            }
            AddKnownDataContracts(classDataContract.KnownDataContracts);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private void AddCollectionDataContract(CollectionDataContract collectionDataContract)
        {
            if (collectionDataContract.UnderlyingType != Globals.TypeOfSchemaDefinedType)
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
                        Add(itemContract.XmlName, itemContract);
                }
            }
            AddKnownDataContracts(collectionDataContract.KnownDataContracts);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private void AddXmlDataContract(XmlDataContract xmlDataContract)
        {
            AddKnownDataContracts(xmlDataContract.KnownDataContracts);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private void AddKnownDataContracts(DataContractDictionary? knownDataContracts)
        {
            if (knownDataContracts?.Count > 0)
            {
                foreach (DataContract knownDataContract in knownDataContracts.Values)
                {
                    Add(knownDataContract);
                }
            }
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal XmlQualifiedName GetXmlName(Type clrType)
        {
            if (_surrogateProvider != null)
            {
                Type dcType = DataContractSurrogateCaller.GetDataContractType(_surrogateProvider, clrType);
                return DataContract.GetXmlName(dcType);
            }
            return DataContract.GetXmlName(clrType);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public DataContract GetDataContract(Type type)
        {
            if (_surrogateProvider == null)
                return DataContract.GetDataContract(type);

            DataContract? dataContract = DataContract.GetBuiltInDataContract(type);
            if (dataContract != null)
                return dataContract;

            Type dcType = DataContractSurrogateCaller.GetDataContractType(_surrogateProvider, type);
            dataContract = DataContract.GetDataContract(dcType);
            if (_extendedSurrogateProvider != null && !SurrogateData.Contains(dataContract))
            {
                object? customData = DataContractSurrogateCaller.GetCustomDataToExport(_extendedSurrogateProvider, type, dcType);
                if (customData != null)
                    SurrogateData.Add(dataContract, customData);
            }

            return dataContract;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
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

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal DataContract GetMemberTypeDataContract(DataMember dataMember)
        {
            if (dataMember.MemberInfo is not Type)
            {
                Type dataMemberType = dataMember.MemberType;
                if (dataMember.IsGetOnlyCollection)
                {
                    if (_surrogateProvider != null)
                    {
                        Type dcType = DataContractSurrogateCaller.GetDataContractType(_surrogateProvider, dataMemberType);
                        if (dcType != dataMemberType)
                        {
                            throw new InvalidDataContractException(SR.Format(SR.SurrogatesWithGetOnlyCollectionsNotSupported,
                                DataContract.GetClrTypeFullName(dataMemberType),
                                (dataMember.MemberInfo.DeclaringType != null) ? DataContract.GetClrTypeFullName(dataMember.MemberInfo.DeclaringType) : dataMember.MemberInfo.DeclaringType,
                                dataMember.MemberInfo.Name));
                        }
                    }
                    return DataContract.GetGetOnlyCollectionDataContract(DataContract.GetId(dataMemberType.TypeHandle), dataMemberType.TypeHandle, dataMemberType);
                }
                else
                {
                    return GetDataContract(dataMemberType);
                }
            }
            return dataMember.MemberTypeContract;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal DataContract GetItemTypeDataContract(CollectionDataContract collectionContract)
        {
            if (collectionContract.ItemType != null)
                return GetDataContract(collectionContract.ItemType);
            return collectionContract.ItemContract;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal bool Remove(XmlQualifiedName key)
        {
            if (DataContract.GetBuiltInDataContract(key.Name, key.Namespace) != null)
                return false;
            return Contracts.Remove(key);
        }

        private Dictionary<XmlQualifiedName, object>? _referencedTypesDictionary;
        private Dictionary<XmlQualifiedName, object>? _referencedCollectionTypesDictionary;

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private Dictionary<XmlQualifiedName, object> GetReferencedTypes()
        {
            if (_referencedTypesDictionary == null)
            {
                _referencedTypesDictionary = new Dictionary<XmlQualifiedName, object>();
                //Always include Nullable as referenced type
                //Do not allow surrogating Nullable<T>
                _referencedTypesDictionary.Add(DataContract.GetXmlName(Globals.TypeOfNullable), Globals.TypeOfNullable);
                if (_referencedTypes != null)
                {
                    foreach (Type type in _referencedTypes)
                    {
                        if (type == null)
                            throw new InvalidOperationException(SR.Format(SR.ReferencedTypesCannotContainNull));

                        AddReferencedType(_referencedTypesDictionary, type);
                    }
                }
            }
            return _referencedTypesDictionary;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
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
                            throw new InvalidOperationException(SR.Format(SR.ReferencedCollectionTypesCannotContainNull));
                        AddReferencedType(_referencedCollectionTypesDictionary, type);
                    }
                }
                XmlQualifiedName genericDictionaryName = DataContract.GetXmlName(Globals.TypeOfDictionaryGeneric);
                if (!_referencedCollectionTypesDictionary.ContainsKey(genericDictionaryName) && GetReferencedTypes().ContainsKey(genericDictionaryName))
                    AddReferencedType(_referencedCollectionTypesDictionary, Globals.TypeOfDictionaryGeneric);
            }
            return _referencedCollectionTypesDictionary;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private void AddReferencedType(Dictionary<XmlQualifiedName, object> referencedTypes, Type type)
        {
            if (IsTypeReferenceable(type))
            {
                XmlQualifiedName xmlName;
                try
                {
                    xmlName = GetXmlName(type);
                }
                catch (InvalidDataContractException)
                {
                    // Type not referenceable if we can't get a xml name.
                    return;
                }
                catch (InvalidOperationException)
                {
                    // Type not referenceable if we can't get a xml name.
                    return;
                }

                if (referencedTypes.TryGetValue(xmlName, out object? value))
                {
                    if (value is Type referencedType)
                    {
                        if (referencedType != type)
                        {
                            referencedTypes.Remove(xmlName);
                            List<Type> types = new List<Type>();
                            types.Add(referencedType);
                            types.Add(type);
                            referencedTypes.Add(xmlName, types);
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
                    referencedTypes.Add(xmlName, type);
            }
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private static bool IsTypeReferenceable(Type type)
        {
            try
            {
                return (
#pragma warning disable SYSLIB0050 // Type.IsSerializable is obsolete
                        type.IsSerializable ||
#pragma warning restore SYSLIB0050
                        type.IsDefined(Globals.TypeOfDataContractAttribute, false) ||
                        (Globals.TypeOfIXmlSerializable.IsAssignableFrom(type) && !type.IsGenericTypeDefinition) ||
                        CollectionDataContract.IsCollection(type, out _) ||
                        ClassDataContract.IsNonAttributedTypeValidForSerialization(type));
            }
            catch (Exception ex) when (!ExceptionUtility.IsFatal(ex))
            {
                // An exception can be thrown in the designer when a project has a runtime binding redirection for a referenced assembly or a reference dependent assembly.
                // Type.IsDefined is known to throw System.IO.FileLoadException.
                // ClassDataContract.IsNonAttributedTypeValidForSerialization is known to throw System.IO.FileNotFoundException.
                // We guard against all non-critical exceptions.
            }

            return false;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public Type? GetReferencedType(XmlQualifiedName xmlName, DataContract dataContract, out DataContract? referencedContract, out object[]? genericParameters, bool? supportGenericTypes = null)
        {
            Type? type = GetReferencedTypeInternal(xmlName, dataContract);
            referencedContract = null;
            genericParameters = null;

            if (supportGenericTypes == null)
                return type;

            if (type != null && !type.IsGenericTypeDefinition && !type.ContainsGenericParameters)
                return type;

            if (dataContract.GenericInfo == null)
                return null;

            XmlQualifiedName genericXmlName = dataContract.GenericInfo.GetExpandedXmlName();
            if (genericXmlName != dataContract.XmlName)
                throw new InvalidDataContractException(SR.Format(SR.GenericTypeNameMismatch, dataContract.XmlName.Name, dataContract.XmlName.Namespace, genericXmlName.Name, genericXmlName.Namespace));

            // This check originally came "here" in the old code. Its tempting to move it up with the GenericInfo check.
            if (!supportGenericTypes.Value)
                return null;

            type = GetReferencedGenericTypeInternal(dataContract.GenericInfo, out referencedContract, out genericParameters);
            return type;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private Type? GetReferencedGenericTypeInternal(GenericInfo genInfo, out DataContract? referencedContract, out object[]? genericParameters)
        {
            genericParameters = null;
            referencedContract = null;

            Type? type = GetReferencedTypeInternal(genInfo.XmlName, null);

            if (type == null)
            {
                if (genInfo.Parameters != null)
                    return null;

                referencedContract = GetDataContract(genInfo.XmlName);
                if (referencedContract != null && referencedContract.GenericInfo != null)
                    referencedContract = null;

                return null;    // No type, but maybe we found a suitable referenced contract?
            }

            // We've got a type. But its generic. So we need some parameter contracts.
            // referencedContract is still null, but will be set if we can verify all parameters.
            if (genInfo.Parameters != null)
            {
                bool enableStructureCheck = (type != Globals.TypeOfNullable);
                genericParameters = new object[genInfo.Parameters.Count];
                DataContract[] structureCheckContracts = new DataContract[genInfo.Parameters.Count];
                for (int i = 0; i < genInfo.Parameters.Count; i++)
                {
                    GenericInfo paramInfo = genInfo.Parameters[i];
                    XmlQualifiedName paramXmlName = paramInfo.GetExpandedXmlName();
                    DataContract? paramContract = GetDataContract(paramXmlName);

                    if (paramContract != null)
                    {
                        genericParameters[i] = paramContract;
                    }
                    else
                    {
                        Type? paramType = GetReferencedGenericTypeInternal(paramInfo, out paramContract, out object[]? paramParameters);
                        if (paramType != null)
                        {
                            genericParameters[i] = new Tuple<Type, object[]?>(paramType, paramParameters);
                        }
                        else
                        {
                            genericParameters[i] = paramContract!;
                        }
                    }

                    structureCheckContracts[i] = paramContract!;    // This is ok. If it's null, we disable the use of this array in the next line.
                    if (paramContract == null)
                        enableStructureCheck = false;
                }
                if (enableStructureCheck)
                    referencedContract = DataContract.GetDataContract(type).BindGenericParameters(structureCheckContracts);
            }

            return type;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private Type? GetReferencedTypeInternal(XmlQualifiedName xmlName, DataContract? dataContract)
        {
            Type? type;

            if (dataContract == null)
            {
                if (TryGetReferencedCollectionType(xmlName, null, out type))
                    return type;
                if (TryGetReferencedType(xmlName, null, out type))
                {
                    // enforce that collection types only be specified via ReferencedCollectionTypes
                    if (CollectionDataContract.IsCollection(type))
                        return null;

                    return type;
                }
            }
            else if (dataContract is CollectionDataContract)
            {
                if (TryGetReferencedCollectionType(xmlName, dataContract, out type))
                    return type;
            }
            else
            {
                if (dataContract is XmlDataContract xmlDataContract && xmlDataContract.IsAnonymous)
                    xmlName = SchemaImporter.ImportActualType(xmlDataContract.XsdType?.Annotation, xmlName, dataContract.XmlName);

                if (TryGetReferencedType(xmlName, dataContract, out type))
                    return type;
            }
            return null;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal bool TryGetReferencedType(XmlQualifiedName xmlName, DataContract? dataContract, [NotNullWhen(true)] out Type? type)
        {
            return TryGetReferencedType(xmlName, dataContract, false/*useReferencedCollectionTypes*/, out type);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal bool TryGetReferencedCollectionType(XmlQualifiedName xmlName, DataContract? dataContract, [NotNullWhen(true)] out Type? type)
        {
            return TryGetReferencedType(xmlName, dataContract, true/*useReferencedCollectionTypes*/, out type);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private bool TryGetReferencedType(XmlQualifiedName xmlName, DataContract? dataContract, bool useReferencedCollectionTypes, [NotNullWhen(true)] out Type? type)
        {
            Dictionary<XmlQualifiedName, object> referencedTypes = useReferencedCollectionTypes ? GetReferencedCollectionTypes() : GetReferencedTypes();
            if (referencedTypes.TryGetValue(xmlName, out object? value))
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
                        throw new InvalidOperationException(SR.Format(
                            (useReferencedCollectionTypes ? SR.AmbiguousReferencedCollectionTypes1 : SR.AmbiguousReferencedTypes1),
                            errorMessage.ToString()));
                    }
                    else
                    {
                        throw new InvalidOperationException(SR.Format(
                            (useReferencedCollectionTypes ? SR.AmbiguousReferencedCollectionTypes3 : SR.AmbiguousReferencedTypes3),
                            XmlConvert.DecodeName(xmlName.Name),
                            xmlName.Namespace,
                            errorMessage.ToString()));
                    }
                }
            }
            type = null;
            return false;
        }

        internal ISerializationSurrogateProvider2? SerializationExtendedSurrogateProvider => _extendedSurrogateProvider;

        internal object? GetSurrogateData(object key)
        {
            return SurrogateData[key];
        }

        internal void SetSurrogateData(object key, object? surrogateData)
        {
            SurrogateData[key] = surrogateData;
        }

        internal bool IsContractProcessed(DataContract dataContract)
        {
            return ProcessedContracts.ContainsKey(dataContract);
        }

        internal void SetContractProcessed(DataContract dataContract)
        {
            ProcessedContracts.Add(dataContract, dataContract);
        }

        internal IEnumerator<KeyValuePair<XmlQualifiedName, DataContract>> GetEnumerator()
        {
            return Contracts.GetEnumerator();
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public void ImportSchemaSet(XmlSchemaSet schemaSet, IEnumerable<XmlQualifiedName>? typeNames, bool importXmlDataType)
        {
            SchemaImporter importer = new SchemaImporter(schemaSet, typeNames, null, this, importXmlDataType);
            importer.Import(out _);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public List<XmlQualifiedName> ImportSchemaSet(XmlSchemaSet schemaSet, IEnumerable<XmlSchemaElement> elements, bool importXmlDataType)
        {
            SchemaImporter importer = new SchemaImporter(schemaSet, Array.Empty<XmlQualifiedName>() /* Needs to be empty, not null for 'elements' to be used. */, elements, this, importXmlDataType);
            importer.Import(out List<XmlQualifiedName>? elementNames);
            return elementNames!;   // Not null when we have provided non-null 'typeNames' and 'elements'
        }
    }
}
