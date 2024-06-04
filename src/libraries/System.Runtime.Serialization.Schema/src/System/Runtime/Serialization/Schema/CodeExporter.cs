// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization.DataContracts;
using System.Security;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

using DataContractDictionary = System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, System.Runtime.Serialization.DataContracts.DataContract>;
using ExceptionUtil = System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility;

namespace System.Runtime.Serialization
{
    internal sealed class CodeExporter
    {
        private const string WildcardNamespaceMapping = "*";
        private const string TypeNameFieldName = "typeName";
        private const int MaxIdentifierLength = 511;

        private static readonly object s_codeUserDataActualTypeKey = new object();
        private static readonly object s_surrogateDataKey = typeof(ISerializationSurrogateProvider2);

        private readonly DataContractSet _dataContractSet;
        private readonly CodeCompileUnit _codeCompileUnit;
        private readonly ImportOptions? _options;
        private readonly Dictionary<string, string> _namespaces;
        private readonly Dictionary<string, string?> _clrNamespaces;

        internal CodeExporter(DataContractSet dataContractSet, ImportOptions? options, CodeCompileUnit codeCompileUnit)
        {
            _dataContractSet = dataContractSet;
            _codeCompileUnit = codeCompileUnit;
            AddReferencedAssembly(Assembly.GetExecutingAssembly());
            _options = options;
            _namespaces = new Dictionary<string, string>();
            _clrNamespaces = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            // Update namespace tables for DataContract(s) that are already processed
            foreach (KeyValuePair<XmlQualifiedName, DataContract> pair in dataContractSet.Contracts)
            {
                DataContract dataContract = pair.Value;
                if (!(dataContract.IsBuiltInDataContract || dataContract.Is(DataContractType.ClassDataContract)))
                {
                    ContractCodeDomInfo contractCodeDomInfo = GetContractCodeDomInfo(dataContract);
                    if (contractCodeDomInfo.IsProcessed && !contractCodeDomInfo.UsesWildcardNamespace)
                    {
                        string? clrNamespace = contractCodeDomInfo.ClrNamespace;
                        if (clrNamespace != null && !_clrNamespaces.ContainsKey(clrNamespace))
                        {
                            _clrNamespaces.Add(clrNamespace, dataContract.XmlName.Namespace);
                            _namespaces.Add(dataContract.XmlName.Namespace, clrNamespace);
                        }
                    }
                }
            }

            // Copy options.Namespaces to namespace tables
            if (_options != null)
            {
                foreach (KeyValuePair<string, string> pair in _options.Namespaces)
                {
                    string dataContractNamespace = pair.Key;
                    string clrNamespace = pair.Value ?? string.Empty;

                    string? currentDataContractNamespace;
                    if (_clrNamespaces.TryGetValue(clrNamespace, out currentDataContractNamespace))
                    {
                        if (dataContractNamespace != currentDataContractNamespace)
                            throw ExceptionUtil.ThrowHelperError(new InvalidOperationException(SR.Format(SR.CLRNamespaceMappedMultipleTimes, currentDataContractNamespace, dataContractNamespace, clrNamespace)));
                    }
                    else
                        _clrNamespaces.Add(clrNamespace, dataContractNamespace);

                    string? currentClrNamespace;
                    if (_namespaces.TryGetValue(dataContractNamespace, out currentClrNamespace))
                    {
                        if (clrNamespace != currentClrNamespace)
                        {
                            _namespaces.Remove(dataContractNamespace);
                            _namespaces.Add(dataContractNamespace, clrNamespace);
                        }
                    }
                    else
                        _namespaces.Add(dataContractNamespace, clrNamespace);
                }
            }

            // Update namespace tables for pre-existing namespaces in CodeCompileUnit
            foreach (CodeNamespace codeNS in codeCompileUnit.Namespaces)
            {
                string ns = codeNS.Name ?? string.Empty;
                if (!_clrNamespaces.ContainsKey(ns))
                {
                    _clrNamespaces.Add(ns, null);
                }
                if (ns.Length == 0)
                {
                    foreach (CodeTypeDeclaration codeTypeDecl in codeNS.Types)
                    {
                        AddGlobalTypeName(codeTypeDecl.Name);
                    }
                }
            }

        }

        private void AddReferencedAssembly(Assembly assembly)
        {
            bool alreadyExisting = false;
#pragma warning disable IL3000 // Avoid accessing Assembly file path when publishing as a single file
            string assemblyName = System.IO.Path.GetFileName(assembly.Location);
            if (string.IsNullOrWhiteSpace(assemblyName))
                assemblyName = $"[{assembly.FullName}]";
#pragma warning restore IL3000 // Avoid accessing Assembly file path when publishing as a single file

            foreach (string? existingName in _codeCompileUnit.ReferencedAssemblies)
            {
                if (string.Equals(existingName, assemblyName, StringComparison.OrdinalIgnoreCase))
                {
                    alreadyExisting = true;
                    break;
                }
            }
            if (!alreadyExisting)
                _codeCompileUnit.ReferencedAssemblies.Add(assemblyName);

        }

        private bool GenerateSerializableTypes
        {
            get { return (_options == null) ? false : _options.GenerateSerializable; }
        }

        private bool GenerateInternalTypes
        {
            get { return (_options == null) ? false : _options.GenerateInternal; }
        }

        private bool EnableDataBinding
        {
            get { return (_options == null) ? false : _options.EnableDataBinding; }
        }

        private CodeDomProvider? CodeProvider
        {
            get { return _options?.CodeProvider; }
        }

        private bool SupportsDeclareEvents
        {
            get { return (CodeProvider == null) ? true : CodeProvider.Supports(GeneratorSupport.DeclareEvents); }
        }

        private bool SupportsDeclareValueTypes
        {
            get { return (CodeProvider == null) ? true : CodeProvider.Supports(GeneratorSupport.DeclareValueTypes); }
        }

        private bool SupportsGenericTypeReference
        {
            get { return (CodeProvider == null) ? true : CodeProvider.Supports(GeneratorSupport.GenericTypeReference); }
        }

        private bool SupportsAssemblyAttributes
        {
            get { return (CodeProvider == null) ? true : CodeProvider.Supports(GeneratorSupport.AssemblyAttributes); }
        }

        private bool SupportsPartialTypes
        {
            get { return (CodeProvider == null) ? true : CodeProvider.Supports(GeneratorSupport.PartialTypes); }
        }

        private bool SupportsNestedTypes
        {
            get { return (CodeProvider == null) ? true : CodeProvider.Supports(GeneratorSupport.NestedTypes); }
        }

        private string FileExtension
        {
            get { return (CodeProvider == null) ? string.Empty : CodeProvider.FileExtension; }
        }

        private Dictionary<string, string> Namespaces
        {
            get { return _namespaces; }
        }

        private Dictionary<string, string?> ClrNamespaces
        {
            get { return _clrNamespaces; }
        }

        [RequiresUnreferencedCode(ImportGlobals.SerializerTrimmerWarning)]
        internal void Export()
        {
            try
            {
                foreach (KeyValuePair<XmlQualifiedName, DataContract> pair in _dataContractSet.Contracts)
                {
                    DataContract dataContract = pair.Value;
                    if (dataContract.IsBuiltInDataContract)
                        continue;

                    ContractCodeDomInfo contractCodeDomInfo = GetContractCodeDomInfo(dataContract);
                    if (!contractCodeDomInfo.IsProcessed)
                    {
                        switch (dataContract.GetContractType())
                        {
                            case DataContractType.ClassDataContract:
                                if (dataContract.IsISerializable)
                                    ExportISerializableDataContract(dataContract, contractCodeDomInfo);
                                else
                                    ExportClassDataContractHierarchy(dataContract.XmlName, dataContract, contractCodeDomInfo, new Dictionary<XmlQualifiedName, object?>());
                                break;
                            case DataContractType.CollectionDataContract:
                                ExportCollectionDataContract(dataContract, contractCodeDomInfo);
                                break;
                            case DataContractType.EnumDataContract:
                                ExportEnumDataContract(dataContract, contractCodeDomInfo);
                                break;
                            default:
                                if (dataContract is XmlDataContract xmlDataContract)
                                    ExportXmlDataContract(xmlDataContract, contractCodeDomInfo);
                                else
                                    throw ExceptionUtil.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.UnexpectedContractType, GetClrTypeFullName(dataContract.GetType()), GetClrTypeFullName(dataContract.UnderlyingType))));
                                break;
                        };
                        contractCodeDomInfo.IsProcessed = true;
                    }
                }

