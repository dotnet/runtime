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
using System.Runtime.Serialization;
using System.Security;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

using DataContractDictionary = System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, System.Runtime.Serialization.DataContract>;
using ExceptionUtil = System.Runtime.Serialization.Schema.DiagnosticUtility.ExceptionUtility;

namespace System.Runtime.Serialization.Schema
{
    internal sealed class CodeExporter
    {
        private const string WildcardNamespaceMapping = "*";
        private const string TypeNameFieldName = "typeName";
        private const int MaxIdentifierLength = 511;

        private static readonly object s_codeUserDataActualTypeKey = new object();
        private static readonly object s_surrogateDataKey = typeof(ISerializationExtendedSurrogateProvider);

        private DataContractSet _dataContractSet;
        private CodeCompileUnit _codeCompileUnit;
        private ImportOptions? _options;
        private Dictionary<string, string> _namespaces;
        private Dictionary<string, string?> _clrNamespaces;

        internal CodeExporter(DataContractSet dataContractSet, ImportOptions? options, CodeCompileUnit codeCompileUnit)
        {
            _dataContractSet = dataContractSet;
            _codeCompileUnit = codeCompileUnit;
            AddReferencedAssembly(Assembly.GetExecutingAssembly());
            _options = options;
            _namespaces = new Dictionary<string, string>();
            _clrNamespaces = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            // Update namespace tables for DataContract(s) that are already processed
            foreach (KeyValuePair<XmlQualifiedName, DataContract> pair in dataContractSet)
            {
                DataContract dataContract = pair.Value;
                if (!(dataContract.IsBuiltInDataContract || dataContract is CollectionDataContract))
                {
                    ContractCodeDomInfo contractCodeDomInfo = GetContractCodeDomInfo(dataContract);
                    if (contractCodeDomInfo.IsProcessed && !contractCodeDomInfo.UsesWildcardNamespace)
                    {
                        string? clrNamespace = contractCodeDomInfo.ClrNamespace;
                        if (clrNamespace != null && !_clrNamespaces.ContainsKey(clrNamespace))
                        {
                            _clrNamespaces.Add(clrNamespace, dataContract.StableName.Namespace);
                            _namespaces.Add(dataContract.StableName.Namespace, clrNamespace);
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
                    string clrNamespace = pair.Value;
                    if (clrNamespace == null)
                        clrNamespace = string.Empty;

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

        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
        internal void Export()
        {
            try
            {
                foreach (KeyValuePair<XmlQualifiedName, DataContract> pair in _dataContractSet)
                {
                    DataContract dataContract = pair.Value;
                    if (dataContract.IsBuiltInDataContract)
                        continue;

                    ContractCodeDomInfo contractCodeDomInfo = GetContractCodeDomInfo(dataContract);
                    if (!contractCodeDomInfo.IsProcessed)
                    {
                        if (dataContract is ClassDataContract classDataContract)
                        {
                            if (classDataContract.IsISerializable)
                                ExportISerializableDataContract(classDataContract, contractCodeDomInfo);
                            else
                                ExportClassDataContractHierarchy(classDataContract.StableName, classDataContract, contractCodeDomInfo, new Dictionary<XmlQualifiedName, object?>());
                        }
                        else if (dataContract is CollectionDataContract)
                            ExportCollectionDataContract((CollectionDataContract)dataContract, contractCodeDomInfo);
                        else if (dataContract is EnumDataContract)
                            ExportEnumDataContract((EnumDataContract)dataContract, contractCodeDomInfo);
                        else if (dataContract is XmlDataContract)
                            ExportXmlDataContract((XmlDataContract)dataContract, contractCodeDomInfo);
                        else
                            throw ExceptionUtil.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.UnexpectedContractType, GetClrTypeFullName(dataContract.GetType()), GetClrTypeFullName(dataContract.UnderlyingType))));
                        contractCodeDomInfo.IsProcessed = true;
                    }
                }

                if (_options?.ProcessImportedType != null)
                {
                    CodeNamespace[] namespaces = new CodeNamespace[_codeCompileUnit.Namespaces.Count];
                    _codeCompileUnit.Namespaces.CopyTo(namespaces, 0);
                    foreach (CodeNamespace codeNamespace in namespaces)
                        InvokeProcessImportedType(codeNamespace.Types);
                }
            }
            finally
            {
                CodeGenerator.ValidateIdentifiers(_codeCompileUnit);
            }
        }

        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
        private void ExportClassDataContractHierarchy(XmlQualifiedName typeName, ClassDataContract classContract, ContractCodeDomInfo contractCodeDomInfo, Dictionary<XmlQualifiedName, object?> contractNamesInHierarchy)
        {
            if (contractNamesInHierarchy.ContainsKey(classContract.StableName))
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.TypeCannotBeImported, typeName.Name, typeName.Namespace, SR.Format(SR.CircularTypeReference, classContract.StableName.Name, classContract.StableName.Namespace))));
            contractNamesInHierarchy.Add(classContract.StableName, null);

            ClassDataContract? baseContract = classContract.BaseContract;
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

        private void InvokeProcessImportedType(CollectionBase collection)
        {
            object[] objects = new object[collection.Count];
            ((ICollection)collection).CopyTo(objects, 0);

            Debug.Assert(_options?.ProcessImportedType != null);   // The only caller into this method already did a null check for this Func

            foreach (object obj in objects)
            {
                if (obj is CodeTypeDeclaration codeTypeDeclaration)
                {
                    CodeTypeDeclaration? newCodeTypeDeclaration = _options?.ProcessImportedType.Invoke(codeTypeDeclaration, _codeCompileUnit);

                    if (newCodeTypeDeclaration != codeTypeDeclaration)
                    {
                        ((IList)collection).Remove(codeTypeDeclaration);
                        if (newCodeTypeDeclaration != null)
                            ((IList)collection).Add(newCodeTypeDeclaration);
                    }
                    if (newCodeTypeDeclaration != null)
                        InvokeProcessImportedType(newCodeTypeDeclaration.Members);
                }
            }
    }

        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
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

        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
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
            [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
            get { return DataContract.GetStableName(typeof(List<>)); }
        }

