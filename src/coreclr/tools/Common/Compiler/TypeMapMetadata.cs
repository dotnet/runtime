// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using ILCompiler.DependencyAnalysis;
using Internal.Text;
using Internal.IL;
using Internal.IL.Stubs;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    public sealed class TypeMapMetadata
    {
        private enum TypeMapAttributeKind
        {
            None,
            TypeMapAssemblyTarget,
            TypeMapTypeTarget,
            TypeMap,
            TypeMapAssociation
        }

        // The place that the type map attributes are read from: an assembly, or a type that holds type map entries.
        private readonly record struct ScanSource(EcmaModule Module, EntityHandle Handle)
        {
            public static ScanSource ForAssembly(EcmaAssembly assembly) => new ScanSource(assembly, EntityHandle.AssemblyDefinition);

            public static ScanSource ForType(EcmaType type) => new ScanSource(type.Module, type.Handle);
        }

        private readonly struct TypeWithSerializedName
        {
            public TypeWithSerializedName(TypeDesc type, string serializedName)
            {
                Type = type;
                SerializedName = serializedName;
            }

            public TypeDesc Type { get; }
            public string SerializedName { get; }
        }

        private readonly struct TypeMapCustomAttributeTypeProvider : ICustomAttributeTypeProvider<TypeWithSerializedName>
        {
            private readonly CustomAttributeTypeProvider _provider;

            public TypeMapCustomAttributeTypeProvider(EcmaModule module)
            {
                _provider = new CustomAttributeTypeProvider(module);
            }

            public TypeWithSerializedName GetPrimitiveType(PrimitiveTypeCode typeCode)
                => new TypeWithSerializedName(_provider.GetPrimitiveType(typeCode), null);

            public TypeWithSerializedName GetSystemType()
                => new TypeWithSerializedName(_provider.GetSystemType(), null);

            public TypeWithSerializedName GetSZArrayType(TypeWithSerializedName elementType)
                => new TypeWithSerializedName(_provider.GetSZArrayType(elementType.Type), null);

            public TypeWithSerializedName GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
                => new TypeWithSerializedName(_provider.GetTypeFromDefinition(reader, handle, rawTypeKind), null);

            public TypeWithSerializedName GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
                => new TypeWithSerializedName(_provider.GetTypeFromReference(reader, handle, rawTypeKind), null);

            public TypeWithSerializedName GetTypeFromSpecification(MetadataReader reader, TypeSpecificationHandle handle, byte rawTypeKind)
                => new TypeWithSerializedName(_provider.GetTypeFromSpecification(reader, handle, rawTypeKind), null);

            public TypeWithSerializedName GetTypeFromSerializedName(string name)
                => new TypeWithSerializedName(_provider.GetTypeFromSerializedName(name), name);

            public PrimitiveTypeCode GetUnderlyingEnumType(TypeWithSerializedName type)
                => _provider.GetUnderlyingEnumType(type.Type);

            public bool IsSystemType(TypeWithSerializedName type)
                => _provider.IsSystemType(type.Type);
        }

        internal readonly struct ExternalTypeMapEntry
        {
            public ExternalTypeMapEntry(
                TypeDesc type,
                string serializedTypeName,
                TypeDesc trimmingType,
                string serializedTrimmingTypeName,
                ModuleDesc declaringModule)
            {
                Type = type;
                SerializedTypeName = serializedTypeName;
                TrimmingType = trimmingType;
                SerializedTrimmingTypeName = serializedTrimmingTypeName;
                DeclaringModule = declaringModule;
            }

            public TypeDesc Type { get; }
            public string SerializedTypeName { get; }
            public TypeDesc TrimmingType { get; }
            public string SerializedTrimmingTypeName { get; }
            public ModuleDesc DeclaringModule { get; }
        }

        internal readonly struct ProxyTypeMapEntry
        {
            public ProxyTypeMapEntry(TypeDesc type, string serializedSourceTypeName, string serializedTypeName, ModuleDesc declaringModule)
            {
                Type = type;
                SerializedSourceTypeName = serializedSourceTypeName;
                SerializedTypeName = serializedTypeName;
                DeclaringModule = declaringModule;
            }

            public TypeDesc Type { get; }
            public string SerializedSourceTypeName { get; }
            public string SerializedTypeName { get; }
            public ModuleDesc DeclaringModule { get; }
        }

        private static TypeMapAttributeKind LookupTypeMapType(TypeDesc attrType)
        {
            var typeDef = attrType.GetTypeDefinition() as MetadataType;
            if (typeDef != null && typeDef.Namespace == "System.Runtime.InteropServices"u8)
            {
                if (typeDef.Name == "TypeMapAssemblyTargetAttribute`1"u8)
                    return TypeMapAttributeKind.TypeMapAssemblyTarget;
                else if (typeDef.Name == "TypeMapTypeTargetAttribute`1"u8)
                    return TypeMapAttributeKind.TypeMapTypeTarget;
                else if (typeDef.Name == "TypeMapAttribute`1"u8)
                    return TypeMapAttributeKind.TypeMap;
                else if (typeDef.Name == "TypeMapAssociationAttribute`1"u8)
                    return TypeMapAttributeKind.TypeMapAssociation;
            }
            return TypeMapAttributeKind.None;
        }

        internal interface IExternalTypeMap
        {
            IReadOnlyDictionary<string, ExternalTypeMapEntry> TypeMap { get; }
            MethodDesc ThrowingMethodStub { get; }
        }

        internal interface IProxyTypeMap
        {
            IReadOnlyDictionary<TypeDesc, ProxyTypeMapEntry> TypeMap { get; }
            MethodDesc ThrowingMethodStub { get; }
        }

        internal sealed class Map : IExternalTypeMap, IProxyTypeMap
        {
            private sealed class ThrowingMethodStub : ILStubMethod
            {
                private readonly TypeDesc _typeMapGroup;
                private readonly byte[] _name;

                public ThrowingMethodStub(TypeDesc owningType, TypeDesc typeMapGroup, string owner, bool externalTypeMap, TypeSystemException ex)
                {
                    OwningType = owningType;
                    _typeMapGroup = typeMapGroup;
                    // The map that starts at an owner type has stubs of its own: they are different methods than the ones of the map of the group.
                    _name = System.Text.Encoding.UTF8.GetBytes($"InvalidTypeMapStub_{_typeMapGroup}{(owner is null ? "" : "_Owner_" + owner)}_{(externalTypeMap ? "External" : "Proxy")}");
                    Exception = ex;
                }

                public TypeSystemException Exception { get; }
                public override Utf8Span Name => _name;
                public override MethodIL EmitIL()
                {
#if READYTORUN
                    throw new UnreachableException();
#else
                    return TypeSystemThrowingILEmitter.EmitIL(this, Exception);
#endif
                }

                protected override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
                {
                    return Name.AsSpan().SequenceCompareTo(other.Name.AsSpan());
                }

                public override bool IsPInvoke => false;

                public override string DiagnosticName => GetName();

                protected override int ClassCode => 1744789196;

                public override TypeDesc OwningType { get; }

                public override MethodSignature Signature => new MethodSignature(MethodSignatureFlags.Static, 0, Context.GetWellKnownType(WellKnownType.Void), []);

                public override TypeSystemContext Context => OwningType.Context;
            }

            private readonly Dictionary<TypeDesc, ProxyTypeMapEntry> _associatedTypeMap = [];
            private readonly Dictionary<string, ExternalTypeMapEntry> _externalTypeMap = [];
            private readonly List<ModuleDesc> _targetModules = [];
            private readonly List<EcmaType> _targetTypes = [];
            private ThrowingMethodStub _externalTypeMapExceptionStub;
            private ThrowingMethodStub _associatedTypeMapExceptionStub;

            // The type that the map starts at, for the maps that start at an owner type.
            private readonly string _owner;

            public Map(TypeDesc typeMapGroup, string owner = null)
            {
                TypeMapGroup = typeMapGroup;
                _owner = owner;
            }

            public TypeDesc TypeMapGroup { get; }

            /// <summary>
            /// Indicates whether any TypeMapAssemblyTarget attributes were processed for this group,
            /// regardless of whether the target assembly was successfully resolved. When true and
            /// <see cref="TargetModules"/> is empty, it means all target attributes failed to resolve
            /// and the runtime should fall back to attribute processing.
            /// </summary>
            public bool HasAssemblyTargetAttributes { get; set; }

            public void AddAssociatedTypeMapEntry(
                TypeDesc type,
                string serializedTypeName,
                TypeDesc associatedType,
                string serializedAssociatedTypeName,
                ModuleDesc declaringModule)
            {
                if (!_associatedTypeMap.TryAdd(
                    type,
                    new ProxyTypeMapEntry(associatedType, serializedTypeName, serializedAssociatedTypeName, declaringModule)))
                {
                    ThrowHelper.ThrowBadImageFormatException();
                }
            }
            public void AddExternalTypeMapEntry(
                string typeName,
                TypeDesc type,
                string serializedTypeName,
                TypeDesc trimmingTarget,
                string serializedTrimmingTypeName,
                ModuleDesc declaringModule)
            {
                if (!_externalTypeMap.TryAdd(
                    typeName,
                    new ExternalTypeMapEntry(type, serializedTypeName, trimmingTarget, serializedTrimmingTypeName, declaringModule)))
                {
                    ThrowHelper.ThrowBadImageFormatException();
                }
            }

            public void SetExternalTypeMapException(ModuleDesc stubModule, TypeSystemException exception)
            {
                if (_externalTypeMapExceptionStub?.Exception is TypeSystemException.FileNotFoundException)
                {
                    // FileNotFound exception takes precedence.
                    return;
                }
                _externalTypeMapExceptionStub ??= new ThrowingMethodStub(stubModule.GetGlobalModuleType(), TypeMapGroup, _owner, externalTypeMap: true, exception);
            }

            public void SetAssociatedTypeMapException(ModuleDesc stubModule, TypeSystemException exception)
            {
                if (_associatedTypeMapExceptionStub?.Exception is TypeSystemException.FileNotFoundException)
                {
                    // FileNotFound exception takes precedence.
                    return;
                }
                _associatedTypeMapExceptionStub ??= new ThrowingMethodStub(stubModule.GetGlobalModuleType(), TypeMapGroup, _owner, externalTypeMap: false, exception);
            }

            public void MergePendingMap(ModuleDesc stubModule, Map pendingMap)
            {
                // Don't waste time adding entries from the pending map if we already have an exception stub,
                // as the exception stub means the map is invalid and the entries won't be used anyway.
                if (_associatedTypeMapExceptionStub is null)
                {
                    if (pendingMap._associatedTypeMapExceptionStub is not null)
                    {
                        _associatedTypeMapExceptionStub = pendingMap._associatedTypeMapExceptionStub;
                    }
                    else
                    {
                        try
                        {
                            foreach (KeyValuePair<TypeDesc, ProxyTypeMapEntry> kvp in pendingMap._associatedTypeMap)
                            {
                                AddAssociatedTypeMapEntry(
                                    kvp.Key,
                                    kvp.Value.SerializedSourceTypeName,
                                    kvp.Value.Type,
                                    kvp.Value.SerializedTypeName,
                                    kvp.Value.DeclaringModule);
                            }
                        }
                        catch (TypeSystemException ex)
                        {
                            SetAssociatedTypeMapException(stubModule, ex);
                        }
                    }
                }
                else if (_associatedTypeMapExceptionStub.Exception is not TypeSystemException.FileNotFoundException &&
                         pendingMap._associatedTypeMapExceptionStub?.Exception is TypeSystemException.FileNotFoundException)
                {
                    // If the pending map has a FileNotFoundException, it takes precedence over our existing exception stub, so use it instead.
                    _associatedTypeMapExceptionStub = pendingMap._associatedTypeMapExceptionStub;
                }

                // Don't waste time adding entries from the pending map if we already have an exception stub,
                // as the exception stub means the map is invalid and the entries won't be used anyway.
                if (_externalTypeMapExceptionStub is null)
                {
                    if (pendingMap._externalTypeMapExceptionStub is not null)
                    {
                        _externalTypeMapExceptionStub = pendingMap._externalTypeMapExceptionStub;
                    }
                    else
                    {
                        try
                        {
                            foreach (KeyValuePair<string, ExternalTypeMapEntry> kvp in pendingMap._externalTypeMap)
                            {
                                AddExternalTypeMapEntry(
                                    kvp.Key,
                                    kvp.Value.Type,
                                    kvp.Value.SerializedTypeName,
                                    kvp.Value.TrimmingType,
                                    kvp.Value.SerializedTrimmingTypeName,
                                    kvp.Value.DeclaringModule);
                            }
                        }
                        catch (TypeSystemException ex)
                        {
                            SetExternalTypeMapException(stubModule, ex);
                        }
                    }
                }
                else if (_externalTypeMapExceptionStub.Exception is not TypeSystemException.FileNotFoundException &&
                         pendingMap._externalTypeMapExceptionStub?.Exception is TypeSystemException.FileNotFoundException)
                {
                    // If the pending map has a FileNotFoundException, it takes precedence over our existing exception stub, so use it instead.
                    _externalTypeMapExceptionStub = pendingMap._externalTypeMapExceptionStub;
                }

                _targetModules.AddRange(pendingMap._targetModules);
                _targetTypes.AddRange(pendingMap._targetTypes);
                HasAssemblyTargetAttributes |= pendingMap.HasAssemblyTargetAttributes;
            }

            public void AddTargetModule(ModuleDesc targetModule)
            {
                _targetModules.Add(targetModule);
            }

            public void AddTargetType(EcmaType targetType)
            {
                _targetTypes.Add(targetType);
            }

            /// <summary>
            /// The types that hold entries and were targeted with TypeMapTypeTarget attributes for this type map group. Like <see cref="TargetModules"/>, these are
            /// only used to find the targets of a pending map: the entries of the types are added to the map when the types are traversed.
            /// </summary>
            internal IReadOnlyList<EcmaType> TargetTypes => _targetTypes;

            /// <summary>
            /// The modules targeted with TypeMapAssemblyTarget attributes for this type map group. This is only populated when TypeMapMetadata is created with TypeMapAssemblyTargetsMode.Record. When TypeMapMetadata is created with TypeMapAssemblyTargetsMode.Traverse, this will be empty as the target assemblies will be traversed to include their type maps instead of just being recorded as targets.
            /// </summary>
            public IReadOnlyList<ModuleDesc> TargetModules => _targetModules;

            IReadOnlyDictionary<string, ExternalTypeMapEntry> IExternalTypeMap.TypeMap => _externalTypeMap;

            MethodDesc IExternalTypeMap.ThrowingMethodStub => _externalTypeMapExceptionStub;

            IReadOnlyDictionary<TypeDesc, ProxyTypeMapEntry> IProxyTypeMap.TypeMap => _associatedTypeMap;

            MethodDesc IProxyTypeMap.ThrowingMethodStub => _associatedTypeMapExceptionStub;
        }

        public static readonly TypeMapMetadata Empty = new TypeMapMetadata(new Dictionary<TypeDesc, Map>(), "No type maps", null);

        private readonly IReadOnlyDictionary<TypeDesc, Map> _states;

        private TypeMapMetadata(IReadOnlyDictionary<TypeDesc, Map> states, string diagnosticName, ModuleDesc associatedModule)
        {
            _states = states;
            DiagnosticName = diagnosticName;
            AssociatedModule = associatedModule;
        }

        public bool IsEmpty => _states.Count == 0;

        internal IEnumerable<KeyValuePair<TypeDesc, Map>> Maps => _states;

        public string DiagnosticName { get; }

        public ModuleDesc AssociatedModule { get; }

        public static TypeMapMetadata CreateFromAssembly(EcmaAssembly assembly, ModuleDesc throwHelperEmitModule, TypeMapAssemblyTargetsMode assemblyTargetsMode)
        {
            Dictionary<TypeDesc, Map> typeMapStates = Scan(ScanSource.ForAssembly(assembly), null, null, throwHelperEmitModule, assemblyTargetsMode);
            return new TypeMapMetadata(typeMapStates, $"Type maps rooted at {assembly}", assembly);
        }

        /// <summary>
        /// Creates the type map of a group that starts at an owner type: the entries declared on the type and on the types reachable from it
        /// through TypeMapTypeTarget attributes. The assembly level entries and the other assemblies are not considered.
        /// </summary>
        internal static Map CreateFromOwnerType(EcmaType ownerType, TypeDesc typeMapGroup, ModuleDesc throwHelperEmitModule)
        {
            string owner = ownerType.ToString();
            Dictionary<TypeDesc, Map> typeMapStates = Scan(ScanSource.ForType(ownerType), typeMapGroup, owner, throwHelperEmitModule, TypeMapAssemblyTargetsMode.Traverse);
            return typeMapStates.TryGetValue(typeMapGroup, out Map map) ? map : new Map(typeMapGroup, owner);
        }

        // Scans the type map attributes starting at a source. If the group is not null, only the entries of that group are traversed.
        // The owner is the name of the type that a map that starts at an owner type starts at.
        private static Dictionary<TypeDesc, Map> Scan(ScanSource firstSource, TypeDesc firstTypeMapGroup, string owner, ModuleDesc throwHelperEmitModule, TypeMapAssemblyTargetsMode assemblyTargetsMode)
        {
            Dictionary<TypeDesc, Map> typeMapStates = [];
            // The pendingMaps collection represents sources that have been scanned, but the provided source
            // has not been added as a target yet for the specified type map group.
            // This can occur when an assembly or a type is the target of a TypeMapAssemblyTarget or TypeMapTypeTarget attribute for a different group.
            Dictionary<(ScanSource source, TypeDesc typeMapGroup), (TypeDesc scannedDuringGroup, Map map)> pendingMaps = [];
            HashSet<(ScanSource source, TypeDesc typeMapGroup)> scannedSources = [];

            Queue<(ScanSource source, TypeDesc typeMapGroup)> sourcesToScan = [];
            sourcesToScan.Enqueue((firstSource, firstTypeMapGroup));

            while (sourcesToScan.Count > 0)
            {
                (ScanSource currentSource, TypeDesc currentTypeMapGroup) = sourcesToScan.Dequeue();
                EcmaModule currentModule = currentSource.Module;

                // A null currentTypeMapGroup means we're looking at all type maps in the assembly, not just traversing one.
                // Otherwise, we're searching for a specific one.
                // Don't rescan specific source + type map group combos as the results will be identical.
                if (currentTypeMapGroup is not null)
                {
                    if (scannedSources.Contains((currentSource, currentTypeMapGroup)))
                    {
                        if (pendingMaps.TryGetValue((currentSource, currentTypeMapGroup), out var pendingMap))
                        {
                            // We have already scanned this source, but we haven't added the results to the type map state for the specified type map group.
                            // Merge in the results.
                            if (!typeMapStates.TryGetValue(currentTypeMapGroup, out Map typeMapState))
                            {
                                typeMapStates[currentTypeMapGroup] = typeMapState = new Map(currentTypeMapGroup, owner);
                            }
                            typeMapState.MergePendingMap(throwHelperEmitModule, pendingMap.map);
                            foreach (ModuleDesc targetModule in pendingMap.map.TargetModules)
                            {
                                Debug.Assert(assemblyTargetsMode == TypeMapAssemblyTargetsMode.Traverse, "We should only have pending maps with target modules when we're traversing for type map groups, as opposed to just recording targets.");
                                // If the pending map has target modules,
                                // then we need to ensure that they also get included in the metadata.
                                sourcesToScan.Enqueue((ScanSource.ForAssembly((EcmaAssembly)targetModule), currentTypeMapGroup));
                            }
                            foreach (EcmaType targetType in pendingMap.map.TargetTypes)
                            {
                                sourcesToScan.Enqueue((ScanSource.ForType(targetType), currentTypeMapGroup));
                            }
                            pendingMaps.Remove((currentSource, currentTypeMapGroup));
                        }
                        // We've already scanned this source for this type map group.
                        // There's no more work to do for this case.
                        continue;
                    }
                }

                // Either we haven't seen this source + group combo before
                // or we are scanning all type map groups in the assembly.
                foreach (CustomAttributeHandle attrHandle in currentModule.MetadataReader.GetCustomAttributes(currentSource.Handle))
                {
                    CustomAttribute attr = currentModule.MetadataReader.GetCustomAttribute(attrHandle);

                    if (!MetadataExtensions.GetAttributeTypeAndConstructor(currentModule.MetadataReader, attrHandle, out EntityHandle attributeType, out _))
                    {
                        continue;
                    }

                    TypeDesc type = (TypeDesc)currentModule.GetObject(attributeType, NotFoundBehavior.ReturnNull);
                    if (type == null)
                    {
                        // If the type doesn't resolve, it can't be a type map attribute
                        continue;
                    }

                    TypeMapAttributeKind attrKind = LookupTypeMapType(type);

                    if (attrKind == TypeMapAttributeKind.None)
                    {
                        // Not a type map attribute, skip it
                        continue;
                    }

                    if (attrKind == TypeMapAttributeKind.TypeMapAssemblyTarget && currentSource.Handle.Kind != HandleKind.AssemblyDefinition)
                    {
                        // The assembly targets are only for the assemblies: a type that holds entries does not link other assemblies.
                        continue;
                    }

                    TypeDesc typeMapGroup = type.Instantiation[0];

                    Map typeMapState;

                    if (currentTypeMapGroup is null || typeMapGroup == currentTypeMapGroup)
                    {
                        // This attribute is definitely included in the type map.
                        if (!typeMapStates.TryGetValue(typeMapGroup, out typeMapState))
                        {
                            typeMapStates[typeMapGroup] = typeMapState = new Map(typeMapGroup, owner);
                        }
                    }
                    else
                    {
                        // This attribute may be included in the type map, but this type map group isn't the one that triggered us to scan this assembly,
                        // so we need to save it as a pending map in case its type map group is triggered later when we scan another assembly.
                        if (!pendingMaps.TryGetValue((currentSource, typeMapGroup), out var pendingMap))
                        {
                            typeMapState = new Map(typeMapGroup, owner);
                            pendingMaps[(currentSource, typeMapGroup)] = (currentTypeMapGroup, typeMapState);
                        }
                        else
                        {
                            if (pendingMap.scannedDuringGroup != currentTypeMapGroup)
                            {
                                // We already scanned this assembly for this type map group while
                                // processing a different type map group.
                                // Skip processing it again to avoid hitting duplicate entries.
                                continue;
                            }
                            typeMapState = pendingMap.map;
                        }
                    }

                    // Mark this source + type map group as scanned to avoid redundant work in the future.
                    scannedSources.Add((currentSource, typeMapGroup));

                    try
                    {
                        CustomAttributeValue<TypeWithSerializedName> attrValue = attr.DecodeValue(new TypeMapCustomAttributeTypeProvider(currentModule));

                        switch (attrKind)
                        {
                            case TypeMapAttributeKind.TypeMapAssemblyTarget:
                                ProcessTypeMapAssemblyTargetAttribute(attrValue, typeMapState);
                                break;

                            case TypeMapAttributeKind.TypeMapTypeTarget:
                                ProcessTypeMapTypeTargetAttribute(attrValue, typeMapState);
                                break;

                            case TypeMapAttributeKind.TypeMap:
                                ProcessTypeMapAttribute(attrValue, typeMapState);
                                break;

                            case TypeMapAttributeKind.TypeMapAssociation:
                                ProcessTypeMapAssociationAttribute(attrValue, typeMapState);
                                break;

                            default:
                                Debug.Fail($"Unexpected TypeMapAttributeKind: {attrKind}");
                                break;
                        }
                    }
                    catch (TypeSystemException ex)
                    {
                        // The map that the attribute is for: if the attribute is only pending (it is for a group that this scan is not traversing),
                        // the exception only takes effect when the source is reached for that group.
                        Map value = typeMapState;
                        if (attrKind is TypeMapAttributeKind.TypeMapAssemblyTarget)
                        {
                            value.HasAssemblyTargetAttributes = true;
                        }

                        if (attrKind is TypeMapAttributeKind.TypeMapAssemblyTarget or TypeMapAttributeKind.TypeMapTypeTarget or TypeMapAttributeKind.TypeMap)
                        {
                            value.SetExternalTypeMapException(throwHelperEmitModule, ex);
                        }

                        if (attrKind is TypeMapAttributeKind.TypeMapAssemblyTarget or TypeMapAttributeKind.TypeMapTypeTarget or TypeMapAttributeKind.TypeMapAssociation)
                        {
                            value.SetAssociatedTypeMapException(throwHelperEmitModule, ex);
                        }
                    }
                }

                if (currentTypeMapGroup is not null)
                {
                    // Mark this source + type map group as scanned to avoid redundant work in the future.
                    // We can hit this case when the type map group has no entries in the current source.
                    scannedSources.Add((currentSource, currentTypeMapGroup));
                }

                void ProcessTypeMapAssemblyTargetAttribute(CustomAttributeValue<TypeWithSerializedName> attrValue, Map typeMapState)
                {
                    typeMapState.HasAssemblyTargetAttributes = true;

                    if (attrValue.FixedArguments is not [{ Value: string assemblyName }])
                    {
                        ThrowHelper.ThrowBadImageFormatException();
                        return;
                    }

                    EcmaAssembly targetAssembly = (EcmaAssembly)currentModule.Context.ResolveAssembly(AssemblyNameInfo.Parse(assemblyName), throwIfNotFound: true);
                    typeMapState.AddTargetModule(targetAssembly);

                    if (assemblyTargetsMode == TypeMapAssemblyTargetsMode.Traverse
                        && (currentTypeMapGroup is null || currentTypeMapGroup == typeMapState.TypeMapGroup))
                    {
                        // If we're traversing for the current type map group, enqueue the target to be scanned.
                        // Otherwise, we'll pull the targets to be scanned when the pending group is scanned.
                        sourcesToScan.Enqueue((ScanSource.ForAssembly(targetAssembly), typeMapState.TypeMapGroup));
                    }
                }

                void ProcessTypeMapTypeTargetAttribute(CustomAttributeValue<TypeWithSerializedName> attrValue, Map typeMapState)
                {
                    if (assemblyTargetsMode == TypeMapAssemblyTargetsMode.Record)
                    {
                        // The type maps that are recorded for a module (ReadyToRun) only cover the attributes of the assembly.
                        // The runtime reads the types that hold entries from the attributes, after the assemblies of the group.
                        return;
                    }

                    if (attrValue.FixedArguments is not [{ Value: TypeWithSerializedName { Type: TypeDesc holderType } }])
                    {
                        ThrowHelper.ThrowBadImageFormatException();
                        return;
                    }

                    // A type that is not defined in metadata (an array for example) does not have attributes.
                    if (holderType.GetTypeDefinition() is not EcmaType holder)
                    {
                        return;
                    }

                    typeMapState.AddTargetType(holder);

                    if (currentTypeMapGroup is null || currentTypeMapGroup == typeMapState.TypeMapGroup)
                    {
                        // Same as for the assembly targets: the pending groups pull their targets when they are scanned.
                        sourcesToScan.Enqueue((ScanSource.ForType(holder), typeMapState.TypeMapGroup));
                    }
                }

                void ProcessTypeMapAttribute(CustomAttributeValue<TypeWithSerializedName> attrValue, Map typeMapState)
                {
                    switch (attrValue.FixedArguments)
                    {
                        case
                        [
                            { Value: string typeName },
                            { Value: TypeWithSerializedName { Type: TypeDesc targetType, SerializedName: string serializedTargetTypeName } }
                        ]:
                        {
                            typeMapState.AddExternalTypeMapEntry(
                                typeName,
                                targetType,
                                serializedTargetTypeName,
                                null,
                                null,
                                currentModule);
                            break;
                        }

                        case
                        [
                            { Value: string typeName },
                            { Value: TypeWithSerializedName { Type: TypeDesc targetType, SerializedName: string serializedTargetTypeName } },
                            { Value: TypeWithSerializedName { Type: TypeDesc trimTargetType, SerializedName: string serializedTrimTargetTypeName } }
                        ]:
                        {
                            typeMapState.AddExternalTypeMapEntry(
                                typeName,
                                targetType,
                                serializedTargetTypeName,
                                trimTargetType,
                                serializedTrimTargetTypeName,
                                currentModule);
                            break;
                        }

                        default:
                            ThrowHelper.ThrowBadImageFormatException();
                            return;
                    }
                }

                void ProcessTypeMapAssociationAttribute(CustomAttributeValue<TypeWithSerializedName> attrValue, Map typeMapState)
                {
                    // If attribute is TypeMapAssociationAttribute, we need to extract the generic argument (type map group)
                    // and process it.
                    if (attrValue.FixedArguments is not
                        [
                            { Value: TypeWithSerializedName { Type: TypeDesc type, SerializedName: string serializedTypeName } },
                            { Value: TypeWithSerializedName { Type: TypeDesc associatedType, SerializedName: string serializedAssociatedTypeName } }
                        ])
                    {
                        ThrowHelper.ThrowBadImageFormatException();
                        return;
                    }

                    typeMapState.AddAssociatedTypeMapEntry(
                        type,
                        serializedTypeName,
                        associatedType,
                        serializedAssociatedTypeName,
                        currentModule);
                }
            }

            return typeMapStates;
        }
    }

    public enum TypeMapAssemblyTargetsMode
    {
        /// <summary>
        /// Traverse the TypeMapAssemblyTarget attributes and include type maps from the target assemblies.
        /// </summary>
        Traverse,
        /// <summary>
        /// Record the TypeMapAssemblyTarget attributes but do not traverse them to include type maps from the target assemblies.
        /// </summary>
        Record
    }
}