                if (_options?.DataContractSurrogate is ISerializationCodeDomSurrogateProvider cdSurrogateProvider)
                {
                    CodeNamespace[] namespaces = new CodeNamespace[_codeCompileUnit.Namespaces.Count];
                    _codeCompileUnit.Namespaces.CopyTo(namespaces, 0);
                    foreach (CodeNamespace codeNamespace in namespaces)
                        InvokeProcessImportedType(codeNamespace.Types, cdSurrogateProvider);
                }
            }
            finally
            {
                CodeGenerator.ValidateIdentifiers(_codeCompileUnit);
            }
        }

        [RequiresUnreferencedCode(ImportGlobals.SerializerTrimmerWarning)]
        private void ExportClassDataContractHierarchy(XmlQualifiedName typeName, DataContract classContract, ContractCodeDomInfo contractCodeDomInfo, Dictionary<XmlQualifiedName, object?> contractNamesInHierarchy)
        {
            Debug.Assert(classContract.Is(DataContractType.ClassDataContract));

            if (contractNamesInHierarchy.ContainsKey(classContract.XmlName))
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.TypeCannotBeImported, typeName.Name, typeName.Namespace, SR.Format(SR.CircularTypeReference, classContract.XmlName.Name, classContract.XmlName.Namespace))));
            contractNamesInHierarchy.Add(classContract.XmlName, null);

            DataContract? baseContract = classContract.BaseContract;
            if (baseContract != null)
            {
                ContractCodeDomInfo baseContractCodeDomInfo = GetContractCodeDomInfo(baseContract);
                if (!baseContractCodeDomInfo.IsProcessed)
                {
                    ExportClassDataContractHierarchy(typeName, baseContract, baseContractCodeDomInfo, contractNamesInHierarchy);
                    baseContractCodeDomInfo.IsProcessed = true;
                }
            }
            ExportClassDataContract(classContract, contractCodeDomInfo);
        }

        private void InvokeProcessImportedType(CollectionBase collection, ISerializationCodeDomSurrogateProvider surrogateProvider)
        {
            object[] objects = new object[collection.Count];
            ((ICollection)collection).CopyTo(objects, 0);

            foreach (object obj in objects)
            {
                if (obj is CodeTypeDeclaration codeTypeDeclaration)
                {
                    CodeTypeDeclaration? newCodeTypeDeclaration = surrogateProvider.ProcessImportedType(codeTypeDeclaration, _codeCompileUnit);

                    if (newCodeTypeDeclaration != codeTypeDeclaration)
                    {
                        ((IList)collection).Remove(codeTypeDeclaration);
                        if (newCodeTypeDeclaration != null)
                            ((IList)collection).Add(newCodeTypeDeclaration);
                    }
                    if (newCodeTypeDeclaration != null)
                        InvokeProcessImportedType(newCodeTypeDeclaration.Members, surrogateProvider);
                }
            }
        }

        [RequiresUnreferencedCode(ImportGlobals.SerializerTrimmerWarning)]
        internal CodeTypeReference GetCodeTypeReference(DataContract dataContract)
        {
            if (dataContract.IsBuiltInDataContract)
                return GetCodeTypeReference(dataContract.UnderlyingType);

            ContractCodeDomInfo contractCodeDomInfo = GetContractCodeDomInfo(dataContract);
            GenerateType(dataContract, contractCodeDomInfo);

            // This is supposed to be set by GenerateType. If it wasn't, there is a problem.
            Debug.Assert(contractCodeDomInfo.TypeReference != null);
            return contractCodeDomInfo.TypeReference!;
        }

        private CodeTypeReference GetCodeTypeReference(Type type)
        {
            AddReferencedAssembly(type.Assembly);
            return new CodeTypeReference(type);
        }

        [RequiresUnreferencedCode(ImportGlobals.SerializerTrimmerWarning)]
        private CodeTypeReference? GetCodeTypeReference(Type type, IList? parameters)
        {
            CodeTypeReference codeTypeReference = GetCodeTypeReference(type);

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    CodeTypeReference? paramTypeReference = null;
                    bool isParamValueType = true;   // Default not important. It either gets set, or paramTypeReference stays null and we short circuit before referencing this value.

                    if (param is DataContract paramContract)
                    {
                        paramTypeReference = GetCodeTypeReference(paramContract);
                        isParamValueType = paramContract.IsValueType;
                    }
                    else if (param is Tuple<Type, object[]?> typeParameters)
                    {
                        paramTypeReference = GetCodeTypeReference(typeParameters.Item1, typeParameters.Item2);
                        isParamValueType = (paramTypeReference != null && paramTypeReference.ArrayRank == 0); // only value type information we can get from CodeTypeReference
                    }

                    if (paramTypeReference == null)
                        return null;
                    if (type == typeof(Nullable<>) && !isParamValueType)
                        return paramTypeReference;
                    else
                        codeTypeReference.TypeArguments.Add(paramTypeReference);
                }
            }

            return codeTypeReference;
        }

        [RequiresUnreferencedCode(ImportGlobals.SerializerTrimmerWarning)]
        internal CodeTypeReference GetElementTypeReference(DataContract dataContract, bool isElementTypeNullable)
        {
            CodeTypeReference elementTypeReference = GetCodeTypeReference(dataContract);
            if (dataContract.IsValueType && isElementTypeNullable)
                elementTypeReference = WrapNullable(elementTypeReference);
            return elementTypeReference;
        }

        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Part of a logical set of properties, some of which are not static. May not remain static with future implementation updates.")]
        private XmlQualifiedName GenericListName
        {
            [RequiresUnreferencedCode(ImportGlobals.SerializerTrimmerWarning)]
            get { return DataContract.GetXmlName(typeof(List<>)); }
        }

        private DataContract GenericListContract
        {
            [RequiresUnreferencedCode(ImportGlobals.SerializerTrimmerWarning)]
            get { return _dataContractSet.GetDataContract(typeof(List<>)); }
        }

        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Part of a logical set of properties, some of which are not static. May not remain static with future implementation updates.")]
        private XmlQualifiedName GenericDictionaryName
        {
            [RequiresUnreferencedCode(ImportGlobals.SerializerTrimmerWarning)]
            get { return DataContract.GetXmlName(typeof(Dictionary<,>)); }
        }

        private DataContract GenericDictionaryContract
        {
            [RequiresUnreferencedCode(ImportGlobals.SerializerTrimmerWarning)]
            get { return _dataContractSet.GetDataContract(typeof(Dictionary<,>)); }
        }

        private ContractCodeDomInfo GetContractCodeDomInfo(DataContract dataContract)
        {
            ContractCodeDomInfo? contractCodeDomInfo = null;
            if (_dataContractSet.ProcessedContracts.TryGetValue(dataContract, out object? info))
                contractCodeDomInfo = info as ContractCodeDomInfo;
            if (contractCodeDomInfo == null)
            {
                contractCodeDomInfo = new ContractCodeDomInfo();
                _dataContractSet.ProcessedContracts.Add(dataContract, contractCodeDomInfo);
            }
            return contractCodeDomInfo;
        }

        [RequiresUnreferencedCode(ImportGlobals.SerializerTrimmerWarning)]
        private void GenerateType(DataContract dataContract, ContractCodeDomInfo contractCodeDomInfo)
        {
            if (!contractCodeDomInfo.IsProcessed)
            {
                CodeTypeReference? referencedType = GetReferencedType(dataContract);
                if (referencedType != null)
                {
                    contractCodeDomInfo.TypeReference = referencedType;
                    contractCodeDomInfo.ReferencedTypeExists = true;
                }
                else
                {
                    CodeTypeDeclaration? type = contractCodeDomInfo.TypeDeclaration;
                    if (type == null)
                    {
                        string clrNamespace = GetClrNamespace(dataContract, contractCodeDomInfo);
                        CodeNamespace ns = GetCodeNamespace(clrNamespace, dataContract.XmlName.Namespace, contractCodeDomInfo);
                        type = GetNestedType(dataContract, contractCodeDomInfo);
                        if (type == null)
                        {
                            string typeName = XmlConvert.DecodeName(dataContract.XmlName.Name);
                            typeName = GetClrIdentifier(typeName, ImportGlobals.DefaultTypeName);
                            if (NamespaceContainsType(ns, typeName) || GlobalTypeNameConflicts(clrNamespace, typeName))
                            {
                                for (int i = 1; ; i++)
                                {
                                    string uniqueName = AppendToValidClrIdentifier(typeName, i.ToString(NumberFormatInfo.InvariantInfo));
                                    if (!NamespaceContainsType(ns, uniqueName) && !GlobalTypeNameConflicts(clrNamespace, uniqueName))
                                    {
                                        typeName = uniqueName;
                                        break;
                                    }
                                    if (i == int.MaxValue)
                                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.CannotComputeUniqueName, typeName)));
                                }
                            }

                            type = CreateTypeDeclaration(typeName, dataContract);
                            ns.Types.Add(type);
                            if (string.IsNullOrEmpty(clrNamespace))
                            {
                                AddGlobalTypeName(typeName);
                            }
                            contractCodeDomInfo.TypeReference = new CodeTypeReference((clrNamespace == null || clrNamespace.Length == 0) ? typeName : clrNamespace + "." + typeName);

                            if (GenerateInternalTypes)
                                type.TypeAttributes = TypeAttributes.NotPublic;
                            else
                                type.TypeAttributes = TypeAttributes.Public;
                        }

                        if (_options?.DataContractSurrogate != null)
                            type.UserData.Add(s_surrogateDataKey, _dataContractSet.SurrogateData[dataContract]);

                        contractCodeDomInfo.TypeDeclaration = type;
                    }
                }
            }
        }

        [RequiresUnreferencedCode(ImportGlobals.SerializerTrimmerWarning)]
        private CodeTypeDeclaration? GetNestedType(DataContract dataContract, ContractCodeDomInfo contractCodeDomInfo)
        {
            if (!SupportsNestedTypes)
                return null;
            string originalName = dataContract.XmlName.Name;
            int nestedTypeIndex = originalName.LastIndexOf('.');
            if (nestedTypeIndex <= 0)
                return null;
            string containingTypeName = originalName.Substring(0, nestedTypeIndex);
            DataContract? containingDataContract = _dataContractSet.GetDataContract(new XmlQualifiedName(containingTypeName, dataContract.XmlName.Namespace));
            if (containingDataContract == null)
                return null;
            string nestedTypeName = XmlConvert.DecodeName(originalName.Substring(nestedTypeIndex + 1));
            nestedTypeName = GetClrIdentifier(nestedTypeName, ImportGlobals.DefaultTypeName);

            ContractCodeDomInfo containingContractCodeDomInfo = GetContractCodeDomInfo(containingDataContract);
            GenerateType(containingDataContract, containingContractCodeDomInfo);
            if (containingContractCodeDomInfo.ReferencedTypeExists)
                return null;

            CodeTypeDeclaration containingType = containingContractCodeDomInfo.TypeDeclaration!; // Nested types by definition have containing types.
            if (TypeContainsNestedType(containingType, nestedTypeName))
            {
                for (int i = 1; ; i++)
                {
                    string uniqueName = AppendToValidClrIdentifier(nestedTypeName, i.ToString(NumberFormatInfo.InvariantInfo));
                    if (!TypeContainsNestedType(containingType, uniqueName))
                    {
                        nestedTypeName = uniqueName;
                        break;
                    }
                }
            }

            CodeTypeDeclaration type = CreateTypeDeclaration(nestedTypeName, dataContract);
            containingType.Members.Add(type);
            contractCodeDomInfo.TypeReference = new CodeTypeReference(containingContractCodeDomInfo.TypeReference!.BaseType + "+" + nestedTypeName); // Again, nested types by definition have containing types.

            if (GenerateInternalTypes)
                type.TypeAttributes = TypeAttributes.NestedAssembly;
            else
                type.TypeAttributes = TypeAttributes.NestedPublic;
            return type;
        }

        private static CodeTypeDeclaration CreateTypeDeclaration(string typeName, DataContract dataContract)
        {
            CodeTypeDeclaration typeDecl = new CodeTypeDeclaration(typeName);
            CodeAttributeDeclaration debuggerStepThroughAttribute = new CodeAttributeDeclaration(typeof(System.Diagnostics.DebuggerStepThroughAttribute).FullName!);
            CodeAttributeDeclaration generatedCodeAttribute = new CodeAttributeDeclaration(typeof(GeneratedCodeAttribute).FullName!);

            AssemblyName assemblyName = Assembly.GetExecutingAssembly().GetName();
            generatedCodeAttribute.Arguments.Add(new CodeAttributeArgument(new CodePrimitiveExpression(assemblyName.Name!)));
            generatedCodeAttribute.Arguments.Add(new CodeAttributeArgument(new CodePrimitiveExpression(assemblyName.Version?.ToString()!)));

            // System.Diagnostics.DebuggerStepThroughAttribute not allowed on enums
            // ensure that the attribute is only generated on types that are not enums
            if (!dataContract.Is(DataContractType.EnumDataContract))
            {
                typeDecl.CustomAttributes.Add(debuggerStepThroughAttribute);
            }
            typeDecl.CustomAttributes.Add(generatedCodeAttribute);
            return typeDecl;
        }

        [RequiresUnreferencedCode(ImportGlobals.SerializerTrimmerWarning)]
        private CodeTypeReference? GetReferencedType(DataContract dataContract)
        {
            CodeTypeReference? typeReference = GetSurrogatedTypeReference(dataContract);
            if (typeReference != null)
                return typeReference;

            Type? type = _dataContractSet.GetReferencedType(dataContract.XmlName, dataContract, out DataContract? referencedContract, out object[]? parameters, SupportsGenericTypeReference);
            if (type != null && !type.IsGenericTypeDefinition && !type.ContainsGenericParameters)
            {
                if (dataContract is XmlDataContract xmlContract)
                {
                    if (typeof(IXmlSerializable).IsAssignableFrom(type))
                    {
                        if (xmlContract.IsTypeDefinedOnImport)
                        {
                            if (!xmlContract.Equals(_dataContractSet.GetDataContract(type)))
                                throw ExceptionUtil.ThrowHelperError(new InvalidOperationException(SR.Format(SR.ReferencedTypeDoesNotMatch, type.AssemblyQualifiedName, dataContract.XmlName.Name, dataContract.XmlName.Namespace)));
                        }
                        else
                        {
                            xmlContract.IsValueType = type.IsValueType;
                            xmlContract.IsTypeDefinedOnImport = true;
                        }
                        return GetCodeTypeReference(type);
                    }
                    throw ExceptionUtil.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.TypeMustBeIXmlSerializable, GetClrTypeFullName(type), GetClrTypeFullName(typeof(IXmlSerializable)), dataContract.XmlName.Name, dataContract.XmlName.Namespace)));
                }
                referencedContract = _dataContractSet.GetDataContract(type);
                if (referencedContract.Equals(dataContract))
                {
                    typeReference = GetCodeTypeReference(type);
                    typeReference.UserData.Add(s_codeUserDataActualTypeKey, type);
                    return typeReference;
                }
                throw ExceptionUtil.ThrowHelperError(new InvalidOperationException(SR.Format(SR.ReferencedTypeDoesNotMatch, type.AssemblyQualifiedName, dataContract.XmlName.Name, dataContract.XmlName.Namespace)));
            }
            else if (type != null)
            {
                typeReference = GetCodeTypeReference(type, parameters);

                if (referencedContract != null && !referencedContract.Equals(dataContract))
                {
                    Debug.Assert(typeReference != null);
                    type = (Type?)typeReference.UserData[s_codeUserDataActualTypeKey];
                    throw ExceptionUtil.ThrowHelperError(new InvalidOperationException(SR.Format(SR.ReferencedTypeDoesNotMatch,
                        type?.AssemblyQualifiedName,
                        referencedContract.XmlName.Name,
                        referencedContract.XmlName.Namespace)));
                }

                return typeReference;
            }
            else if (referencedContract != null)
            {
                typeReference = GetCodeTypeReference(referencedContract);
                return typeReference;
            }

            return GetReferencedCollectionType(dataContract.As(DataContractType.CollectionDataContract));
        }

        [RequiresUnreferencedCode(ImportGlobals.SerializerTrimmerWarning)]
        private CodeTypeReference? GetReferencedCollectionType(DataContract? collectionContract)
        {
            if (collectionContract == null)
                return null;

            Debug.Assert(collectionContract.Is(DataContractType.CollectionDataContract));

            if (HasDefaultCollectionNames(collectionContract))
            {
                CodeTypeReference? typeReference;
                if (!TryGetReferencedDictionaryType(collectionContract, out typeReference))
                {
                    // ItemContract - aka BaseContract - is never null for CollectionDataContract
                    DataContract itemContract = collectionContract.BaseContract!;
                    if (collectionContract.IsDictionaryLike(out _, out _, out _))
                    {
                        GenerateKeyValueType(itemContract.As(DataContractType.ClassDataContract));
                    }
                    bool isItemTypeNullable = collectionContract.IsItemTypeNullable();
                    if (!TryGetReferencedListType(itemContract, isItemTypeNullable, out typeReference))
                    {
                        CodeTypeReference? elementTypeReference = GetElementTypeReference(itemContract, isItemTypeNullable);
                        if (elementTypeReference != null)
                            typeReference = new CodeTypeReference(elementTypeReference, 1);
                    }
                }
                return typeReference;
            }
            return null;
        }

        [RequiresUnreferencedCode(ImportGlobals.SerializerTrimmerWarning)]
        private static bool HasDefaultCollectionNames(DataContract collectionContract)
        {
            Debug.Assert(collectionContract.Is(DataContractType.CollectionDataContract));

            // ItemContract - aka BaseContract - is never null for CollectionDataContract
            DataContract itemContract = collectionContract.BaseContract!;
            bool isDictionary = collectionContract.IsDictionaryLike(out string? keyName, out string? valueName, out string? itemName);
            if (itemName != itemContract.XmlName.Name)
                return false;

            if (isDictionary && (keyName != ImportGlobals.KeyLocalName || valueName != ImportGlobals.ValueLocalName))
                return false;

            XmlQualifiedName expectedType = itemContract.GetArrayTypeName(collectionContract.IsItemTypeNullable());
            return (collectionContract.XmlName.Name == expectedType.Name && collectionContract.XmlName.Namespace == expectedType.Namespace);
        }

        [RequiresUnreferencedCode(ImportGlobals.SerializerTrimmerWarning)]
        private bool TryGetReferencedDictionaryType(DataContract collectionContract, [NotNullWhen(true)] out CodeTypeReference? typeReference)
        {
            Debug.Assert(collectionContract.Is(DataContractType.CollectionDataContract));

            // Check if it is a dictionary and use referenced dictionary type if present
            if (collectionContract.IsDictionaryLike(out _, out _, out _)
                && SupportsGenericTypeReference)
            {
                Type? type = _dataContractSet.GetReferencedType(GenericDictionaryName, GenericDictionaryContract, out DataContract? _, out object[]? _) ?? typeof(Dictionary<,>);

                // ItemContract - aka BaseContract - is never null for CollectionDataContract
                DataContract? itemContract = collectionContract.BaseContract!.As(DataContractType.ClassDataContract);

                // A dictionary should have a Key/Value item contract that has at least two members: key and value.
                Debug.Assert(itemContract != null);
                Debug.Assert(itemContract.DataMembers.Count >= 2);

                DataMember keyMember = itemContract.DataMembers[0];
                DataMember valueMember = itemContract.DataMembers[1];
                CodeTypeReference? keyTypeReference = GetElementTypeReference(keyMember.MemberTypeContract, keyMember.IsNullable);
                CodeTypeReference? valueTypeReference = GetElementTypeReference(valueMember.MemberTypeContract, valueMember.IsNullable);
                if (keyTypeReference != null && valueTypeReference != null)
                {
                    typeReference = GetCodeTypeReference(type);
                    typeReference.TypeArguments.Add(keyTypeReference);
                    typeReference.TypeArguments.Add(valueTypeReference);
                    return true;
                }
            }
            typeReference = null;
            return false;
        }

        [RequiresUnreferencedCode(ImportGlobals.SerializerTrimmerWarning)]
        private bool TryGetReferencedListType(DataContract itemContract, bool isItemTypeNullable, out CodeTypeReference? typeReference)
        {
            if (SupportsGenericTypeReference)
            {
                Type? type = _dataContractSet.GetReferencedType(GenericListName, GenericListContract, out DataContract? _, out object[]? _);
                if (type != null)
                {
                    typeReference = GetCodeTypeReference(type);
                    typeReference.TypeArguments.Add(GetElementTypeReference(itemContract, isItemTypeNullable)!);    // Lists have an item type
                    return true;
                }
            }
            typeReference = null;
            return false;
        }

        [RequiresUnreferencedCode(ImportGlobals.SerializerTrimmerWarning)]
        private CodeTypeReference? GetSurrogatedTypeReference(DataContract dataContract)
        {
            Type? type = GetReferencedTypeOnImport(dataContract);
            if (type != null)
            {
                CodeTypeReference typeReference = GetCodeTypeReference(type);
                typeReference.UserData.Add(s_codeUserDataActualTypeKey, type);
                return typeReference;
            }
            return null;
        }

        [RequiresUnreferencedCode(ImportGlobals.SerializerTrimmerWarning)]
        private Type? GetReferencedTypeOnImport(DataContract dataContract)
        {
            Type? type = null;
            if (_options?.DataContractSurrogate is ISerializationSurrogateProvider2 surrogateProvider)
            {
                if (DataContract.GetBuiltInDataContract(dataContract.XmlName.Name, dataContract.XmlName.Namespace) == null)
                    type = surrogateProvider.GetReferencedTypeOnImport(dataContract.XmlName.Name, dataContract.XmlName.Namespace, _dataContractSet.SurrogateData[dataContract]);
            }
            return type;
        }

        private static bool NamespaceContainsType(CodeNamespace ns, string typeName)
        {
            foreach (CodeTypeDeclaration type in ns.Types)
            {
                if (string.Equals(typeName, type.Name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private bool GlobalTypeNameConflicts(string clrNamespace, string typeName)
        {
            return (string.IsNullOrEmpty(clrNamespace) && _clrNamespaces.ContainsKey(typeName));
        }

        private void AddGlobalTypeName(string typeName)
        {
            if (!_clrNamespaces.ContainsKey(typeName))
            {
                _clrNamespaces.Add(typeName, null);
            }
        }

        private static bool TypeContainsNestedType(CodeTypeDeclaration containingType, string typeName)
        {
            foreach (CodeTypeMember member in containingType.Members)
            {
                if (member is CodeTypeDeclaration declaration)
                {
                    if (string.Equals(typeName, declaration.Name, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            return false;
        }

        private static string GetNameForAttribute(string name)
        {
            string decodedName = XmlConvert.DecodeName(name);
            if (name == decodedName)
                return name;

            string reencodedName = SchemaImportHelper.EncodeLocalName(decodedName);
            return name == reencodedName ? decodedName : name;
        }

        private void AddSerializableAttribute(bool generateSerializable, CodeTypeDeclaration type, ContractCodeDomInfo contractCodeDomInfo)
        {
            if (generateSerializable)
            {
                type.CustomAttributes.Add(SerializableAttribute);
                AddImportStatement(typeof(SerializableAttribute).Namespace, contractCodeDomInfo.CodeNamespace);
            }
        }

        [RequiresUnreferencedCode(ImportGlobals.SerializerTrimmerWarning)]
        private void ExportClassDataContract(DataContract classDataContract, ContractCodeDomInfo contractCodeDomInfo)
        {
            Debug.Assert(classDataContract.Is(DataContractType.ClassDataContract));

            GenerateType(classDataContract, contractCodeDomInfo);
            if (contractCodeDomInfo.ReferencedTypeExists)
                return;

            // This is supposed to be set by GenerateType. If it wasn't, there is a problem.
            Debug.Assert(contractCodeDomInfo.TypeDeclaration != null);

            CodeTypeDeclaration type = contractCodeDomInfo.TypeDeclaration;
            if (SupportsPartialTypes)
                type.IsPartial = true;
            if (classDataContract.IsValueType && SupportsDeclareValueTypes)
                type.IsStruct = true;
            else
                type.IsClass = true;

            string dataContractName = GetNameForAttribute(classDataContract.XmlName.Name);
            CodeAttributeDeclaration dataContractAttribute = new CodeAttributeDeclaration(GetClrTypeFullName(typeof(DataContractAttribute)));
            dataContractAttribute.Arguments.Add(new CodeAttributeArgument(ImportGlobals.NameProperty, new CodePrimitiveExpression(dataContractName)));
            dataContractAttribute.Arguments.Add(new CodeAttributeArgument(ImportGlobals.NamespaceProperty, new CodePrimitiveExpression(classDataContract.XmlName.Namespace)));
            if (classDataContract.IsReference != ImportGlobals.DefaultIsReference)
                dataContractAttribute.Arguments.Add(new CodeAttributeArgument(ImportGlobals.IsReferenceProperty, new CodePrimitiveExpression(classDataContract.IsReference)));
            type.CustomAttributes.Add(dataContractAttribute);
            AddImportStatement(typeof(DataContractAttribute).Namespace, contractCodeDomInfo.CodeNamespace);

            AddSerializableAttribute(GenerateSerializableTypes, type, contractCodeDomInfo);

            AddKnownTypes(classDataContract, contractCodeDomInfo);

            bool raisePropertyChanged = EnableDataBinding && SupportsDeclareEvents;
            if (classDataContract.BaseContract == null)
            {
                if (!type.IsStruct)
                    type.BaseTypes.Add(typeof(object));
                AddExtensionData(contractCodeDomInfo);
                AddPropertyChangedNotifier(contractCodeDomInfo, type.IsStruct);
            }
            else
            {
                ContractCodeDomInfo baseContractCodeDomInfo = GetContractCodeDomInfo(classDataContract.BaseContract);
                Debug.Assert(baseContractCodeDomInfo.IsProcessed, "Cannot generate code for type if code for base type has not been generated");
                type.BaseTypes.Add(baseContractCodeDomInfo.TypeReference!);
                AddBaseMemberNames(baseContractCodeDomInfo, contractCodeDomInfo);
                if (baseContractCodeDomInfo.ReferencedTypeExists)
                {
                    Type? actualType = (Type?)baseContractCodeDomInfo.TypeReference?.UserData[s_codeUserDataActualTypeKey];
                    Debug.Assert(actualType != null);   // If we're in this if condition, then we should be able to get a Type
                    ThrowIfReferencedBaseTypeSealed(actualType, classDataContract);
                    if (!typeof(IExtensibleDataObject).IsAssignableFrom(actualType))
                        AddExtensionData(contractCodeDomInfo);
                    if (!typeof(INotifyPropertyChanged).IsAssignableFrom(actualType))
                    {
                        AddPropertyChangedNotifier(contractCodeDomInfo, type.IsStruct);
                    }
                    else
                    {
                        raisePropertyChanged = false;
                    }
                }
            }

            for (int i = 0; i < classDataContract.DataMembers.Count; i++)
            {
                DataMember dataMember = classDataContract.DataMembers[i];

                CodeTypeReference memberType = GetElementTypeReference(dataMember.MemberTypeContract,
                    (dataMember.IsNullable && dataMember.MemberTypeContract.IsValueType));

                string dataMemberName = GetNameForAttribute(dataMember.Name);
                string propertyName = GetMemberName(dataMemberName, contractCodeDomInfo);
                string fieldName = GetMemberName(AppendToValidClrIdentifier(propertyName, ImportGlobals.DefaultFieldSuffix), contractCodeDomInfo);

                CodeMemberField field = new CodeMemberField();
                field.Type = memberType;
                field.Name = fieldName;
                field.Attributes = MemberAttributes.Private;

                CodeMemberProperty property = CreateProperty(memberType, propertyName, fieldName, dataMember.MemberTypeContract.IsValueType && SupportsDeclareValueTypes, raisePropertyChanged);
                object? surrogateData = _dataContractSet.SurrogateData[dataMember];
                if (surrogateData != null)
                    property.UserData.Add(s_surrogateDataKey, surrogateData);

                CodeAttributeDeclaration dataMemberAttribute = new CodeAttributeDeclaration(GetClrTypeFullName(typeof(DataMemberAttribute)));
                if (dataMemberName != property.Name)
                    dataMemberAttribute.Arguments.Add(new CodeAttributeArgument(ImportGlobals.NameProperty, new CodePrimitiveExpression(dataMemberName)));
                if (dataMember.IsRequired != ImportGlobals.DefaultIsRequired)
                    dataMemberAttribute.Arguments.Add(new CodeAttributeArgument(ImportGlobals.IsRequiredProperty, new CodePrimitiveExpression(dataMember.IsRequired)));
                if (dataMember.EmitDefaultValue != ImportGlobals.DefaultEmitDefaultValue)
                    dataMemberAttribute.Arguments.Add(new CodeAttributeArgument(ImportGlobals.EmitDefaultValueProperty, new CodePrimitiveExpression(dataMember.EmitDefaultValue)));
                if (dataMember.Order != ImportGlobals.DefaultOrder)
                    dataMemberAttribute.Arguments.Add(new CodeAttributeArgument(ImportGlobals.OrderProperty, new CodePrimitiveExpression(dataMember.Order)));
                property.CustomAttributes.Add(dataMemberAttribute);

                if (GenerateSerializableTypes && !dataMember.IsRequired)
                {
                    CodeAttributeDeclaration optionalFieldAttribute = new CodeAttributeDeclaration(GetClrTypeFullName(typeof(OptionalFieldAttribute)));
                    field.CustomAttributes.Add(optionalFieldAttribute);
                }

                type.Members.Add(field);
                type.Members.Add(property);
            }
        }

        private bool CanDeclareAssemblyAttribute(ContractCodeDomInfo contractCodeDomInfo)
        {
            return SupportsAssemblyAttributes && !contractCodeDomInfo.UsesWildcardNamespace;
        }

        private static bool NeedsExplicitNamespace(string dataContractNamespace, string clrNamespace)
        {
            return (SchemaImportHelper.GetDefaultXmlNamespace(clrNamespace) != dataContractNamespace);
        }

        [RequiresUnreferencedCode(ImportGlobals.SerializerTrimmerWarning)]
        internal ICollection<CodeTypeReference>? GetKnownTypeReferences(DataContract dataContract)
        {
            DataContractDictionary? knownTypeDictionary = GetKnownTypeContracts(dataContract);
            if (knownTypeDictionary == null)
                return null;

            DataContractDictionary.ValueCollection? knownTypeContracts = knownTypeDictionary.Values;
            if (knownTypeContracts == null || knownTypeContracts.Count == 0)
                return null;

            List<CodeTypeReference> knownTypeReferences = new List<CodeTypeReference>();
            foreach (DataContract knownTypeContract in knownTypeContracts)
            {
                knownTypeReferences.Add(GetCodeTypeReference(knownTypeContract));
            }
            return knownTypeReferences;
        }

        [RequiresUnreferencedCode(ImportGlobals.SerializerTrimmerWarning)]
        private DataContractDictionary? GetKnownTypeContracts(DataContract dataContract)
        {
            if (_dataContractSet.KnownTypesForObject != null && IsObjectContract(dataContract))
            {
                return _dataContractSet.KnownTypesForObject;
            }
            else if (dataContract.Is(DataContractType.ClassDataContract))
            {
                ContractCodeDomInfo contractCodeDomInfo = GetContractCodeDomInfo(dataContract);
                if (!contractCodeDomInfo.IsProcessed)
                    GenerateType(dataContract, contractCodeDomInfo);
                if (contractCodeDomInfo.ReferencedTypeExists)
                    return GetKnownTypeContracts(dataContract, new Dictionary<DataContract, object?>());
            }
            return null;
        }

        [RequiresUnreferencedCode(ImportGlobals.SerializerTrimmerWarning)]
        private DataContractDictionary? GetKnownTypeContracts(DataContract classDataContract, Dictionary<DataContract, object?> handledContracts)
        {
            Debug.Assert(classDataContract.Is(DataContractType.ClassDataContract));

            if (handledContracts.ContainsKey(classDataContract))
                return classDataContract.KnownDataContracts;

            handledContracts.Add(classDataContract, null);
            bool objectMemberHandled = false;
            foreach (DataMember dataMember in classDataContract.DataMembers)
            {
                DataContract memberContract = dataMember.MemberTypeContract;
                if (!objectMemberHandled && _dataContractSet.KnownTypesForObject != null && IsObjectContract(memberContract))
                {
                    AddKnownTypeContracts(classDataContract, _dataContractSet.KnownTypesForObject);
                    objectMemberHandled = true;
                }
                else if (memberContract.Is(DataContractType.ClassDataContract))
                {
                    ContractCodeDomInfo memberCodeDomInfo = GetContractCodeDomInfo(memberContract);
                    if (!memberCodeDomInfo.IsProcessed)
                        GenerateType(memberContract, memberCodeDomInfo);
                    if (memberCodeDomInfo.ReferencedTypeExists)
                    {
                        AddKnownTypeContracts(classDataContract, GetKnownTypeContracts(memberContract, handledContracts));
                    }
                }
            }

            return classDataContract.KnownDataContracts;
        }

        [RequiresUnreferencedCode(ImportGlobals.SerializerTrimmerWarning)]
        private static void AddKnownTypeContracts(DataContract classDataContract, DataContractDictionary? knownContracts)
        {
            if (knownContracts == null || knownContracts.Count == 0)
                return;

            // This is a ClassDataContract. As such, it's KnownDataContracts collection is always non-null.
            Debug.Assert(classDataContract.Is(DataContractType.ClassDataContract));
            Debug.Assert(classDataContract.KnownDataContracts != null);

            foreach (KeyValuePair<XmlQualifiedName, DataContract> pair in knownContracts)
            {
                if (classDataContract.XmlName != pair.Key && !classDataContract.KnownDataContracts.ContainsKey(pair.Key) && !pair.Value.IsBuiltInDataContract)
                    classDataContract.KnownDataContracts.Add(pair.Key, pair.Value);
            }
        }

        [RequiresUnreferencedCode(ImportGlobals.SerializerTrimmerWarning)]
        private void AddKnownTypes(DataContract classDataContract, ContractCodeDomInfo contractCodeDomInfo)
        {
            Debug.Assert(classDataContract.Is(DataContractType.ClassDataContract));

            DataContractDictionary? knownContractDictionary = GetKnownTypeContracts(classDataContract, new Dictionary<DataContract, object?>());
            if (knownContractDictionary == null || knownContractDictionary.Count == 0)
                return;

            ICollection<DataContract> knownTypeContracts = knownContractDictionary.Values;
            foreach (DataContract knownTypeContract in knownTypeContracts)
            {
                // This is only called from methods that first call GenerateType to fill in this info.
                Debug.Assert(contractCodeDomInfo.TypeDeclaration != null);

                CodeAttributeDeclaration knownTypeAttribute = new CodeAttributeDeclaration(GetClrTypeFullName(typeof(KnownTypeAttribute)));
                knownTypeAttribute.Arguments.Add(new CodeAttributeArgument(new CodeTypeOfExpression(GetCodeTypeReference(knownTypeContract))));
                contractCodeDomInfo.TypeDeclaration.CustomAttributes.Add(knownTypeAttribute);
            }
            AddImportStatement(typeof(KnownTypeAttribute).Namespace, contractCodeDomInfo.CodeNamespace);
        }

        private CodeTypeReference WrapNullable(CodeTypeReference memberType)
        {
            if (!SupportsGenericTypeReference)
                return memberType;

            CodeTypeReference nullableOfMemberType = GetCodeTypeReference(typeof(Nullable<>));
            nullableOfMemberType.TypeArguments.Add(memberType);
            return nullableOfMemberType;
        }

        private void AddExtensionData(ContractCodeDomInfo contractCodeDomInfo)
        {
            if (contractCodeDomInfo.TypeDeclaration != null)
            {
                CodeTypeDeclaration type = contractCodeDomInfo.TypeDeclaration;
                type.BaseTypes.Add(GetClrTypeFullName(typeof(IExtensibleDataObject)));
                CodeMemberField extensionDataObjectField = ExtensionDataObjectField;
                if (GenerateSerializableTypes)
                {
                    CodeAttributeDeclaration nonSerializedAttribute = new CodeAttributeDeclaration(GetClrTypeFullName(typeof(NonSerializedAttribute)));
                    extensionDataObjectField.CustomAttributes.Add(nonSerializedAttribute);
                }
                type.Members.Add(extensionDataObjectField);
                contractCodeDomInfo.AddMemberName(extensionDataObjectField.Name);
                CodeMemberProperty extensionDataObjectProperty = ExtensionDataObjectProperty;
                type.Members.Add(extensionDataObjectProperty);
                contractCodeDomInfo.AddMemberName(extensionDataObjectProperty.Name);
            }
        }

        private void AddPropertyChangedNotifier(ContractCodeDomInfo contractCodeDomInfo, bool isValueType)
        {
            if (EnableDataBinding && SupportsDeclareEvents && contractCodeDomInfo.TypeDeclaration != null)
            {
                CodeTypeDeclaration codeTypeDeclaration = contractCodeDomInfo.TypeDeclaration;
                codeTypeDeclaration.BaseTypes.Add(CodeTypeIPropertyChange);
                CodeMemberEvent memberEvent = PropertyChangedEvent;
                codeTypeDeclaration.Members.Add(memberEvent);
                CodeMemberMethod raisePropertyChangedEventMethod = RaisePropertyChangedEventMethod;
                if (!isValueType)
                    raisePropertyChangedEventMethod.Attributes |= MemberAttributes.Family;
                codeTypeDeclaration.Members.Add(raisePropertyChangedEventMethod);
                contractCodeDomInfo.AddMemberName(memberEvent.Name);
                contractCodeDomInfo.AddMemberName(raisePropertyChangedEventMethod.Name);
            }
        }

        private static void ThrowIfReferencedBaseTypeSealed(Type baseType, DataContract dataContract)
        {
            if (baseType.IsSealed)
                throw ExceptionUtil.ThrowHelperError(new InvalidOperationException(SR.Format(SR.CannotDeriveFromSealedReferenceType, dataContract.XmlName.Name, dataContract.XmlName.Namespace, GetClrTypeFullName(baseType))));
        }

        [RequiresUnreferencedCode(ImportGlobals.SerializerTrimmerWarning)]
        private void ExportEnumDataContract(DataContract enumDataContract, ContractCodeDomInfo contractCodeDomInfo)
        {
            Debug.Assert(enumDataContract.Is(DataContractType.EnumDataContract));

            GenerateType(enumDataContract, contractCodeDomInfo);
            if (contractCodeDomInfo.ReferencedTypeExists)
                return;

            // This is supposed to be set by GenerateType. If it wasn't, there is a problem.
            Debug.Assert(contractCodeDomInfo.TypeDeclaration != null);

            CodeTypeDeclaration type = contractCodeDomInfo.TypeDeclaration;
            // BaseContract is never null for EnumDataContract
            Type baseType = enumDataContract.BaseContract!.UnderlyingType;
            type.IsEnum = true;
            type.BaseTypes.Add(baseType);
            if (baseType.IsDefined(typeof(FlagsAttribute), false))
            {
                type.CustomAttributes.Add(new CodeAttributeDeclaration(GetClrTypeFullName(typeof(FlagsAttribute))));
                AddImportStatement(typeof(FlagsAttribute).Namespace, contractCodeDomInfo.CodeNamespace);
            }

            string dataContractName = GetNameForAttribute(enumDataContract.XmlName.Name);
            CodeAttributeDeclaration dataContractAttribute = new CodeAttributeDeclaration(GetClrTypeFullName(typeof(DataContractAttribute)));
            dataContractAttribute.Arguments.Add(new CodeAttributeArgument(ImportGlobals.NameProperty, new CodePrimitiveExpression(dataContractName)));
            dataContractAttribute.Arguments.Add(new CodeAttributeArgument(ImportGlobals.NamespaceProperty, new CodePrimitiveExpression(enumDataContract.XmlName.Namespace)));
            type.CustomAttributes.Add(dataContractAttribute);
            AddImportStatement(typeof(DataContractAttribute).Namespace, contractCodeDomInfo.CodeNamespace);

            for (int i = 0; i < enumDataContract.DataMembers.Count; i++)
            {
                string stringValue = enumDataContract.DataMembers[i].Name;
                long longValue = enumDataContract.DataMembers[i].Order;   // Members[] and Values[] go hand in hand.

                CodeMemberField enumMember = new CodeMemberField();
                if (baseType == typeof(ulong))
                    enumMember.InitExpression = new CodeSnippetExpression(XmlConvert.ToString((ulong)longValue));
                else
                    enumMember.InitExpression = new CodePrimitiveExpression(longValue);
                enumMember.Name = GetMemberName(stringValue, contractCodeDomInfo);
                CodeAttributeDeclaration enumMemberAttribute = new CodeAttributeDeclaration(GetClrTypeFullName(typeof(EnumMemberAttribute)));
                if (enumMember.Name != stringValue)
                    enumMemberAttribute.Arguments.Add(new CodeAttributeArgument(ImportGlobals.ValueProperty, new CodePrimitiveExpression(stringValue)));
                enumMember.CustomAttributes.Add(enumMemberAttribute);
                type.Members.Add(enumMember);
            }
        }

        [RequiresUnreferencedCode(ImportGlobals.SerializerTrimmerWarning)]
        private void ExportISerializableDataContract(DataContract classDataContract, ContractCodeDomInfo contractCodeDomInfo)
        {
            Debug.Assert(classDataContract.Is(DataContractType.ClassDataContract));

            GenerateType(classDataContract, contractCodeDomInfo);
            if (contractCodeDomInfo.ReferencedTypeExists)
                return;

            if (SchemaImportHelper.GetDefaultXmlNamespace(contractCodeDomInfo.ClrNamespace) != classDataContract.XmlName.Namespace)
                throw ExceptionUtil.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.InvalidClrNamespaceGeneratedForISerializable, classDataContract.XmlName.Name, classDataContract.XmlName.Namespace, SchemaImportHelper.GetDataContractNamespaceFromUri(classDataContract.XmlName.Namespace), contractCodeDomInfo.ClrNamespace)));

            string dataContractName = GetNameForAttribute(classDataContract.XmlName.Name);
            int nestedTypeIndex = dataContractName.LastIndexOf('.');
            string expectedName = (nestedTypeIndex <= 0 || nestedTypeIndex == dataContractName.Length - 1) ? dataContractName : dataContractName.Substring(nestedTypeIndex + 1);

            // This is supposed to be set by GenerateType. If it wasn't, there is a problem.
            Debug.Assert(contractCodeDomInfo.TypeDeclaration != null);

            if (contractCodeDomInfo.TypeDeclaration.Name != expectedName)
                throw ExceptionUtil.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.InvalidClrNameGeneratedForISerializable, classDataContract.XmlName.Name, classDataContract.XmlName.Namespace, contractCodeDomInfo.TypeDeclaration.Name)));

            CodeTypeDeclaration type = contractCodeDomInfo.TypeDeclaration;
            if (SupportsPartialTypes)
                type.IsPartial = true;
            if (classDataContract.IsValueType && SupportsDeclareValueTypes)
                type.IsStruct = true;
            else
                type.IsClass = true;

            AddSerializableAttribute(true /*generateSerializable*/, type, contractCodeDomInfo);

            AddKnownTypes(classDataContract, contractCodeDomInfo);

            if (classDataContract.BaseContract == null)
            {
                if (!type.IsStruct)
                    type.BaseTypes.Add(typeof(object));
                type.BaseTypes.Add(GetClrTypeFullName(typeof(ISerializable)));
                type.Members.Add(ISerializableBaseConstructor);
                type.Members.Add(SerializationInfoField);
                type.Members.Add(SerializationInfoProperty);
                type.Members.Add(GetObjectDataMethod);
                AddPropertyChangedNotifier(contractCodeDomInfo, type.IsStruct);
            }
            else
            {
                ContractCodeDomInfo baseContractCodeDomInfo = GetContractCodeDomInfo(classDataContract.BaseContract);
                GenerateType(classDataContract.BaseContract, baseContractCodeDomInfo);
                type.BaseTypes.Add(baseContractCodeDomInfo.TypeReference!);
                if (baseContractCodeDomInfo.ReferencedTypeExists)
                {
                    Type? actualType = (Type?)baseContractCodeDomInfo.TypeReference?.UserData[s_codeUserDataActualTypeKey];
                    Debug.Assert(actualType != null);   // If we're in this if condition, then we should be able to get a Type
                    ThrowIfReferencedBaseTypeSealed(actualType, classDataContract);
                }
                type.Members.Add(ISerializableDerivedConstructor);
            }
        }

        [RequiresUnreferencedCode(ImportGlobals.SerializerTrimmerWarning)]
        private void GenerateKeyValueType(DataContract? keyValueContract)
        {
            Debug.Assert(keyValueContract == null || keyValueContract.Is(DataContractType.ClassDataContract));

            // Add code for KeyValue item type in the case where its usage is limited to dictionary
            // and dictionary is not found in referenced types
            if (keyValueContract != null && _dataContractSet.GetDataContract(keyValueContract.XmlName) == null)
            {
                ContractCodeDomInfo? contractCodeDomInfo = null;
                if (_dataContractSet.ProcessedContracts.TryGetValue(keyValueContract, out object? info))
                    contractCodeDomInfo = info as ContractCodeDomInfo;
                if (contractCodeDomInfo == null)
                {
                    contractCodeDomInfo = new ContractCodeDomInfo();
                    _dataContractSet.ProcessedContracts.Add(keyValueContract, contractCodeDomInfo);
                    ExportClassDataContract(keyValueContract, contractCodeDomInfo);
                    contractCodeDomInfo.IsProcessed = true;
                }
            }
        }

        [RequiresUnreferencedCode(ImportGlobals.SerializerTrimmerWarning)]
        private void ExportCollectionDataContract(DataContract collectionContract, ContractCodeDomInfo contractCodeDomInfo)
        {
            Debug.Assert(collectionContract.Is(DataContractType.CollectionDataContract));

            GenerateType(collectionContract, contractCodeDomInfo);
            if (contractCodeDomInfo.ReferencedTypeExists)
                return;

            string dataContractName = GetNameForAttribute(collectionContract.XmlName.Name);

            // If type name is not expected, generate collection type that derives from referenced list type and uses [CollectionDataContract]
            if (!SupportsGenericTypeReference)
                throw ExceptionUtil.ThrowHelperError(new InvalidOperationException(
                    SR.Format(SR.CannotUseGenericTypeAsBase, dataContractName,
                    collectionContract.XmlName.Namespace)));

            // ItemContract - aka BaseContract - is never null for CollectionDataContract
            DataContract itemContract = collectionContract.BaseContract!;
            bool isItemTypeNullable = collectionContract.IsItemTypeNullable();
            bool isDictionary = collectionContract.IsDictionaryLike(out string? keyName, out string? valueName, out string? itemName);

            CodeTypeReference? baseTypeReference;
            bool foundDictionaryBase = TryGetReferencedDictionaryType(collectionContract, out baseTypeReference);
            if (!foundDictionaryBase)
            {
                if (isDictionary)
                {
                    GenerateKeyValueType(itemContract.As(DataContractType.ClassDataContract));
                }
                if (!TryGetReferencedListType(itemContract, isItemTypeNullable, out baseTypeReference))
                {
                    if (SupportsGenericTypeReference)
                    {
                        baseTypeReference = GetCodeTypeReference(typeof(List<>));
                        baseTypeReference.TypeArguments.Add(GetElementTypeReference(itemContract, isItemTypeNullable));
                    }
                    else
                    {
                        string expectedTypeName = ImportGlobals.ArrayPrefix + itemContract.XmlName.Name;
                        string expectedTypeNs = SchemaImportHelper.GetCollectionNamespace(itemContract.XmlName.Namespace);
                        throw ExceptionUtil.ThrowHelperError(new InvalidOperationException(SR.Format(SR.ReferencedBaseTypeDoesNotExist,
                            dataContractName, collectionContract.XmlName.Namespace,
                            expectedTypeName, expectedTypeNs, GetClrTypeFullName(typeof(IList<>)), GetClrTypeFullName(typeof(ICollection<>)))));
                    }
                }
            }

            // This is supposed to be set by GenerateType. If it wasn't, there is a problem.
            Debug.Assert(contractCodeDomInfo.TypeDeclaration != null);

            CodeTypeDeclaration generatedType = contractCodeDomInfo.TypeDeclaration;
            generatedType.BaseTypes.Add(baseTypeReference!);
            CodeAttributeDeclaration collectionContractAttribute = new CodeAttributeDeclaration(GetClrTypeFullName(typeof(CollectionDataContractAttribute)));
            collectionContractAttribute.Arguments.Add(new CodeAttributeArgument(ImportGlobals.NameProperty, new CodePrimitiveExpression(dataContractName)));
            collectionContractAttribute.Arguments.Add(new CodeAttributeArgument(ImportGlobals.NamespaceProperty, new CodePrimitiveExpression(collectionContract.XmlName.Namespace)));
            if (collectionContract.IsReference != ImportGlobals.DefaultIsReference)
                collectionContractAttribute.Arguments.Add(new CodeAttributeArgument(ImportGlobals.IsReferenceProperty, new CodePrimitiveExpression(collectionContract.IsReference)));
            collectionContractAttribute.Arguments.Add(new CodeAttributeArgument(ImportGlobals.ItemNameProperty, new CodePrimitiveExpression(GetNameForAttribute(itemName!))));    // ItemName is never null for Collection contracts.
            if (foundDictionaryBase)
            {
                // These are not null if we are working with a dictionary. See CollectionDataContract.IsDictionary
                Debug.Assert(keyName != null);
                Debug.Assert(valueName != null);
                collectionContractAttribute.Arguments.Add(new CodeAttributeArgument(ImportGlobals.KeyNameProperty, new CodePrimitiveExpression(GetNameForAttribute(keyName))));
                collectionContractAttribute.Arguments.Add(new CodeAttributeArgument(ImportGlobals.ValueNameProperty, new CodePrimitiveExpression(GetNameForAttribute(valueName))));
            }
            generatedType.CustomAttributes.Add(collectionContractAttribute);
            AddImportStatement(typeof(CollectionDataContractAttribute).Namespace, contractCodeDomInfo.CodeNamespace);
            AddSerializableAttribute(GenerateSerializableTypes, generatedType, contractCodeDomInfo);
        }

        [RequiresUnreferencedCode(ImportGlobals.SerializerTrimmerWarning)]
        private void ExportXmlDataContract(XmlDataContract xmlDataContract, ContractCodeDomInfo contractCodeDomInfo)
        {
            GenerateType(xmlDataContract, contractCodeDomInfo);
            if (contractCodeDomInfo.ReferencedTypeExists)
                return;

            // This is supposed to be set by GenerateType. If it wasn't, there is a problem.
            Debug.Assert(contractCodeDomInfo.TypeDeclaration != null);

            CodeTypeDeclaration type = contractCodeDomInfo.TypeDeclaration;
            if (SupportsPartialTypes)
                type.IsPartial = true;
            if (xmlDataContract.IsValueType)
                type.IsStruct = true;
            else
            {
                type.IsClass = true;
                type.BaseTypes.Add(typeof(object));
            }
            AddSerializableAttribute(GenerateSerializableTypes, type, contractCodeDomInfo);

            type.BaseTypes.Add(GetClrTypeFullName(typeof(IXmlSerializable)));

            type.Members.Add(NodeArrayField);
            type.Members.Add(NodeArrayProperty);
            type.Members.Add(ReadXmlMethod);
            type.Members.Add(WriteXmlMethod);
            type.Members.Add(GetSchemaMethod);
            if (xmlDataContract.IsAnonymous && !xmlDataContract.HasRoot)
            {
                type.CustomAttributes.Add(new CodeAttributeDeclaration(
                    GetClrTypeFullName(ImportGlobals.TypeOfXmlSchemaProviderAttribute),
                    new CodeAttributeArgument(NullReference),
                    new CodeAttributeArgument(ImportGlobals.IsAnyProperty, new CodePrimitiveExpression(true)))
                );
            }
            else
            {
                type.CustomAttributes.Add(new CodeAttributeDeclaration(
                    GetClrTypeFullName(ImportGlobals.TypeOfXmlSchemaProviderAttribute),
                    new CodeAttributeArgument(new CodePrimitiveExpression(ImportGlobals.ExportSchemaMethod)))
                );

                CodeMemberField typeNameField = new CodeMemberField(ImportGlobals.TypeOfXmlQualifiedName, TypeNameFieldName);
                typeNameField.Attributes |= MemberAttributes.Static | MemberAttributes.Private;
                XmlQualifiedName typeName = xmlDataContract.IsAnonymous
                    ? XsdDataContractImporter.ImportActualType(xmlDataContract.XsdType?.Annotation, xmlDataContract.XmlName, xmlDataContract.XmlName)
                    : xmlDataContract.XmlName;
                typeNameField.InitExpression = new CodeObjectCreateExpression(ImportGlobals.TypeOfXmlQualifiedName, new CodePrimitiveExpression(typeName.Name), new CodePrimitiveExpression(typeName.Namespace));
                type.Members.Add(typeNameField);

                type.Members.Add(GetSchemaStaticMethod);

                bool isElementNameDifferent =
                    (xmlDataContract.TopLevelElementName != null && xmlDataContract.TopLevelElementName.Value != xmlDataContract.XmlName.Name) ||
                    (xmlDataContract.TopLevelElementNamespace != null && xmlDataContract.TopLevelElementNamespace.Value != xmlDataContract.XmlName.Namespace);
                if (isElementNameDifferent || xmlDataContract.IsTopLevelElementNullable == false)
                {
                    CodeAttributeDeclaration xmlRootAttribute = new CodeAttributeDeclaration(GetClrTypeFullName(typeof(XmlRootAttribute)));
                    if (isElementNameDifferent)
                    {
                        if (xmlDataContract.TopLevelElementName != null)
                        {
                            xmlRootAttribute.Arguments.Add(new CodeAttributeArgument("ElementName", new CodePrimitiveExpression(xmlDataContract.TopLevelElementName.Value)));
                        }
                        if (xmlDataContract.TopLevelElementNamespace != null)
                        {
                            xmlRootAttribute.Arguments.Add(new CodeAttributeArgument("Namespace", new CodePrimitiveExpression(xmlDataContract.TopLevelElementNamespace.Value)));
                        }
                    }
                    if (xmlDataContract.IsTopLevelElementNullable == false)
                        xmlRootAttribute.Arguments.Add(new CodeAttributeArgument("IsNullable", new CodePrimitiveExpression(false)));
                    type.CustomAttributes.Add(xmlRootAttribute);
                }
            }
            AddPropertyChangedNotifier(contractCodeDomInfo, type.IsStruct);
        }

        private CodeNamespace GetCodeNamespace(string clrNamespace, string dataContractNamespace, ContractCodeDomInfo contractCodeDomInfo)
        {
            if (contractCodeDomInfo.CodeNamespace != null)
                return contractCodeDomInfo.CodeNamespace;

            CodeNamespaceCollection codeNamespaceCollection = _codeCompileUnit.Namespaces;
            foreach (CodeNamespace ns in codeNamespaceCollection)
            {
                if (ns.Name == clrNamespace)
                {
                    contractCodeDomInfo.CodeNamespace = ns;
                    return ns;
                }
            }

            CodeNamespace codeNamespace = new CodeNamespace(clrNamespace);
            codeNamespaceCollection.Add(codeNamespace);

            if (CanDeclareAssemblyAttribute(contractCodeDomInfo)
                && NeedsExplicitNamespace(dataContractNamespace, clrNamespace))
            {
                CodeAttributeDeclaration namespaceAttribute = new CodeAttributeDeclaration(GetClrTypeFullName(typeof(ContractNamespaceAttribute)));
                namespaceAttribute.Arguments.Add(new CodeAttributeArgument(new CodePrimitiveExpression(dataContractNamespace)));
                namespaceAttribute.Arguments.Add(new CodeAttributeArgument(ImportGlobals.ClrNamespaceProperty, new CodePrimitiveExpression(clrNamespace)));
                _codeCompileUnit.AssemblyCustomAttributes.Add(namespaceAttribute);
            }
            contractCodeDomInfo.CodeNamespace = codeNamespace;
            return codeNamespace;
        }

        private static string GetMemberName(string memberName, ContractCodeDomInfo contractCodeDomInfo)
        {
            memberName = GetClrIdentifier(memberName, ImportGlobals.DefaultGeneratedMember);

            // This is only called from Export* methods which have already called GenerateType to fill in this info.
            Debug.Assert(contractCodeDomInfo.TypeDeclaration != null);

            if (memberName == contractCodeDomInfo.TypeDeclaration.Name)
                memberName = AppendToValidClrIdentifier(memberName, ImportGlobals.DefaultMemberSuffix);

            if (contractCodeDomInfo.GetMemberNames().Contains(memberName))
            {
                string uniqueMemberName;
                for (int i = 1; ; i++)
                {
                    uniqueMemberName = AppendToValidClrIdentifier(memberName, i.ToString(NumberFormatInfo.InvariantInfo));
                    if (!contractCodeDomInfo.GetMemberNames().Contains(uniqueMemberName))
                    {
                        memberName = uniqueMemberName;
                        break;
                    }
                }
            }

            contractCodeDomInfo.AddMemberName(memberName);
            return memberName;
        }

        private static void AddBaseMemberNames(ContractCodeDomInfo baseContractCodeDomInfo, ContractCodeDomInfo contractCodeDomInfo)
        {
            if (!baseContractCodeDomInfo.ReferencedTypeExists)
            {
                HashSet<string> baseMemberNames = baseContractCodeDomInfo.GetMemberNames();
                HashSet<string> memberNames = contractCodeDomInfo.GetMemberNames();
                foreach (string name in baseMemberNames)
                {
                    memberNames.Add(name);
                }
            }
        }

        private static string GetClrIdentifier(string identifier, string defaultIdentifier)
        {
            if (identifier.Length <= MaxIdentifierLength && CodeGenerator.IsValidLanguageIndependentIdentifier(identifier))
                return identifier;

            bool isStart = true;
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < identifier.Length && builder.Length < MaxIdentifierLength; i++)
            {
                char c = identifier[i];
                if (IsValid(c))
                {
                    if (isStart && !IsValidStart(c))
                        builder.Append('_');
                    builder.Append(c);
                    isStart = false;
                }
            }
            if (builder.Length == 0)
                return defaultIdentifier;

            return builder.ToString();
        }

        internal static string GetClrTypeFullName(Type type)
        {
            return !type.IsGenericTypeDefinition && type.ContainsGenericParameters ? type.Namespace + "." + type.Name : type.FullName!;
        }

        private static string AppendToValidClrIdentifier(string identifier, string appendString)
        {
            int availableLength = MaxIdentifierLength - identifier.Length;
            int requiredLength = appendString.Length;
            if (availableLength < requiredLength)
                identifier = identifier.Substring(0, MaxIdentifierLength - requiredLength);
            identifier += appendString;
            return identifier;
        }

        private string GetClrNamespace(DataContract dataContract, ContractCodeDomInfo contractCodeDomInfo)
        {
            string? clrNamespace = contractCodeDomInfo.ClrNamespace;
            bool usesWildcardNamespace = false;
            if (clrNamespace == null)
            {
                if (!Namespaces.TryGetValue(dataContract.XmlName.Namespace, out clrNamespace))
                {
                    if (Namespaces.TryGetValue(WildcardNamespaceMapping, out clrNamespace))
                    {
                        usesWildcardNamespace = true;
                    }
                    else
                    {
                        clrNamespace = GetClrNamespace(dataContract.XmlName.Namespace);
                        if (ClrNamespaces.ContainsKey(clrNamespace))
                        {
                            string uniqueNamespace;
                            for (int i = 1; ; i++)
                            {
                                uniqueNamespace = ((clrNamespace.Length == 0) ? ImportGlobals.DefaultClrNamespace : clrNamespace) + i.ToString(NumberFormatInfo.InvariantInfo);
                                if (!ClrNamespaces.ContainsKey(uniqueNamespace))
                                {
                                    clrNamespace = uniqueNamespace;
                                    break;
                                }
                            }
                        }
                        AddNamespacePair(dataContract.XmlName.Namespace, clrNamespace);
                    }
                }
                contractCodeDomInfo.ClrNamespace = clrNamespace;
                contractCodeDomInfo.UsesWildcardNamespace = usesWildcardNamespace;
            }
            return clrNamespace;
        }

        private void AddNamespacePair(string dataContractNamespace, string clrNamespace)
        {
            Namespaces.Add(dataContractNamespace, clrNamespace);
            ClrNamespaces.Add(clrNamespace, dataContractNamespace);
        }

        private static void AddImportStatement(string? clrNamespace, CodeNamespace? codeNamespace)
        {
            // We don't expect these to be null when passed in, but they are usually properties on larger classes which declare their types as nullable and we can't control, so we allow nullable parameters.
            Debug.Assert(clrNamespace != null);
            Debug.Assert(codeNamespace != null);

            if (clrNamespace == codeNamespace.Name)
                return;

            CodeNamespaceImportCollection importCollection = codeNamespace.Imports;
            foreach (CodeNamespaceImport import in importCollection)
            {
                if (import.Namespace == clrNamespace)
                    return;
            }

            importCollection.Add(new CodeNamespaceImport(clrNamespace));
        }

        private static string GetClrNamespace(string? dataContractNamespace)
        {
            if (string.IsNullOrEmpty(dataContractNamespace))
                return string.Empty;

            StringBuilder builder = new StringBuilder();
            if (Uri.TryCreate(dataContractNamespace, UriKind.RelativeOrAbsolute, out Uri? uri))
            {
                Dictionary<string, object?> fragments = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                if (!uri.IsAbsoluteUri)
                    AddToNamespace(builder, uri.OriginalString, fragments);
                else
                {
                    string uriString = uri.AbsoluteUri;
                    if (uriString.StartsWith(ImportGlobals.DataContractXsdBaseNamespace, StringComparison.Ordinal))
                        AddToNamespace(builder, uriString.Substring(ImportGlobals.DataContractXsdBaseNamespace.Length), fragments);
                    else
                    {
                        string host = uri.Host;
                        if (host != null)
                            AddToNamespace(builder, host, fragments);
                        string path = uri.PathAndQuery;
                        if (path != null)
                            AddToNamespace(builder, path, fragments);
                    }
                }
            }

            if (builder.Length == 0)
                return string.Empty;

            int length = builder.Length;
            if (builder[builder.Length - 1] == '.')
                length--;
            length = Math.Min(MaxIdentifierLength, length);

            return builder.ToString(0, length);
        }

        private static void AddToNamespace(StringBuilder builder, string? fragment, Dictionary<string, object?> fragments)
        {
            if (fragment == null)
                return;
            bool isStart = true;
            int fragmentOffset = builder.Length;
            int fragmentLength = 0;

            for (int i = 0; i < fragment.Length && builder.Length < MaxIdentifierLength; i++)
            {
                char c = fragment[i];

                if (IsValid(c))
                {
                    if (isStart && !IsValidStart(c))
                        builder.Append('_');
                    builder.Append(c);
                    fragmentLength++;
                    isStart = false;
                }
                else if ((c == '.' || c == '/' || c == ':') && (builder.Length == 1
                    || (builder.Length > 1 && builder[builder.Length - 1] != '.')))
                {
                    AddNamespaceFragment(builder, fragmentOffset, fragmentLength, fragments);
                    builder.Append('.');
                    fragmentOffset = builder.Length;
                    fragmentLength = 0;
                    isStart = true;
                }
            }
            AddNamespaceFragment(builder, fragmentOffset, fragmentLength, fragments);
        }

        private static void AddNamespaceFragment(StringBuilder builder, int fragmentOffset,
            int fragmentLength, Dictionary<string, object?> fragments)
        {
            if (fragmentLength == 0)
                return;

            string nsFragment = builder.ToString(fragmentOffset, fragmentLength);
            if (fragments.ContainsKey(nsFragment))
            {
                for (int i = 1; ; i++)
                {
                    string uniquifier = i.ToString(NumberFormatInfo.InvariantInfo);
                    string uniqueNsFragment = AppendToValidClrIdentifier(nsFragment, uniquifier);
                    if (!fragments.ContainsKey(uniqueNsFragment))
                    {
                        builder.Append(uniquifier);
                        nsFragment = uniqueNsFragment;
                        break;
                    }
                    if (i == int.MaxValue)
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.CannotComputeUniqueName, nsFragment)));
                }
            }
            fragments.Add(nsFragment, null);
        }

        [RequiresUnreferencedCode(ImportGlobals.SerializerTrimmerWarning)]
        internal static bool IsObjectContract(DataContract? dataContract)
        {
            Dictionary<Type, object> previousCollectionTypes = new Dictionary<Type, object>();
            while (dataContract != null && dataContract.Is(DataContractType.CollectionDataContract))
            {
                if (dataContract.OriginalUnderlyingType == null)
                {
                    dataContract = dataContract.BaseContract;
                    continue;
                }

                if (!previousCollectionTypes.ContainsKey(dataContract.OriginalUnderlyingType))
                {
                    previousCollectionTypes.Add(dataContract.OriginalUnderlyingType, dataContract.OriginalUnderlyingType);
                    dataContract = dataContract.BaseContract;
                }
                else
                {
                    break;
                }
            }

            return dataContract != null && dataContract.Is(DataContractType.PrimitiveDataContract) && dataContract.UnderlyingType == typeof(object);
        }

        private static bool IsValidStart(char c)
        {
            return (char.GetUnicodeCategory(c) != UnicodeCategory.DecimalDigitNumber);
        }

        private static bool IsValid(char c)
        {
            UnicodeCategory uc = char.GetUnicodeCategory(c);

            // each char must be Lu, Ll, Lt, Lm, Lo, Nd, Mn, Mc, Pc

            switch (uc)
            {
                case UnicodeCategory.UppercaseLetter:        // Lu
                case UnicodeCategory.LowercaseLetter:        // Ll
                case UnicodeCategory.TitlecaseLetter:        // Lt
                case UnicodeCategory.ModifierLetter:         // Lm
                case UnicodeCategory.OtherLetter:            // Lo
                case UnicodeCategory.DecimalDigitNumber:     // Nd
                case UnicodeCategory.NonSpacingMark:         // Mn
                case UnicodeCategory.SpacingCombiningMark:   // Mc
                case UnicodeCategory.ConnectorPunctuation:   // Pc
                    return true;
                default:
                    return false;
            }
        }

        private CodeTypeReference CodeTypeIPropertyChange
        {
            get { return GetCodeTypeReference(typeof(System.ComponentModel.INotifyPropertyChanged)); }
        }

        private static CodeThisReferenceExpression ThisReference
        {
            get { return new CodeThisReferenceExpression(); }
        }

        private static CodePrimitiveExpression NullReference
        {
            get { return new CodePrimitiveExpression(null!); }
        }

        private CodeParameterDeclarationExpression SerializationInfoParameter
        {
            get { return new CodeParameterDeclarationExpression(GetCodeTypeReference(typeof(SerializationInfo)), ImportGlobals.SerializationInfoFieldName); }
        }

        private CodeParameterDeclarationExpression StreamingContextParameter
        {
            get { return new CodeParameterDeclarationExpression(GetCodeTypeReference(typeof(StreamingContext)), ImportGlobals.ContextFieldName); }
        }

        private CodeAttributeDeclaration SerializableAttribute
        {
            get { return new CodeAttributeDeclaration(GetCodeTypeReference(typeof(SerializableAttribute))); }
        }

        private CodeMemberProperty NodeArrayProperty
        {
            get
            {
                return CreateProperty(GetCodeTypeReference(ImportGlobals.TypeOfXmlNodeArray), ImportGlobals.NodeArrayPropertyName, ImportGlobals.NodeArrayFieldName, false/*isValueType*/);
            }
        }

        private CodeMemberField NodeArrayField
        {
            get
            {
                CodeMemberField nodeArrayField = new CodeMemberField();
                nodeArrayField.Type = GetCodeTypeReference(ImportGlobals.TypeOfXmlNodeArray);
                nodeArrayField.Name = ImportGlobals.NodeArrayFieldName;
                nodeArrayField.Attributes = MemberAttributes.Private;
                return nodeArrayField;
            }
        }

        private CodeMemberMethod ReadXmlMethod
        {
            get
            {
                CodeMemberMethod readXmlMethod = new CodeMemberMethod();
                readXmlMethod.Name = "ReadXml";
                CodeParameterDeclarationExpression readerArg = new CodeParameterDeclarationExpression(typeof(XmlReader), "reader");
                readXmlMethod.Parameters.Add(readerArg);
                readXmlMethod.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                readXmlMethod.ImplementationTypes.Add(typeof(IXmlSerializable));
                CodeAssignStatement setNode = new CodeAssignStatement();
                setNode.Left = new CodeFieldReferenceExpression(ThisReference, ImportGlobals.NodeArrayFieldName);
                setNode.Right = new CodeMethodInvokeExpression(
                                      new CodeTypeReferenceExpression(GetCodeTypeReference(typeof(XmlSerializableServices))),
                                      nameof(XmlSerializableServices.ReadNodes),
                                      new CodeArgumentReferenceExpression(readerArg.Name)
                                    );
                readXmlMethod.Statements.Add(setNode);
                return readXmlMethod;
            }
        }

        private CodeMemberMethod WriteXmlMethod
        {
            get
            {
                CodeMemberMethod writeXmlMethod = new CodeMemberMethod();
                writeXmlMethod.Name = "WriteXml";
                CodeParameterDeclarationExpression writerArg = new CodeParameterDeclarationExpression(typeof(XmlWriter), "writer");
                writeXmlMethod.Parameters.Add(writerArg);
                writeXmlMethod.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                writeXmlMethod.ImplementationTypes.Add(typeof(IXmlSerializable));
                writeXmlMethod.Statements.Add(
                    new CodeMethodInvokeExpression(
                        new CodeTypeReferenceExpression(GetCodeTypeReference(typeof(XmlSerializableServices))),
                        nameof(XmlSerializableServices.WriteNodes),
                        new CodeArgumentReferenceExpression(writerArg.Name),
                        new CodePropertyReferenceExpression(ThisReference, ImportGlobals.NodeArrayPropertyName)
                    )
                );
                return writeXmlMethod;
            }
        }

        private CodeMemberMethod GetSchemaMethod
        {
            get
            {
                CodeMemberMethod getSchemaMethod = new CodeMemberMethod();
                getSchemaMethod.Name = "GetSchema";
                getSchemaMethod.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                getSchemaMethod.ImplementationTypes.Add(typeof(IXmlSerializable));
                getSchemaMethod.ReturnType = GetCodeTypeReference(typeof(XmlSchema));
                getSchemaMethod.Statements.Add(new CodeMethodReturnStatement(NullReference));
                return getSchemaMethod;
            }
        }

        private CodeMemberMethod GetSchemaStaticMethod
        {
            get
            {
                CodeMemberMethod getSchemaStaticMethod = new CodeMemberMethod();
                getSchemaStaticMethod.Name = ImportGlobals.ExportSchemaMethod;
                getSchemaStaticMethod.ReturnType = GetCodeTypeReference(ImportGlobals.TypeOfXmlQualifiedName);
                CodeParameterDeclarationExpression paramDeclaration = new CodeParameterDeclarationExpression(typeof(XmlSchemaSet), "schemas");
                getSchemaStaticMethod.Parameters.Add(paramDeclaration);
                getSchemaStaticMethod.Attributes = MemberAttributes.Static | MemberAttributes.Public;
                getSchemaStaticMethod.Statements.Add(
                    new CodeMethodInvokeExpression(
                        new CodeTypeReferenceExpression(GetCodeTypeReference(typeof(XmlSerializableServices))),
                        nameof(XmlSerializableServices.AddDefaultSchema),
                        new CodeArgumentReferenceExpression(paramDeclaration.Name),
                        new CodeFieldReferenceExpression(null!, TypeNameFieldName)
                    )
                );
                getSchemaStaticMethod.Statements.Add(
                    new CodeMethodReturnStatement(
                        new CodeFieldReferenceExpression(null!, TypeNameFieldName)
                    )
                );
                return getSchemaStaticMethod;
            }
        }

        private CodeConstructor ISerializableBaseConstructor
        {
            get
            {
                CodeConstructor baseConstructor = new CodeConstructor();
                baseConstructor.Attributes = MemberAttributes.Public;
                baseConstructor.Parameters.Add(SerializationInfoParameter);
                baseConstructor.Parameters.Add(StreamingContextParameter);
                CodeAssignStatement setObjectData = new CodeAssignStatement();
                setObjectData.Left = new CodePropertyReferenceExpression(ThisReference, ImportGlobals.SerializationInfoFieldName);
                setObjectData.Right = new CodeArgumentReferenceExpression(ImportGlobals.SerializationInfoFieldName);
                baseConstructor.Statements.Add(setObjectData);
                // Special-cased check for vb here since CodeGeneratorOptions does not provide information indicating that VB cannot initialize event member
                if (EnableDataBinding && SupportsDeclareEvents && FileExtension != "vb")
                {
                    baseConstructor.Statements.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(ThisReference, PropertyChangedEvent.Name), NullReference));
                }
                return baseConstructor;
            }
        }

        private CodeConstructor ISerializableDerivedConstructor
        {
            get
            {
                CodeConstructor derivedConstructor = new CodeConstructor();
                derivedConstructor.Attributes = MemberAttributes.Public;
                derivedConstructor.Parameters.Add(SerializationInfoParameter);
                derivedConstructor.Parameters.Add(StreamingContextParameter);
                derivedConstructor.BaseConstructorArgs.Add(new CodeVariableReferenceExpression(ImportGlobals.SerializationInfoFieldName));
                derivedConstructor.BaseConstructorArgs.Add(new CodeVariableReferenceExpression(ImportGlobals.ContextFieldName));
                return derivedConstructor;
            }
        }

        private CodeMemberField SerializationInfoField
        {
            get
            {
                CodeMemberField serializationInfoField = new CodeMemberField();
                serializationInfoField.Type = GetCodeTypeReference(typeof(SerializationInfo));
                serializationInfoField.Name = ImportGlobals.SerializationInfoFieldName;
                serializationInfoField.Attributes = MemberAttributes.Private;
                return serializationInfoField;
            }
        }

        private CodeMemberProperty SerializationInfoProperty
        {
            get
            {
                return CreateProperty(GetCodeTypeReference(typeof(SerializationInfo)), ImportGlobals.SerializationInfoPropertyName, ImportGlobals.SerializationInfoFieldName, false/*isValueType*/);
            }
        }

        private CodeMemberMethod GetObjectDataMethod
        {
            get
            {
                CodeMemberMethod getObjectDataMethod = new CodeMemberMethod();
                getObjectDataMethod.Name = ImportGlobals.GetObjectDataMethodName;
                getObjectDataMethod.Parameters.Add(SerializationInfoParameter);
                getObjectDataMethod.Parameters.Add(StreamingContextParameter);
                getObjectDataMethod.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                getObjectDataMethod.ImplementationTypes.Add(typeof(ISerializable));

                // Generates: if (this.SerializationInfo == null) return;
                CodeConditionStatement returnIfNull = new CodeConditionStatement();
                returnIfNull.Condition = new CodeBinaryOperatorExpression(
                    new CodePropertyReferenceExpression(ThisReference, ImportGlobals.SerializationInfoPropertyName),
                    CodeBinaryOperatorType.IdentityEquality,
                    NullReference);
                returnIfNull.TrueStatements.Add(new CodeMethodReturnStatement());

                // Generates: SerializationInfoEnumerator enumerator = this.SerializationInfo.GetEnumerator();
                CodeVariableDeclarationStatement getEnumerator = new CodeVariableDeclarationStatement();
                getEnumerator.Type = GetCodeTypeReference(typeof(SerializationInfoEnumerator));
                getEnumerator.Name = ImportGlobals.EnumeratorFieldName;
                getEnumerator.InitExpression = new CodeMethodInvokeExpression(
                    new CodePropertyReferenceExpression(ThisReference, ImportGlobals.SerializationInfoPropertyName),
                    ImportGlobals.GetEnumeratorMethodName);

                //Generates: SerializationEntry entry = enumerator.Current;
                CodeVariableDeclarationStatement getCurrent = new CodeVariableDeclarationStatement();
                getCurrent.Type = GetCodeTypeReference(typeof(SerializationEntry));
                getCurrent.Name = ImportGlobals.SerializationEntryFieldName;
                getCurrent.InitExpression = new CodePropertyReferenceExpression(
                    new CodeVariableReferenceExpression(ImportGlobals.EnumeratorFieldName),
                    ImportGlobals.CurrentPropertyName);

                //Generates: info.AddValue(entry.Name, entry.Value);
                CodeExpressionStatement addValue = new CodeExpressionStatement();
                CodePropertyReferenceExpression getCurrentName = new CodePropertyReferenceExpression(
                    new CodeVariableReferenceExpression(ImportGlobals.SerializationEntryFieldName),
                    ImportGlobals.NameProperty);
                CodePropertyReferenceExpression getCurrentValue = new CodePropertyReferenceExpression(
                    new CodeVariableReferenceExpression(ImportGlobals.SerializationEntryFieldName),
                    ImportGlobals.ValueProperty);
                addValue.Expression = new CodeMethodInvokeExpression(
                    new CodeArgumentReferenceExpression(ImportGlobals.SerializationInfoFieldName),
                    ImportGlobals.AddValueMethodName,
                    new CodeExpression[] { getCurrentName, getCurrentValue });

                //Generates: for (; enumerator.MoveNext(); )
                CodeIterationStatement loop = new CodeIterationStatement();
                loop.TestExpression = new CodeMethodInvokeExpression(
                    new CodeVariableReferenceExpression(ImportGlobals.EnumeratorFieldName),
                    ImportGlobals.MoveNextMethodName);
                loop.InitStatement = loop.IncrementStatement = new CodeSnippetStatement(string.Empty);
                loop.Statements.Add(getCurrent);
                loop.Statements.Add(addValue);

                getObjectDataMethod.Statements.Add(returnIfNull);
                getObjectDataMethod.Statements.Add(getEnumerator);
                getObjectDataMethod.Statements.Add(loop);

                return getObjectDataMethod;
            }
        }

        private CodeMemberField ExtensionDataObjectField
        {
            get
            {
                CodeMemberField extensionDataObjectField = new CodeMemberField();
                extensionDataObjectField.Type = GetCodeTypeReference(typeof(ExtensionDataObject));
                extensionDataObjectField.Name = ImportGlobals.ExtensionDataObjectFieldName;
                extensionDataObjectField.Attributes = MemberAttributes.Private;
                return extensionDataObjectField;
            }
        }

        private CodeMemberProperty ExtensionDataObjectProperty
        {
            get
            {
                CodeMemberProperty extensionDataObjectProperty = new CodeMemberProperty();
                extensionDataObjectProperty.Type = GetCodeTypeReference(typeof(ExtensionDataObject));
                extensionDataObjectProperty.Name = ImportGlobals.ExtensionDataObjectPropertyName;
                extensionDataObjectProperty.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                extensionDataObjectProperty.ImplementationTypes.Add(typeof(IExtensibleDataObject));

                CodeMethodReturnStatement propertyGet = new CodeMethodReturnStatement();
                propertyGet.Expression = new CodeFieldReferenceExpression(ThisReference, ImportGlobals.ExtensionDataObjectFieldName);
                extensionDataObjectProperty.GetStatements.Add(propertyGet);

                CodeAssignStatement propertySet = new CodeAssignStatement();
                propertySet.Left = new CodeFieldReferenceExpression(ThisReference, ImportGlobals.ExtensionDataObjectFieldName);
                propertySet.Right = new CodePropertySetValueReferenceExpression();
                extensionDataObjectProperty.SetStatements.Add(propertySet);

                return extensionDataObjectProperty;
            }
        }

        private CodeMemberMethod RaisePropertyChangedEventMethod
        {
            get
            {
                CodeMemberMethod raisePropertyChangedEventMethod = new CodeMemberMethod();
                raisePropertyChangedEventMethod.Name = "RaisePropertyChanged";
                raisePropertyChangedEventMethod.Attributes = MemberAttributes.Final;
                CodeArgumentReferenceExpression propertyName = new CodeArgumentReferenceExpression("propertyName");
                raisePropertyChangedEventMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), propertyName.ParameterName));
                CodeVariableReferenceExpression propertyChanged = new CodeVariableReferenceExpression("propertyChanged");
                raisePropertyChangedEventMethod.Statements.Add(new CodeVariableDeclarationStatement(typeof(PropertyChangedEventHandler), propertyChanged.VariableName, new CodeEventReferenceExpression(ThisReference, PropertyChangedEvent.Name)));
                CodeConditionStatement ifStatement = new CodeConditionStatement(new CodeBinaryOperatorExpression(propertyChanged, CodeBinaryOperatorType.IdentityInequality, NullReference));
                raisePropertyChangedEventMethod.Statements.Add(ifStatement);
                ifStatement.TrueStatements.Add(new CodeDelegateInvokeExpression(propertyChanged, ThisReference, new CodeObjectCreateExpression(typeof(PropertyChangedEventArgs), propertyName)));
                return raisePropertyChangedEventMethod;
            }
        }

        private CodeMemberEvent PropertyChangedEvent
        {
            get
            {
                CodeMemberEvent propertyChangedEvent = new CodeMemberEvent();
                propertyChangedEvent.Attributes = MemberAttributes.Public;
                propertyChangedEvent.Name = "PropertyChanged";
                propertyChangedEvent.Type = GetCodeTypeReference(typeof(PropertyChangedEventHandler));
                propertyChangedEvent.ImplementationTypes.Add(typeof(INotifyPropertyChanged));
                return propertyChangedEvent;
            }
        }

        private CodeMemberProperty CreateProperty(CodeTypeReference type, string propertyName, string fieldName, bool isValueType)
        {
            return CreateProperty(type, propertyName, fieldName, isValueType, EnableDataBinding && SupportsDeclareEvents);
        }

        private CodeMemberProperty CreateProperty(CodeTypeReference type, string propertyName, string fieldName, bool isValueType, bool raisePropertyChanged)
        {
            CodeMemberProperty property = new CodeMemberProperty();
            property.Type = type;
            property.Name = propertyName;
            property.Attributes = MemberAttributes.Final;
            if (GenerateInternalTypes)
                property.Attributes |= MemberAttributes.Assembly;
            else
                property.Attributes |= MemberAttributes.Public;

            CodeMethodReturnStatement propertyGet = new CodeMethodReturnStatement();
            propertyGet.Expression = new CodeFieldReferenceExpression(ThisReference, fieldName);
            property.GetStatements.Add(propertyGet);

            CodeAssignStatement propertySet = new CodeAssignStatement();
            propertySet.Left = new CodeFieldReferenceExpression(ThisReference, fieldName);
            propertySet.Right = new CodePropertySetValueReferenceExpression();
            if (raisePropertyChanged)
            {
                CodeConditionStatement ifStatement = new CodeConditionStatement();
                CodeExpression left = new CodeFieldReferenceExpression(ThisReference, fieldName);
                CodeExpression right = new CodePropertySetValueReferenceExpression();
                if (!isValueType)
                {
                    left = new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(object)),
                        "ReferenceEquals", new CodeExpression[] { left, right });
                }
                else
                {
                    left = new CodeMethodInvokeExpression(left, "Equals", new CodeExpression[] { right });
                }
                right = new CodePrimitiveExpression(true);
                ifStatement.Condition = new CodeBinaryOperatorExpression(left, CodeBinaryOperatorType.IdentityInequality, right);
                ifStatement.TrueStatements.Add(propertySet);
                ifStatement.TrueStatements.Add(new CodeMethodInvokeExpression(ThisReference, RaisePropertyChangedEventMethod.Name, new CodePrimitiveExpression(propertyName)));
                property.SetStatements.Add(ifStatement);
            }
            else
                property.SetStatements.Add(propertySet);
            return property;
        }
    }
}