        private CollectionDataContract GenericListContract
        {
            [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
            get { return (_dataContractSet.GetDataContract(typeof(List<>)) as CollectionDataContract)!; }
        }

        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Part of a logical set of properties, some of which are not static. May not remain static with future implementation updates.")]
        private XmlQualifiedName GenericDictionaryName
        {
            [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
            get { return DataContract.GetStableName(typeof(Dictionary<,>)); }
        }

        private CollectionDataContract GenericDictionaryContract
        {
            [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
            get { return (_dataContractSet.GetDataContract(typeof(Dictionary<,>)) as CollectionDataContract)!; }
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

        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
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
                        CodeNamespace ns = GetCodeNamespace(clrNamespace, dataContract.StableName.Namespace, contractCodeDomInfo);
                        type = GetNestedType(dataContract, contractCodeDomInfo);
                        if (type == null)
                        {
                            string typeName = XmlConvert.DecodeName(dataContract.StableName.Name);
                            typeName = GetClrIdentifier(typeName, Globals.DefaultTypeName);
                            if (NamespaceContainsType(ns, typeName) || GlobalTypeNameConflicts(clrNamespace, typeName))
                            {
                                for (int i = 1;; i++)
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

                        if (_dataContractSet.TryGetSurrogateData(dataContract, out object? surrogateData))
                            type.UserData.Add(s_surrogateDataKey, surrogateData);

                        contractCodeDomInfo.TypeDeclaration = type;
                    }
                }
            }
        }

        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
        private CodeTypeDeclaration? GetNestedType(DataContract dataContract, ContractCodeDomInfo contractCodeDomInfo)
        {
            if (!SupportsNestedTypes)
                return null;
            string originalName = dataContract.StableName.Name;
            int nestedTypeIndex = originalName.LastIndexOf('.');
            if (nestedTypeIndex <= 0)
                return null;
            string containingTypeName = originalName.Substring(0, nestedTypeIndex);
            DataContract? containingDataContract = _dataContractSet.GetDataContract(new XmlQualifiedName(containingTypeName, dataContract.StableName.Namespace));
            if (containingDataContract == null)
                return null;
            string nestedTypeName = XmlConvert.DecodeName(originalName.Substring(nestedTypeIndex + 1));
            nestedTypeName = GetClrIdentifier(nestedTypeName, Globals.DefaultTypeName);

            ContractCodeDomInfo containingContractCodeDomInfo = GetContractCodeDomInfo(containingDataContract);
            GenerateType(containingDataContract, containingContractCodeDomInfo);
            if (containingContractCodeDomInfo.ReferencedTypeExists)
                return null;

            CodeTypeDeclaration containingType = containingContractCodeDomInfo.TypeDeclaration!; // Nested types by definition have containing types.
            if (TypeContainsNestedType(containingType, nestedTypeName))
            {
                for (int i = 1;; i++)
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
            generatedCodeAttribute.Arguments.Add(new CodeAttributeArgument(new CodePrimitiveExpression(assemblyName.Name)));
            generatedCodeAttribute.Arguments.Add(new CodeAttributeArgument(new CodePrimitiveExpression(assemblyName.Version?.ToString())));

            // System.Diagnostics.DebuggerStepThroughAttribute not allowed on enums
            // ensure that the attribute is only generated on types that are not enums
            EnumDataContract? enumDataContract = dataContract as EnumDataContract;
            if (enumDataContract == null)
            {
                typeDecl.CustomAttributes.Add(debuggerStepThroughAttribute);
            }
            typeDecl.CustomAttributes.Add(generatedCodeAttribute);
            return typeDecl;
        }

        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
        private CodeTypeReference? GetReferencedType(DataContract dataContract)
        {
            CodeTypeReference? typeReference = GetSurrogatedTypeReference(dataContract);
            if (typeReference != null)
                return typeReference;

            if (_dataContractSet.TryGetReferencedType(dataContract.StableName, dataContract, out Type? type)
                && !type.IsGenericTypeDefinition && !type.ContainsGenericParameters)
            {
                if (dataContract is XmlDataContract)
                {
                    if (typeof(IXmlSerializable).IsAssignableFrom(type))
                    {
                        XmlDataContract xmlContract = (XmlDataContract)dataContract;
                        if (xmlContract.IsTypeDefinedOnImport)
                        {
                            if (!xmlContract.Equals(_dataContractSet.GetDataContract(type)))
                                throw ExceptionUtil.ThrowHelperError(new InvalidOperationException(SR.Format(SR.ReferencedTypeDoesNotMatch, type.AssemblyQualifiedName, dataContract.StableName.Name, dataContract.StableName.Namespace)));
                        }
                        else
                        {
                            xmlContract.IsValueType = type.IsValueType;
                            xmlContract.IsTypeDefinedOnImport = true;
                        }
                        return GetCodeTypeReference(type);
                    }
                    throw ExceptionUtil.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.TypeMustBeIXmlSerializable, GetClrTypeFullName(type), GetClrTypeFullName(typeof(IXmlSerializable)), dataContract.StableName.Name, dataContract.StableName.Namespace)));
                }
                DataContract referencedContract = _dataContractSet.GetDataContract(type);
                if (referencedContract.Equals(dataContract))
                {
                    typeReference = GetCodeTypeReference(type);
                    typeReference.UserData.Add(s_codeUserDataActualTypeKey, type);
                    return typeReference;
                }
                throw ExceptionUtil.ThrowHelperError(new InvalidOperationException(SR.Format(SR.ReferencedTypeDoesNotMatch, type.AssemblyQualifiedName, dataContract.StableName.Name, dataContract.StableName.Namespace)));
            }
            else if (dataContract.GenericInfo != null)
            {
                DataContract? referencedContract;
                XmlQualifiedName genericStableName = dataContract.GenericInfo.GetExpandedStableName();
                if (genericStableName != dataContract.StableName)
                    throw ExceptionUtil.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.GenericTypeNameMismatch, dataContract.StableName.Name, dataContract.StableName.Namespace, genericStableName.Name, genericStableName.Namespace)));

