// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Globalization;
using System.Reflection;
using System.Threading;
using Microsoft.Internal;
using Microsoft.Internal.Collections;
using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel.Composition.ReflectionModel
{
    internal sealed class GenericSpecializationPartCreationInfo : IReflectionPartCreationInfo
    {
        private readonly IReflectionPartCreationInfo _originalPartCreationInfo;
        private readonly ReflectionComposablePartDefinition _originalPart;
        private readonly Type[] _specialization;
        private readonly string[] _specializationIdentities;
        private IEnumerable<ExportDefinition>? _exports;
        private IEnumerable<ImportDefinition>? _imports;
        private readonly Lazy<Type> _lazyPartType;
        private List<LazyMemberInfo>? _members;
        private List<Lazy<ParameterInfo>>? _parameters;
        private Dictionary<LazyMemberInfo, MemberInfo[]>? _membersTable;
        private Dictionary<Lazy<ParameterInfo>, ParameterInfo>? _parametersTable;
        private ConstructorInfo? _constructor;
        private readonly object _lock = new object();

        public GenericSpecializationPartCreationInfo(IReflectionPartCreationInfo originalPartCreationInfo, ReflectionComposablePartDefinition originalPart, Type[] specialization)
        {
            if (originalPartCreationInfo == null)
            {
                throw new ArgumentNullException(nameof(originalPartCreationInfo));
            }

            if (originalPart == null)
            {
                throw new ArgumentNullException(nameof(originalPart));
            }

            if (specialization == null)
            {
                throw new ArgumentNullException(nameof(specialization));
            }

            _originalPartCreationInfo = originalPartCreationInfo;
            _originalPart = originalPart;
            _specialization = specialization;
            _specializationIdentities = new string[_specialization.Length];
            for (int i = 0; i < _specialization.Length; i++)
            {
                _specializationIdentities[i] = AttributedModelServices.GetTypeIdentity(_specialization[i]);
            }
            _lazyPartType = new Lazy<Type>(
                () => _originalPartCreationInfo.GetPartType().MakeGenericType(specialization),
                LazyThreadSafetyMode.PublicationOnly);

        }

        public ReflectionComposablePartDefinition OriginalPart
        {
            get
            {
                return _originalPart;
            }
        }

        public Type GetPartType()
        {
            return _lazyPartType.Value;
        }

        public Lazy<Type> GetLazyPartType()
        {
            return _lazyPartType;
        }

        public ConstructorInfo? GetConstructor()
        {
            if (_constructor == null)
            {
                ConstructorInfo? genericConstuctor = _originalPartCreationInfo.GetConstructor();
                ConstructorInfo? result = null;
                if (genericConstuctor != null)
                {
                    foreach (ConstructorInfo constructor in GetPartType().GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    {
                        if (constructor.MetadataToken == genericConstuctor.MetadataToken)
                        {
                            result = constructor;
                            break;
                        }
                    }
                }

                Thread.MemoryBarrier();
                lock (_lock)
                {
                    if (_constructor == null)
                    {
                        _constructor = result;
                    }
                }
            }

            return _constructor;
        }

        public IDictionary<string, object?> GetMetadata()
        {
            var originalMetadata = new Dictionary<string, object?>(_originalPartCreationInfo.GetMetadata()!, StringComparers.MetadataKeyNames);
            originalMetadata.Remove(CompositionConstants.IsGenericPartMetadataName);
            originalMetadata.Remove(CompositionConstants.GenericPartArityMetadataName);
            originalMetadata.Remove(CompositionConstants.GenericParameterConstraintsMetadataName);
            originalMetadata.Remove(CompositionConstants.GenericParameterAttributesMetadataName);

            return originalMetadata;
        }

        private MemberInfo[] GetAccessors(LazyMemberInfo originalLazyMember)
        {
            BuildTables();
            if (_membersTable == null)
            {
                throw new ArgumentNullException(nameof(_membersTable));
            }

            return _membersTable[originalLazyMember];
        }

        private ParameterInfo GetParameter(Lazy<ParameterInfo> originalParameter)
        {
            BuildTables();
            if (_parametersTable == null)
            {
                throw new ArgumentNullException(nameof(_parametersTable));
            }

            return _parametersTable[originalParameter];
        }

        private void BuildTables()
        {
            if (_membersTable != null)
            {
                return;
            }

            PopulateImportsAndExports();

            List<LazyMemberInfo>? members = null;
            List<Lazy<ParameterInfo>>? parameters = null;
            lock (_lock)
            {
                if (_membersTable == null)
                {
                    members = _members;
                    parameters = _parameters;

                    if (members == null)
                    {
                        throw new Exception(SR.Diagnostic_InternalExceptionMessage);
                    }
                }
            }

            //
            // Get all members that can be of interest and extract their MetadataTokens
            //
            Dictionary<LazyMemberInfo, MemberInfo[]> membersTable = BuildMembersTable(members!);
            Dictionary<Lazy<ParameterInfo>, ParameterInfo>? parametersTable = BuildParametersTable(parameters);

            lock (_lock)
            {
                if (_membersTable == null)
                {
                    _membersTable = membersTable;
                    _parametersTable = parametersTable;

                    Thread.MemoryBarrier();

                    _parameters = null;
                    _members = null;
                }
            }
        }

        private Dictionary<LazyMemberInfo, MemberInfo[]> BuildMembersTable(List<LazyMemberInfo> members)
        {
            if (members == null)
            {
                throw new ArgumentNullException(nameof(members));
            }

            Dictionary<LazyMemberInfo, MemberInfo[]> membersTable = new Dictionary<LazyMemberInfo, MemberInfo[]>();
            Dictionary<int, MemberInfo> specializedPartMembers = new Dictionary<int, MemberInfo>();

            Type closedGenericPartType = GetPartType();

            specializedPartMembers[closedGenericPartType.MetadataToken] = closedGenericPartType;
            foreach (MethodInfo method in closedGenericPartType.GetAllMethods())
            {
                specializedPartMembers[method.MetadataToken] = method;
            }

            foreach (FieldInfo field in closedGenericPartType.GetAllFields())
            {
                specializedPartMembers[field.MetadataToken] = field;
            }

            foreach (var iface in closedGenericPartType.GetInterfaces())
            {
                specializedPartMembers[iface.MetadataToken] = iface;
            }

            foreach (var type in closedGenericPartType.GetNestedTypes())
            {
                specializedPartMembers[type.MetadataToken] = type;
            }

            //Walk the base class list
            var baseType = closedGenericPartType.BaseType;
            while (baseType != null && baseType != typeof(object))
            {
                specializedPartMembers[baseType.MetadataToken] = baseType;
                baseType = baseType.BaseType;
            }

            //
            // Now go through the members table and resolve them into the new closed type based on the tokens
            //
            foreach (LazyMemberInfo lazyMemberInfo in members)
            {
                MemberInfo[] genericAccessors = lazyMemberInfo.GetAccessors();
                MemberInfo[] accessors = new MemberInfo[genericAccessors.Length];

                for (int i = 0; i < genericAccessors.Length; i++)
                {
                    if (genericAccessors[i] != null)
                    {
                        specializedPartMembers.TryGetValue(genericAccessors[i].MetadataToken, out accessors[i]!);
                        if (accessors[i] == null)
                        {
                            throw new Exception(SR.Diagnostic_InternalExceptionMessage);
                        }
                    }
                }

                membersTable[lazyMemberInfo] = accessors;
            }

            return membersTable;
        }

        [return: NotNullIfNotNull("parameters")]
        private Dictionary<Lazy<ParameterInfo>, ParameterInfo>? BuildParametersTable(List<Lazy<ParameterInfo>>? parameters)
        {
            if (parameters != null)
            {
                Dictionary<Lazy<ParameterInfo>, ParameterInfo> parametersTable = new Dictionary<Lazy<ParameterInfo>, ParameterInfo>();
                // GENTODO - error case
                ParameterInfo[] constructorParameters = GetConstructor()!.GetParameters();
                foreach (var lazyParameter in parameters)
                {
                    parametersTable[lazyParameter] = constructorParameters[lazyParameter.Value.Position];
                }
                return parametersTable;
            }
            else
            {
                return null;
            }

        }

        private List<ImportDefinition> PopulateImports(List<LazyMemberInfo> members, List<Lazy<ParameterInfo>> parameters)
        {
            List<ImportDefinition> imports = new List<ImportDefinition>();

            foreach (ImportDefinition originalImport in _originalPartCreationInfo.GetImports())
            {
                ReflectionImportDefinition? reflectionImport = originalImport as ReflectionImportDefinition;
                if (reflectionImport == null)
                {
                    // we always ignore these
                    continue;
                }

                imports.Add(TranslateImport(reflectionImport, members, parameters));
            }

            return imports;
        }

        private ImportDefinition TranslateImport(ReflectionImportDefinition reflectionImport, List<LazyMemberInfo> members, List<Lazy<ParameterInfo>> parameters)
        {
            bool isExportFactory = false;
            ContractBasedImportDefinition productImport = reflectionImport;

            if (reflectionImport is IPartCreatorImportDefinition exportFactoryImportDefinition)
            {
                productImport = exportFactoryImportDefinition.ProductImportDefinition;
                isExportFactory = true;
            }

            string contractName = Translate(productImport.ContractName);
            string requiredTypeIdentity = Translate(productImport.RequiredTypeIdentity!);
            IDictionary<string, object?> metadata = TranslateImportMetadata(productImport);

            ImportDefinition? import = null;
            if (reflectionImport is ReflectionMemberImportDefinition memberImport)
            {
                LazyMemberInfo lazyMember = memberImport.ImportingLazyMember;
                LazyMemberInfo importingMember = new LazyMemberInfo(lazyMember.MemberType, () => GetAccessors(lazyMember));

                if (isExportFactory)
                {
                    import = new PartCreatorMemberImportDefinition(
                        importingMember,
                        ((ICompositionElement)memberImport).Origin,
                        new ContractBasedImportDefinition(
                            contractName,
                            requiredTypeIdentity,
                            productImport.RequiredMetadata,
                            productImport.Cardinality,
                            productImport.IsRecomposable,
                            false,
                            CreationPolicy.NonShared,
                            metadata));
                }
                else
                {
                    import = new ReflectionMemberImportDefinition(
                         importingMember,
                         contractName,
                         requiredTypeIdentity,
                         productImport.RequiredMetadata,
                         productImport.Cardinality,
                         productImport.IsRecomposable,
                         false,
                         productImport.RequiredCreationPolicy,
                         metadata,
                         ((ICompositionElement)memberImport).Origin);
                }

                members.Add(lazyMember);
            }
            else
            {
                ReflectionParameterImportDefinition? parameterImport = reflectionImport as ReflectionParameterImportDefinition;
                if (parameterImport == null)
                {
                    throw new Exception(SR.Diagnostic_InternalExceptionMessage);
                }

                Lazy<ParameterInfo> lazyParameter = parameterImport.ImportingLazyParameter;
                Lazy<ParameterInfo> parameter = new Lazy<ParameterInfo>(() => GetParameter(lazyParameter));

                if (isExportFactory)
                {
                    import = new PartCreatorParameterImportDefinition(
                            parameter,
                            ((ICompositionElement)parameterImport).Origin,
                            new ContractBasedImportDefinition(
                                contractName,
                                requiredTypeIdentity,
                                productImport.RequiredMetadata,
                                productImport.Cardinality,
                                false,
                                true,
                                CreationPolicy.NonShared,
                                metadata));
                }
                else
                {
                    import = new ReflectionParameterImportDefinition(
                         parameter,
                         contractName,
                         requiredTypeIdentity,
                         productImport.RequiredMetadata,
                         productImport.Cardinality,
                         productImport.RequiredCreationPolicy,
                         metadata,
                         ((ICompositionElement)parameterImport).Origin);
                }

                parameters.Add(lazyParameter);
            }

            return import;
        }

        private List<ExportDefinition> PopulateExports(List<LazyMemberInfo> members)
        {
            List<ExportDefinition> exports = new List<ExportDefinition>();

            foreach (ExportDefinition originalExport in _originalPartCreationInfo.GetExports())
            {
                ReflectionMemberExportDefinition? reflectionExport = originalExport as ReflectionMemberExportDefinition;
                if (reflectionExport == null)
                {
                    // we always ignore these
                    continue;
                }

                exports.Add(TranslateExpot(reflectionExport, members));
            }

            return exports;
        }

        public ExportDefinition TranslateExpot(ReflectionMemberExportDefinition reflectionExport, List<LazyMemberInfo> members)
        {
            ExportDefinition? export = null;
            LazyMemberInfo lazyMember = reflectionExport.ExportingLazyMember;
            var capturedLazyMember = lazyMember;
            var capturedReflectionExport = reflectionExport;

            string contractName = Translate(reflectionExport.ContractName, reflectionExport.Metadata.GetValue<int[]>(CompositionConstants.GenericExportParametersOrderMetadataName));

            LazyMemberInfo exportingMember = new LazyMemberInfo(capturedLazyMember.MemberType, () => GetAccessors(capturedLazyMember));
            Lazy<IDictionary<string, object?>> lazyMetadata = new Lazy<IDictionary<string, object?>>(() => TranslateExportMetadata(capturedReflectionExport));

            export = new ReflectionMemberExportDefinition(
                            exportingMember,
                            new LazyExportDefinition(contractName, lazyMetadata),
                            ((ICompositionElement)reflectionExport).Origin);

            members.Add(capturedLazyMember);

            return export;
        }

        private string Translate(string originalValue, int[]? genericParametersOrder)
        {
            if (genericParametersOrder != null)
            {
                string[] specializationIdentities = GenericServices.Reorder(_specializationIdentities, genericParametersOrder);
                return string.Format(CultureInfo.InvariantCulture, originalValue, specializationIdentities);
            }
            else
            {
                return Translate(originalValue);
            }
        }

        private string Translate(string originalValue)
        {
            return string.Format(CultureInfo.InvariantCulture, originalValue, _specializationIdentities);
        }

        private IDictionary<string, object?> TranslateImportMetadata(ContractBasedImportDefinition originalImport)
        {
            int[]? importParametersOrder = originalImport.Metadata.GetValue<int[]>(CompositionConstants.GenericImportParametersOrderMetadataName);
            if (importParametersOrder != null)
            {
                Dictionary<string, object?> metadata = new Dictionary<string, object?>(originalImport.Metadata, StringComparers.MetadataKeyNames);

                // Get the newly re-qualified name of the generic contract and the subset of applicable types from the specialization
                metadata[CompositionConstants.GenericContractMetadataName] = GenericServices.GetGenericName(originalImport.ContractName, importParametersOrder, _specialization.Length);
                metadata[CompositionConstants.GenericParametersMetadataName] = GenericServices.Reorder(_specialization, importParametersOrder);
                metadata.Remove(CompositionConstants.GenericImportParametersOrderMetadataName);

                return metadata.AsReadOnly();
            }
            else
            {
                return originalImport.Metadata;
            }
        }

        private IDictionary<string, object?> TranslateExportMetadata(ReflectionMemberExportDefinition originalExport)
        {
            Dictionary<string, object?> metadata = new Dictionary<string, object?>(originalExport.Metadata, StringComparers.MetadataKeyNames);

            string? exportTypeIdentity = originalExport.Metadata.GetValue<string>(CompositionConstants.ExportTypeIdentityMetadataName);
            if (!string.IsNullOrEmpty(exportTypeIdentity))
            {
                metadata[CompositionConstants.ExportTypeIdentityMetadataName] = Translate(exportTypeIdentity, originalExport.Metadata.GetValue<int[]>(CompositionConstants.GenericExportParametersOrderMetadataName));
            }
            metadata.Remove(CompositionConstants.GenericExportParametersOrderMetadataName);

            return metadata;
        }

        private void PopulateImportsAndExports()
        {
            if ((_exports == null) || (_imports == null))
            {
                List<LazyMemberInfo> members = new List<LazyMemberInfo>();
                List<Lazy<ParameterInfo>> parameters = new List<Lazy<ParameterInfo>>();

                // we are very careful to not call any 3rd party code in either of these
                var exports = PopulateExports(members);
                var imports = PopulateImports(members, parameters);
                Thread.MemoryBarrier();

                lock (_lock)
                {
                    if ((_exports == null) || (_imports == null))
                    {
                        _members = members;
                        if (parameters.Count > 0)
                        {
                            _parameters = parameters;
                        }

                        _exports = exports;
                        _imports = imports;
                    }
                }
            }
        }

        public IEnumerable<ExportDefinition> GetExports()
        {
            PopulateImportsAndExports();
            return _exports!;
        }

        public IEnumerable<ImportDefinition> GetImports()
        {
            PopulateImportsAndExports();
            return _imports!;
        }

        public bool IsDisposalRequired
        {
            get { return _originalPartCreationInfo.IsDisposalRequired; }
        }

        public bool IsIdentityComparison
        {
            get
            {
                return false;
            }
        }

        public string DisplayName
        {
            get { return Translate(_originalPartCreationInfo.DisplayName); }
        }

        public ICompositionElement? Origin
        {
            get { return _originalPartCreationInfo.Origin; }
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is GenericSpecializationPartCreationInfo that && (_originalPartCreationInfo.Equals(that._originalPartCreationInfo)) &&
                (_specialization.IsArrayEqual(that._specialization));
        }

        public override int GetHashCode()
        {
            return _originalPartCreationInfo.GetHashCode();
        }

        public static bool CanSpecialize(IDictionary<string, object?> partMetadata, Type[] specialization)
        {
            int partArity = partMetadata.GetValue<int>(CompositionConstants.GenericPartArityMetadataName);

            if (partArity != specialization.Length)
            {
                return false;
            }

            object[]? genericParameterConstraints = partMetadata.GetValue<object[]>(CompositionConstants.GenericParameterConstraintsMetadataName);
            GenericParameterAttributes[]? genericParameterAttributes = partMetadata.GetValue<GenericParameterAttributes[]>(CompositionConstants.GenericParameterAttributesMetadataName);

            // if no constraints and attributes been specifed, anything can be created
            if ((genericParameterConstraints == null) && (genericParameterAttributes == null))
            {
                return true;
            }

            if ((genericParameterConstraints != null) && (genericParameterConstraints.Length != partArity))
            {
                return false;
            }

            if ((genericParameterAttributes != null) && (genericParameterAttributes.Length != partArity))
            {
                return false;
            }

            for (int i = 0; i < partArity; i++)
            {
                if (!GenericServices.CanSpecialize(
                    specialization[i],
                    (genericParameterConstraints![i] as Type[]).CreateTypeSpecializations(specialization),
                    genericParameterAttributes![i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