                typeReference = GetReferencedGenericType(dataContract.GenericInfo, out referencedContract);
                if (referencedContract != null && !referencedContract.Equals(dataContract))
                {
                    // NOTE TODO smolloy - Is this right? Looking at 'GetReferenceGenericType()'  it is possible to get a non-null referencedContract, but a null typeReference. Is that supposed to happen?
                    // Are we supposed to only check the dataContract if we did not fail to get a typeReference? Should GetReferenceGenericType() be consistent with the two return parameters?
                    // For now... assert to get out of the way of compilation. But come revisit this.
                    Debug.Assert(typeReference != null);
                    type = (Type?)typeReference.UserData[s_codeUserDataActualTypeKey];
                    throw ExceptionUtil.ThrowHelperError(new InvalidOperationException(SR.Format(SR.ReferencedTypeDoesNotMatch,
                        type?.AssemblyQualifiedName,
                        referencedContract.StableName.Name,
                        referencedContract.StableName.Namespace)));
                }
                return typeReference;
            }

            return GetReferencedCollectionType(dataContract as CollectionDataContract);
        }

        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
        private CodeTypeReference? GetReferencedCollectionType(CollectionDataContract? collectionContract)
        {
            if (collectionContract == null)
                return null;

            if (HasDefaultCollectionNames(collectionContract))
            {
                CodeTypeReference? typeReference;
                if (!TryGetReferencedDictionaryType(collectionContract, out typeReference))
                {
                    DataContract itemContract = collectionContract.ItemContract;
                    if (collectionContract.IsDictionary)
                    {
                        GenerateKeyValueType(itemContract as ClassDataContract);
                    }
                    bool isItemTypeNullable = collectionContract.IsItemTypeNullable;
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

        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
        private static bool HasDefaultCollectionNames(CollectionDataContract collectionContract)
        {
            DataContract itemContract = collectionContract.ItemContract;
            if (collectionContract.ItemName != itemContract.StableName.Name)
                return false;

            if (collectionContract.IsDictionary &&
                (collectionContract.KeyName != Globals.KeyLocalName || collectionContract.ValueName != Globals.ValueLocalName))
                return false;

            XmlQualifiedName expectedType = itemContract.GetArrayTypeName(collectionContract.IsItemTypeNullable);
            return (collectionContract.StableName.Name == expectedType.Name && collectionContract.StableName.Namespace == expectedType.Namespace);
        }

        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
        private bool TryGetReferencedDictionaryType(CollectionDataContract collectionContract, [NotNullWhen(true)] out CodeTypeReference? typeReference)
        {
            // Check if it is a dictionary and use referenced dictionary type if present
            if (collectionContract.IsDictionary
                && SupportsGenericTypeReference)
            {
                Type? type;
                if (!_dataContractSet.TryGetReferencedType(GenericDictionaryName, GenericDictionaryContract, out type))
                    type = typeof(Dictionary<,>);

                ClassDataContract? itemContract = collectionContract.ItemContract as ClassDataContract;

                // A dictionary should have a Key/Value item contract that has at least two members: key and value.
                Debug.Assert(itemContract != null);
                Debug.Assert(itemContract.Members != null && itemContract.Members.Count > 1);

                DataMember keyMember = itemContract.Members[0];
                DataMember valueMember = itemContract.Members[1];
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

        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
        private bool TryGetReferencedListType(DataContract itemContract, bool isItemTypeNullable, out CodeTypeReference? typeReference)
        {
            Type? type;
            if (SupportsGenericTypeReference && _dataContractSet.TryGetReferencedType(GenericListName, GenericListContract, out type))
            {
                typeReference = GetCodeTypeReference(type);
                typeReference.TypeArguments.Add(GetElementTypeReference(itemContract, isItemTypeNullable)!);    // Lists have an item type
                return true;
            }
            typeReference = null;
            return false;
        }

        [RequiresUnreferencedCode("TODO smolloy - better string here")]
        private CodeTypeReference? GetSurrogatedTypeReference(DataContract dataContract)
        {
            Type? type = _dataContractSet.GetReferencedTypeOnImport(dataContract);
            if (type != null)
            {
                CodeTypeReference typeReference = GetCodeTypeReference(type);
                typeReference.UserData.Add(s_codeUserDataActualTypeKey, type);
                return typeReference;
            }
            return null;

            // TODO smolloy - the stuff above replaces the stuff below completely. The stuff below
            // is left here as a reference for when DCSet gets fleshed out with this logic.

            //ISerializationExtendedSurrogateProvider? dataContractSurrogate = _dataContractSet.SerializationExtendedSurrogateProvider;
            //if (dataContractSurrogate != null)
            //{
            //    Type? type = DataContractSurrogateCaller.GetReferencedTypeOnImport(
            //            dataContractSurrogate,
            //            dataContract.StableName.Name,
            //            dataContract.StableName.Namespace,
            //            _dataContractSet.GetSurrogateData(dataContract));
            //    if (type != null)
            //    {
            //        CodeTypeReference typeReference = GetCodeTypeReference(type);
            //        typeReference.UserData.Add(s_codeUserDataActualTypeKey, type);
            //        return typeReference;
            //    }
            //}
            //return null;
        }

        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
        private CodeTypeReference? GetReferencedGenericType(GenericInfo genInfo, out DataContract? dataContract)
        {
            dataContract = null;

            if (!SupportsGenericTypeReference)
                return null;

            Type? type;
            if (!_dataContractSet.TryGetReferencedType(genInfo.StableName, null, out type))
            {
                if (genInfo.Parameters != null)
                    return null;
                dataContract = _dataContractSet.GetDataContract(genInfo.StableName);
                if (dataContract == null)
                    return null;
                if (dataContract.GenericInfo != null)
                    return null;
                return GetCodeTypeReference(dataContract);
            }

            bool enableStructureCheck = (type != typeof(Nullable<>));
            CodeTypeReference typeReference = GetCodeTypeReference(type);
            typeReference.UserData.Add(s_codeUserDataActualTypeKey, type);
            if (genInfo.Parameters != null)
            {
                DataContract[] paramContracts = new DataContract[genInfo.Parameters.Count];
                for (int i = 0; i < genInfo.Parameters.Count; i++)
                {
                    GenericInfo paramInfo = genInfo.Parameters[i];
                    XmlQualifiedName stableName = paramInfo.GetExpandedStableName();
                    DataContract? paramContract = _dataContractSet.GetDataContract(stableName);

                    CodeTypeReference? paramTypeReference;
                    bool isParamValueType;
                    if (paramContract != null)
                    {
                        paramTypeReference = GetCodeTypeReference(paramContract);
                        isParamValueType = paramContract.IsValueType;
                    }
                    else
                    {
                        paramTypeReference = GetReferencedGenericType(paramInfo, out paramContract);
                        isParamValueType = (paramTypeReference != null && paramTypeReference.ArrayRank == 0); // only value type information we can get from CodeTypeReference
                    }
                    paramContracts[i] = paramContract!; // Potentially tricky here. We could assign a null item here, and that's ok. We subsequently disable the structure check in that case. See note below.
                    if (paramContract == null)
                        enableStructureCheck = false;
                    if (paramTypeReference == null)
                        return null;
                    if (type == typeof(Nullable<>) && !isParamValueType)
                        return paramTypeReference;
                    else
                        typeReference.TypeArguments.Add(paramTypeReference);
                }
                // paramContracts could contain null values, but if it does, this structure check is disabled. So we know paramContracts has no null values if we go through with this call.
                if (enableStructureCheck)
                    dataContract = DataContract.GetDataContract(type).BindGenericParameters(paramContracts, new Dictionary<DataContract, DataContract>());
            }
            return typeReference;
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
            if (string.CompareOrdinal(name, decodedName) == 0)
                return name;
            string reencodedName = DataContract.EncodeLocalName(decodedName);
            return (string.CompareOrdinal(name, reencodedName) == 0) ? decodedName : name;
        }

        private void AddSerializableAttribute(bool generateSerializable, CodeTypeDeclaration type, ContractCodeDomInfo contractCodeDomInfo)
        {
            if (generateSerializable)
            {
                type.CustomAttributes.Add(SerializableAttribute);
                AddImportStatement(typeof(SerializableAttribute).Namespace, contractCodeDomInfo.CodeNamespace);
            }
        }

        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
        private void ExportClassDataContract(ClassDataContract classDataContract, ContractCodeDomInfo contractCodeDomInfo)
        {
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

            string dataContractName = GetNameForAttribute(classDataContract.StableName.Name);
            CodeAttributeDeclaration dataContractAttribute = new CodeAttributeDeclaration(GetClrTypeFullName(typeof(DataContractAttribute)));
            dataContractAttribute.Arguments.Add(new CodeAttributeArgument(Globals.NameProperty, new CodePrimitiveExpression(dataContractName)));
            dataContractAttribute.Arguments.Add(new CodeAttributeArgument(Globals.NamespaceProperty, new CodePrimitiveExpression(classDataContract.StableName.Namespace)));
            if (classDataContract.IsReference != Globals.DefaultIsReference)
                dataContractAttribute.Arguments.Add(new CodeAttributeArgument(Globals.IsReferenceProperty, new CodePrimitiveExpression(classDataContract.IsReference)));
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
                type.BaseTypes.Add(baseContractCodeDomInfo.TypeReference);
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

            if (classDataContract.Members != null)
            {
                for (int i = 0; i < classDataContract.Members.Count; i++)
                {
                    DataMember dataMember = classDataContract.Members[i];

                    CodeTypeReference memberType = GetElementTypeReference(dataMember.MemberTypeContract,
                        (dataMember.IsNullable && dataMember.MemberTypeContract.IsValueType));

                    string dataMemberName = GetNameForAttribute(dataMember.Name);
                    string propertyName = GetMemberName(dataMemberName, contractCodeDomInfo);
                    string fieldName = GetMemberName(AppendToValidClrIdentifier(propertyName, Globals.DefaultFieldSuffix), contractCodeDomInfo);

                    CodeMemberField field = new CodeMemberField();
                    field.Type = memberType;
                    field.Name = fieldName;
                    field.Attributes = MemberAttributes.Private;

                    CodeMemberProperty property = CreateProperty(memberType, propertyName, fieldName, dataMember.MemberTypeContract.IsValueType && SupportsDeclareValueTypes, raisePropertyChanged);
                    if (_dataContractSet.TryGetSurrogateData(dataMember, out object? surrogateData))
                        property.UserData.Add(s_surrogateDataKey, surrogateData);

                    CodeAttributeDeclaration dataMemberAttribute = new CodeAttributeDeclaration(GetClrTypeFullName(typeof(DataMemberAttribute)));
                    if (dataMemberName != property.Name)
                        dataMemberAttribute.Arguments.Add(new CodeAttributeArgument(Globals.NameProperty, new CodePrimitiveExpression(dataMemberName)));
                    if (dataMember.IsRequired != Globals.DefaultIsRequired)
                        dataMemberAttribute.Arguments.Add(new CodeAttributeArgument(Globals.IsRequiredProperty, new CodePrimitiveExpression(dataMember.IsRequired)));
                    if (dataMember.EmitDefaultValue != Globals.DefaultEmitDefaultValue)
                        dataMemberAttribute.Arguments.Add(new CodeAttributeArgument(Globals.EmitDefaultValueProperty, new CodePrimitiveExpression(dataMember.EmitDefaultValue)));
                    if (dataMember.Order != Globals.DefaultOrder)
                        dataMemberAttribute.Arguments.Add(new CodeAttributeArgument(Globals.OrderProperty, new CodePrimitiveExpression(dataMember.Order)));
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
        }

        private bool CanDeclareAssemblyAttribute(ContractCodeDomInfo contractCodeDomInfo)
        {
            return SupportsAssemblyAttributes && !contractCodeDomInfo.UsesWildcardNamespace;
        }

        private static bool NeedsExplicitNamespace(string dataContractNamespace, string clrNamespace)
        {
            return (SchemaHelper.GetDefaultStableNamespace(clrNamespace) != dataContractNamespace);
        }

        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
        internal ICollection<CodeTypeReference>? GetKnownTypeReferences(DataContract dataContract)
        {
            DataContractDictionary? knownTypeDictionary = GetKnownTypeContracts(dataContract);
            if (knownTypeDictionary == null)
                return null;

            ICollection<DataContract>? knownTypeContracts = knownTypeDictionary.Values;
            if (knownTypeContracts == null || knownTypeContracts.Count == 0)
                return null;

            List<CodeTypeReference> knownTypeReferences = new List<CodeTypeReference>();
            foreach (DataContract knownTypeContract in knownTypeContracts)
            {
                knownTypeReferences.Add(GetCodeTypeReference(knownTypeContract));
            }
            return knownTypeReferences;
        }

        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
        private DataContractDictionary? GetKnownTypeContracts(DataContract dataContract)
        {
            if (_dataContractSet.KnownTypesForObject != null && IsObjectContract(dataContract))
            {
                return _dataContractSet.KnownTypesForObject;
            }
            else if (dataContract is ClassDataContract)
            {
                ContractCodeDomInfo contractCodeDomInfo = GetContractCodeDomInfo(dataContract);
                if (!contractCodeDomInfo.IsProcessed)
                    GenerateType(dataContract, contractCodeDomInfo);
                if (contractCodeDomInfo.ReferencedTypeExists)
                    return GetKnownTypeContracts((ClassDataContract)dataContract, new Dictionary<DataContract, object?>());
            }
            return null;
        }

        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
        private DataContractDictionary? GetKnownTypeContracts(ClassDataContract dataContract, Dictionary<DataContract, object?> handledContracts)
        {
            if (handledContracts.ContainsKey(dataContract))
                return dataContract.KnownDataContracts;

            handledContracts.Add(dataContract, null);
            if (dataContract.Members != null)
            {
                bool objectMemberHandled = false;
                foreach (DataMember dataMember in dataContract.Members)
                {
                    DataContract memberContract = dataMember.MemberTypeContract;
                    if (!objectMemberHandled && _dataContractSet.KnownTypesForObject != null && IsObjectContract(memberContract))
                    {
                        AddKnownTypeContracts(dataContract, _dataContractSet.KnownTypesForObject);
                        objectMemberHandled = true;
                    }
                    else if (memberContract is ClassDataContract)
                    {
                        ContractCodeDomInfo memberCodeDomInfo = GetContractCodeDomInfo(memberContract);
                        if (!memberCodeDomInfo.IsProcessed)
                            GenerateType(memberContract, memberCodeDomInfo);
                        if (memberCodeDomInfo.ReferencedTypeExists)
                        {
                            AddKnownTypeContracts(dataContract, GetKnownTypeContracts((ClassDataContract)memberContract, handledContracts));
                        }
                    }
                }
            }

            return dataContract.KnownDataContracts;
        }

        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
        private static void AddKnownTypeContracts(ClassDataContract dataContract, DataContractDictionary? knownContracts)
        {
            if (knownContracts == null || knownContracts.Count == 0)
                return;

            if (dataContract.KnownDataContracts == null)
                dataContract.KnownDataContracts = new DataContractDictionary();

            foreach (KeyValuePair<XmlQualifiedName, DataContract> pair in knownContracts)
            {
                if (dataContract.StableName != pair.Key && !dataContract.KnownDataContracts.ContainsKey(pair.Key) && !pair.Value.IsBuiltInDataContract)
                    dataContract.KnownDataContracts.Add(pair.Key, pair.Value);
            }
        }

        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
        private void AddKnownTypes(ClassDataContract dataContract, ContractCodeDomInfo contractCodeDomInfo)
        {
            DataContractDictionary? knownContractDictionary = GetKnownTypeContracts(dataContract, new Dictionary<DataContract, object?>());
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
                contractCodeDomInfo.GetMemberNames().Add(extensionDataObjectField.Name);
                CodeMemberProperty extensionDataObjectProperty = ExtensionDataObjectProperty;
                type.Members.Add(extensionDataObjectProperty);
                contractCodeDomInfo.GetMemberNames().Add(extensionDataObjectProperty.Name);
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
                contractCodeDomInfo.GetMemberNames().Add(memberEvent.Name);
                contractCodeDomInfo.GetMemberNames().Add(raisePropertyChangedEventMethod.Name);
            }
        }

        private static void ThrowIfReferencedBaseTypeSealed(Type baseType, DataContract dataContract)
        {
            if (baseType.IsSealed)
                throw ExceptionUtil.ThrowHelperError(new InvalidOperationException(SR.Format(SR.CannotDeriveFromSealedReferenceType, dataContract.StableName.Name, dataContract.StableName.Namespace, GetClrTypeFullName(baseType))));
        }

        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
        private void ExportEnumDataContract(EnumDataContract enumDataContract, ContractCodeDomInfo contractCodeDomInfo)
        {
            GenerateType(enumDataContract, contractCodeDomInfo);
            if (contractCodeDomInfo.ReferencedTypeExists)
                return;

            // This is supposed to be set by GenerateType. If it wasn't, there is a problem.
            Debug.Assert(contractCodeDomInfo.TypeDeclaration != null);

            CodeTypeDeclaration type = contractCodeDomInfo.TypeDeclaration;
            type.IsEnum = true;
            type.BaseTypes.Add(EnumDataContract.GetBaseType(enumDataContract.BaseContractName));
            if (enumDataContract.IsFlags)
            {
                type.CustomAttributes.Add(new CodeAttributeDeclaration(GetClrTypeFullName(typeof(FlagsAttribute))));
                AddImportStatement(typeof(FlagsAttribute).Namespace, contractCodeDomInfo.CodeNamespace);
            }

            string dataContractName = GetNameForAttribute(enumDataContract.StableName.Name);
            CodeAttributeDeclaration dataContractAttribute = new CodeAttributeDeclaration(GetClrTypeFullName(typeof(DataContractAttribute)));
            dataContractAttribute.Arguments.Add(new CodeAttributeArgument(Globals.NameProperty, new CodePrimitiveExpression(dataContractName)));
            dataContractAttribute.Arguments.Add(new CodeAttributeArgument(Globals.NamespaceProperty, new CodePrimitiveExpression(enumDataContract.StableName.Namespace)));
            type.CustomAttributes.Add(dataContractAttribute);
            AddImportStatement(typeof(DataContractAttribute).Namespace, contractCodeDomInfo.CodeNamespace);

            if (enumDataContract.Members != null)
            {
                for (int i = 0; i < enumDataContract.Members.Count; i++)
                {
                    string stringValue = enumDataContract.Members[i].Name;
                    long longValue = enumDataContract.Values![i];   // Members[] and Values[] go hand in hand.

                    CodeMemberField enumMember = new CodeMemberField();
                    if (enumDataContract.IsULong)
                        enumMember.InitExpression = new CodeSnippetExpression(enumDataContract.GetStringFromEnumValue(longValue));
                    else
                        enumMember.InitExpression = new CodePrimitiveExpression(longValue);
                    enumMember.Name = GetMemberName(stringValue, contractCodeDomInfo);
                    CodeAttributeDeclaration enumMemberAttribute = new CodeAttributeDeclaration(GetClrTypeFullName(typeof(EnumMemberAttribute)));
                    if (enumMember.Name != stringValue)
                        enumMemberAttribute.Arguments.Add(new CodeAttributeArgument(Globals.ValueProperty, new CodePrimitiveExpression(stringValue)));
                    enumMember.CustomAttributes.Add(enumMemberAttribute);
                    type.Members.Add(enumMember);
                }
            }
        }

        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
        private void ExportISerializableDataContract(ClassDataContract dataContract, ContractCodeDomInfo contractCodeDomInfo)
        {
            GenerateType(dataContract, contractCodeDomInfo);
            if (contractCodeDomInfo.ReferencedTypeExists)
                return;

            if (SchemaHelper.GetDefaultStableNamespace(contractCodeDomInfo.ClrNamespace) != dataContract.StableName.Namespace)
                throw ExceptionUtil.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.InvalidClrNamespaceGeneratedForISerializable, dataContract.StableName.Name, dataContract.StableName.Namespace, SchemaHelper.GetDataContractNamespaceFromUri(dataContract.StableName.Namespace), contractCodeDomInfo.ClrNamespace)));

            string dataContractName = GetNameForAttribute(dataContract.StableName.Name);
            int nestedTypeIndex = dataContractName.LastIndexOf('.');
            string expectedName = (nestedTypeIndex <= 0 || nestedTypeIndex == dataContractName.Length - 1) ? dataContractName : dataContractName.Substring(nestedTypeIndex + 1);

            // This is supposed to be set by GenerateType. If it wasn't, there is a problem.
            Debug.Assert(contractCodeDomInfo.TypeDeclaration != null);

            if (contractCodeDomInfo.TypeDeclaration.Name != expectedName)
                throw ExceptionUtil.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.InvalidClrNameGeneratedForISerializable, dataContract.StableName.Name, dataContract.StableName.Namespace, contractCodeDomInfo.TypeDeclaration.Name)));

            CodeTypeDeclaration type = contractCodeDomInfo.TypeDeclaration;
            if (SupportsPartialTypes)
                type.IsPartial = true;
            if (dataContract.IsValueType && SupportsDeclareValueTypes)
                type.IsStruct = true;
            else
                type.IsClass = true;

            AddSerializableAttribute(true /*generateSerializable*/, type, contractCodeDomInfo);

            AddKnownTypes(dataContract, contractCodeDomInfo);

            if (dataContract.BaseContract == null)
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
                ContractCodeDomInfo baseContractCodeDomInfo = GetContractCodeDomInfo(dataContract.BaseContract);
                GenerateType(dataContract.BaseContract, baseContractCodeDomInfo);
                type.BaseTypes.Add(baseContractCodeDomInfo.TypeReference);
                if (baseContractCodeDomInfo.ReferencedTypeExists)
                {
                    Type? actualType = (Type?)baseContractCodeDomInfo.TypeReference?.UserData[s_codeUserDataActualTypeKey];
                    Debug.Assert(actualType != null);   // If we're in this if condition, then we should be able to get a Type
                    ThrowIfReferencedBaseTypeSealed(actualType, dataContract);
                }
                type.Members.Add(ISerializableDerivedConstructor);
            }
        }

        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
        private void GenerateKeyValueType(ClassDataContract? keyValueContract)
        {
            // Add code for KeyValue item type in the case where its usage is limited to dictionary
            // and dictionary is not found in referenced types
            if (keyValueContract != null && _dataContractSet.GetDataContract(keyValueContract.StableName) == null)
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

        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
        private void ExportCollectionDataContract(CollectionDataContract collectionContract, ContractCodeDomInfo contractCodeDomInfo)
        {
            GenerateType(collectionContract, contractCodeDomInfo);
            if (contractCodeDomInfo.ReferencedTypeExists)
                return;

            string dataContractName = GetNameForAttribute(collectionContract.StableName.Name);

            // If type name is not expected, generate collection type that derives from referenced list type and uses [CollectionDataContract]
            if (!SupportsGenericTypeReference)
                throw ExceptionUtil.ThrowHelperError(new InvalidOperationException(
                    SR.Format(SR.CannotUseGenericTypeAsBase, dataContractName,
                    collectionContract.StableName.Namespace)));

            DataContract itemContract = collectionContract.ItemContract;
            bool isItemTypeNullable = collectionContract.IsItemTypeNullable;

            CodeTypeReference? baseTypeReference;
            bool foundDictionaryBase = TryGetReferencedDictionaryType(collectionContract, out baseTypeReference);
            if (!foundDictionaryBase)
            {
                if (collectionContract.IsDictionary)
                {
                    GenerateKeyValueType(collectionContract.ItemContract as ClassDataContract);
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
                        string expectedTypeName = Globals.ArrayPrefix + itemContract.StableName.Name;
                        string expectedTypeNs = SchemaHelper.GetCollectionNamespace(itemContract.StableName.Namespace);
                        throw ExceptionUtil.ThrowHelperError(new InvalidOperationException(SR.Format(SR.ReferencedBaseTypeDoesNotExist,
                            dataContractName, collectionContract.StableName.Namespace,
                            expectedTypeName, expectedTypeNs, GetClrTypeFullName(typeof(IList<>)), GetClrTypeFullName(typeof(ICollection<>)))));
                    }
                }
            }

            // This is supposed to be set by GenerateType. If it wasn't, there is a problem.
            Debug.Assert(contractCodeDomInfo.TypeDeclaration != null);

            CodeTypeDeclaration generatedType = contractCodeDomInfo.TypeDeclaration;
            generatedType.BaseTypes.Add(baseTypeReference);
            CodeAttributeDeclaration collectionContractAttribute = new CodeAttributeDeclaration(GetClrTypeFullName(typeof(CollectionDataContractAttribute)));
            collectionContractAttribute.Arguments.Add(new CodeAttributeArgument(Globals.NameProperty, new CodePrimitiveExpression(dataContractName)));
            collectionContractAttribute.Arguments.Add(new CodeAttributeArgument(Globals.NamespaceProperty, new CodePrimitiveExpression(collectionContract.StableName.Namespace)));
            if (collectionContract.IsReference != Globals.DefaultIsReference)
                collectionContractAttribute.Arguments.Add(new CodeAttributeArgument(Globals.IsReferenceProperty, new CodePrimitiveExpression(collectionContract.IsReference)));
            collectionContractAttribute.Arguments.Add(new CodeAttributeArgument(Globals.ItemNameProperty, new CodePrimitiveExpression(GetNameForAttribute(collectionContract.ItemName))));
            if (foundDictionaryBase)
            {
                // These are not null if we are working with a dictionary. See CollectionDataContract.IsDictionary
                Debug.Assert(collectionContract.KeyName != null);
                Debug.Assert(collectionContract.ValueName != null);
                collectionContractAttribute.Arguments.Add(new CodeAttributeArgument(Globals.KeyNameProperty, new CodePrimitiveExpression(GetNameForAttribute(collectionContract.KeyName))));
                collectionContractAttribute.Arguments.Add(new CodeAttributeArgument(Globals.ValueNameProperty, new CodePrimitiveExpression(GetNameForAttribute(collectionContract.ValueName))));
            }
            generatedType.CustomAttributes.Add(collectionContractAttribute);
            AddImportStatement(typeof(CollectionDataContractAttribute).Namespace, contractCodeDomInfo.CodeNamespace);
            AddSerializableAttribute(GenerateSerializableTypes, generatedType, contractCodeDomInfo);
        }

        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
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
                    GetClrTypeFullName(Globals.TypeOfXmlSchemaProviderAttribute),
                    new CodeAttributeArgument(NullReference),
                    new CodeAttributeArgument(Globals.IsAnyProperty, new CodePrimitiveExpression(true)))
                );
            }
            else
            {
                type.CustomAttributes.Add(new CodeAttributeDeclaration(
                    GetClrTypeFullName(Globals.TypeOfXmlSchemaProviderAttribute),
                    new CodeAttributeArgument(new CodePrimitiveExpression(Globals.ExportSchemaMethod)))
                );

                CodeMemberField typeNameField = new CodeMemberField(Globals.TypeOfXmlQualifiedName, TypeNameFieldName);
                typeNameField.Attributes |= MemberAttributes.Static | MemberAttributes.Private;
                XmlQualifiedName typeName = xmlDataContract.IsAnonymous
                    ? XsdDataContractImporter.ImportActualType(xmlDataContract.XsdType?.Annotation, xmlDataContract.StableName, xmlDataContract.StableName)
                    : xmlDataContract.StableName;
                typeNameField.InitExpression = new CodeObjectCreateExpression(Globals.TypeOfXmlQualifiedName, new CodePrimitiveExpression(typeName.Name), new CodePrimitiveExpression(typeName.Namespace));
                type.Members.Add(typeNameField);

                type.Members.Add(GetSchemaStaticMethod);

                bool isElementNameDifferent =
                    (xmlDataContract.TopLevelElementName != null && xmlDataContract.TopLevelElementName.Value != xmlDataContract.StableName.Name) ||
                    (xmlDataContract.TopLevelElementNamespace != null && xmlDataContract.TopLevelElementNamespace.Value != xmlDataContract.StableName.Namespace);
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
                namespaceAttribute.Arguments.Add(new CodeAttributeArgument(Globals.ClrNamespaceProperty, new CodePrimitiveExpression(clrNamespace)));
                _codeCompileUnit.AssemblyCustomAttributes.Add(namespaceAttribute);
            }
            contractCodeDomInfo.CodeNamespace = codeNamespace;
            return codeNamespace;
        }

        private static string GetMemberName(string memberName, ContractCodeDomInfo contractCodeDomInfo)
        {
            memberName = GetClrIdentifier(memberName, Globals.DefaultGeneratedMember);

            // This is only called from Export* methods which have already called GenerateType to fill in this info.
            Debug.Assert(contractCodeDomInfo.TypeDeclaration != null);

            if (memberName == contractCodeDomInfo.TypeDeclaration.Name)
                memberName = AppendToValidClrIdentifier(memberName, Globals.DefaultMemberSuffix);

            if (contractCodeDomInfo.GetMemberNames().Contains(memberName))
            {
                string uniqueMemberName;
                for (int i = 1;; i++)
                {
                    uniqueMemberName = AppendToValidClrIdentifier(memberName, i.ToString(NumberFormatInfo.InvariantInfo));
                    if (!contractCodeDomInfo.GetMemberNames().Contains(uniqueMemberName))
                    {
                        memberName = uniqueMemberName;
                        break;
                    }
                }
            }

            contractCodeDomInfo.GetMemberNames().Add(memberName);
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
                if (!Namespaces.TryGetValue(dataContract.StableName.Namespace, out clrNamespace))
                {
                    if (Namespaces.TryGetValue(WildcardNamespaceMapping, out clrNamespace))
                    {
                        usesWildcardNamespace = true;
                    }
                    else
                    {
                        clrNamespace = GetClrNamespace(dataContract.StableName.Namespace);
                        if (ClrNamespaces.ContainsKey(clrNamespace))
                        {
                            string uniqueNamespace;
                            for (int i = 1;; i++)
                            {
                                uniqueNamespace = ((clrNamespace.Length == 0) ? Globals.DefaultClrNamespace : clrNamespace) + i.ToString(NumberFormatInfo.InvariantInfo);
                                if (!ClrNamespaces.ContainsKey(uniqueNamespace))
                                {
                                    clrNamespace = uniqueNamespace;
                                    break;
                                }
                            }
                        }
                        AddNamespacePair(dataContract.StableName.Namespace, clrNamespace);
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
            if (dataContractNamespace == null || dataContractNamespace.Length == 0)
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
                    if (uriString.StartsWith(Globals.DataContractXsdBaseNamespace, StringComparison.Ordinal))
                        AddToNamespace(builder, uriString.Substring(Globals.DataContractXsdBaseNamespace.Length), fragments);
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
                for (int i = 1;; i++)
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

        [RequiresUnreferencedCode("TODO smolloy - Placeholder")]
        internal static bool IsObjectContract(DataContract dataContract)
        {
            Dictionary<Type, object> previousCollectionTypes = new Dictionary<Type, object>();
            while (dataContract is CollectionDataContract)
            {
                if (dataContract.OriginalUnderlyingType == null)
                {
                    dataContract = ((CollectionDataContract)dataContract).ItemContract;
                    continue;
                }

                if (!previousCollectionTypes.ContainsKey(dataContract.OriginalUnderlyingType))
                {
                    previousCollectionTypes.Add(dataContract.OriginalUnderlyingType, dataContract.OriginalUnderlyingType);
                    dataContract = ((CollectionDataContract)dataContract).ItemContract;
                }
                else
                {
                    break;
                }
            }

            return dataContract is PrimitiveDataContract && ((PrimitiveDataContract)dataContract).UnderlyingType == typeof(object);
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
            get { return new CodePrimitiveExpression(null); }
        }

        private CodeParameterDeclarationExpression SerializationInfoParameter
        {
            get { return new CodeParameterDeclarationExpression(GetCodeTypeReference(typeof(SerializationInfo)), Globals.SerializationInfoFieldName); }
        }

        private CodeParameterDeclarationExpression StreamingContextParameter
        {
            get { return new CodeParameterDeclarationExpression(GetCodeTypeReference(typeof(StreamingContext)), Globals.ContextFieldName); }
        }

        private CodeAttributeDeclaration SerializableAttribute
        {
            get { return new CodeAttributeDeclaration(GetCodeTypeReference(typeof(SerializableAttribute))); }
        }

        private CodeMemberProperty NodeArrayProperty
        {
            get
            {
                return CreateProperty(GetCodeTypeReference(Globals.TypeOfXmlNodeArray), Globals.NodeArrayPropertyName, Globals.NodeArrayFieldName, false/*isValueType*/);
            }
        }

        private CodeMemberField NodeArrayField
        {
            get
            {
                CodeMemberField nodeArrayField = new CodeMemberField();
                nodeArrayField.Type = GetCodeTypeReference(Globals.TypeOfXmlNodeArray);
                nodeArrayField.Name = Globals.NodeArrayFieldName;
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
                setNode.Left = new CodeFieldReferenceExpression(ThisReference, Globals.NodeArrayFieldName);
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
                        new CodePropertyReferenceExpression(ThisReference, Globals.NodeArrayPropertyName)
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
                getSchemaStaticMethod.Name = Globals.ExportSchemaMethod;
                getSchemaStaticMethod.ReturnType = GetCodeTypeReference(Globals.TypeOfXmlQualifiedName);
                CodeParameterDeclarationExpression paramDeclaration = new CodeParameterDeclarationExpression(typeof(XmlSchemaSet), "schemas");
                getSchemaStaticMethod.Parameters.Add(paramDeclaration);
                getSchemaStaticMethod.Attributes = MemberAttributes.Static | MemberAttributes.Public;
                getSchemaStaticMethod.Statements.Add(
                    new CodeMethodInvokeExpression(
                        new CodeTypeReferenceExpression(GetCodeTypeReference(typeof(XmlSerializableServices))),
                        nameof(XmlSerializableServices.AddDefaultSchema),
                        new CodeArgumentReferenceExpression(paramDeclaration.Name),
                        new CodeFieldReferenceExpression(null, TypeNameFieldName)
                    )
                );
                getSchemaStaticMethod.Statements.Add(
                    new CodeMethodReturnStatement(
                        new CodeFieldReferenceExpression(null, TypeNameFieldName)
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
                setObjectData.Left = new CodePropertyReferenceExpression(ThisReference, Globals.SerializationInfoFieldName);
                setObjectData.Right = new CodeArgumentReferenceExpression(Globals.SerializationInfoFieldName);
                baseConstructor.Statements.Add(setObjectData);
                // Special-cased check for vb here since CodeGeneratorOptions does not provide information indicating that VB cannot initialize event member
                if (EnableDataBinding && SupportsDeclareEvents && string.CompareOrdinal(FileExtension, "vb") != 0)
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
                derivedConstructor.BaseConstructorArgs.Add(new CodeVariableReferenceExpression(Globals.SerializationInfoFieldName));
                derivedConstructor.BaseConstructorArgs.Add(new CodeVariableReferenceExpression(Globals.ContextFieldName));
                return derivedConstructor;
            }
        }

        private CodeMemberField SerializationInfoField
        {
            get
            {
                CodeMemberField serializationInfoField = new CodeMemberField();
                serializationInfoField.Type = GetCodeTypeReference(typeof(SerializationInfo));
                serializationInfoField.Name = Globals.SerializationInfoFieldName;
                serializationInfoField.Attributes = MemberAttributes.Private;
                return serializationInfoField;
            }
        }

        private CodeMemberProperty SerializationInfoProperty
        {
            get
            {
                return CreateProperty(GetCodeTypeReference(typeof(SerializationInfo)), Globals.SerializationInfoPropertyName, Globals.SerializationInfoFieldName, false/*isValueType*/);
            }
        }

        private CodeMemberMethod GetObjectDataMethod
        {
            get
            {
                CodeMemberMethod getObjectDataMethod = new CodeMemberMethod();
                getObjectDataMethod.Name = Globals.GetObjectDataMethodName;
                getObjectDataMethod.Parameters.Add(SerializationInfoParameter);
                getObjectDataMethod.Parameters.Add(StreamingContextParameter);
                getObjectDataMethod.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                getObjectDataMethod.ImplementationTypes.Add(typeof(ISerializable));

                // Generates: if (this.SerializationInfo == null) return;
                CodeConditionStatement returnIfNull = new CodeConditionStatement();
                returnIfNull.Condition = new CodeBinaryOperatorExpression(
                    new CodePropertyReferenceExpression(ThisReference, Globals.SerializationInfoPropertyName),
                    CodeBinaryOperatorType.IdentityEquality,
                    NullReference);
                returnIfNull.TrueStatements.Add(new CodeMethodReturnStatement());

                // Generates: SerializationInfoEnumerator enumerator = this.SerializationInfo.GetEnumerator();
                CodeVariableDeclarationStatement getEnumerator = new CodeVariableDeclarationStatement();
                getEnumerator.Type = GetCodeTypeReference(typeof(SerializationInfoEnumerator));
                getEnumerator.Name = Globals.EnumeratorFieldName;
                getEnumerator.InitExpression = new CodeMethodInvokeExpression(
                    new CodePropertyReferenceExpression(ThisReference, Globals.SerializationInfoPropertyName),
                    Globals.GetEnumeratorMethodName);

                //Generates: SerializationEntry entry = enumerator.Current;
                CodeVariableDeclarationStatement getCurrent = new CodeVariableDeclarationStatement();
                getCurrent.Type = GetCodeTypeReference(typeof(SerializationEntry));
                getCurrent.Name = Globals.SerializationEntryFieldName;
                getCurrent.InitExpression = new CodePropertyReferenceExpression(
                    new CodeVariableReferenceExpression(Globals.EnumeratorFieldName),
                    Globals.CurrentPropertyName);

                //Generates: info.AddValue(entry.Name, entry.Value);
                CodeExpressionStatement addValue = new CodeExpressionStatement();
                CodePropertyReferenceExpression getCurrentName = new CodePropertyReferenceExpression(
                    new CodeVariableReferenceExpression(Globals.SerializationEntryFieldName),
                    Globals.NameProperty);
                CodePropertyReferenceExpression getCurrentValue = new CodePropertyReferenceExpression(
                    new CodeVariableReferenceExpression(Globals.SerializationEntryFieldName),
                    Globals.ValueProperty);
                addValue.Expression = new CodeMethodInvokeExpression(
                    new CodeArgumentReferenceExpression(Globals.SerializationInfoFieldName),
                    Globals.AddValueMethodName,
                    new CodeExpression[] { getCurrentName, getCurrentValue });

                //Generates: for (; enumerator.MoveNext(); )
                CodeIterationStatement loop = new CodeIterationStatement();
                loop.TestExpression = new CodeMethodInvokeExpression(
                    new CodeVariableReferenceExpression(Globals.EnumeratorFieldName),
                    Globals.MoveNextMethodName);
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
                extensionDataObjectField.Name = Globals.ExtensionDataObjectFieldName;
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
                extensionDataObjectProperty.Name = Globals.ExtensionDataObjectPropertyName;
                extensionDataObjectProperty.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                extensionDataObjectProperty.ImplementationTypes.Add(typeof(IExtensibleDataObject));

                CodeMethodReturnStatement propertyGet = new CodeMethodReturnStatement();
                propertyGet.Expression = new CodeFieldReferenceExpression(ThisReference, Globals.ExtensionDataObjectFieldName);
                extensionDataObjectProperty.GetStatements.Add(propertyGet);

                CodeAssignStatement propertySet = new CodeAssignStatement();
                propertySet.Left = new CodeFieldReferenceExpression(ThisReference, Globals.ExtensionDataObjectFieldName);
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
