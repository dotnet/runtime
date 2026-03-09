// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using ILCompiler.DependencyAnalysis;
using Internal.IL;
using Internal.IL.Stubs;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using static ILCompiler.TypeMapManager;
using static ILCompiler.UsageBasedTypeMapManager;

namespace ILCompiler
{
    public sealed class TypeMapMetadata
    {
        internal sealed class Map
        {
            private sealed class ThrowingMethodStub : ILStubMethod
            {
                private readonly TypeDesc _typeMapGroup;
                private readonly byte[] _name;

                public ThrowingMethodStub(TypeDesc owningType, TypeDesc typeMapGroup, bool externalTypeMap, TypeSystemException ex)
                {
                    OwningType = owningType;
                    _typeMapGroup = typeMapGroup;
                    _name = System.Text.Encoding.UTF8.GetBytes($"InvalidTypeMapStub_{_typeMapGroup}_{(externalTypeMap ? "External" : "Proxy")}");
                    Exception = ex;
                }

                public TypeSystemException Exception { get; }
                public override ReadOnlySpan<byte> Name => _name;
                public override MethodIL EmitIL()
                {
                    return TypeSystemThrowingILEmitter.EmitIL(this, Exception);
                }

                protected override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
                {
                    return Name.SequenceCompareTo(other.Name);
                }

                public override bool IsPInvoke => false;

                public override string DiagnosticName => GetName();

                protected override int ClassCode => 1744789196;

                public override TypeDesc OwningType { get; }

                public override MethodSignature Signature => new MethodSignature(MethodSignatureFlags.Static, 0, Context.GetWellKnownType(WellKnownType.Void), []);

                public override TypeSystemContext Context => OwningType.Context;
            }

            private readonly Dictionary<TypeDesc, TypeDesc> _associatedTypeMap = [];
            private readonly Dictionary<string, (TypeDesc type, TypeDesc trimmingTarget)> _externalTypeMap = [];
            private readonly List<ModuleDesc> _targetModules = [];
            private ThrowingMethodStub _externalTypeMapExceptionStub;
            private ThrowingMethodStub _associatedTypeMapExceptionStub;

            public Map(TypeDesc typeMapGroup)
            {
                TypeMapGroup = typeMapGroup;
            }

            public TypeDesc TypeMapGroup { get; }

            public void AddAssociatedTypeMapEntry(TypeDesc type, TypeDesc associatedType)
            {
                if (!_associatedTypeMap.TryAdd(type, associatedType))
                {
                    ThrowHelper.ThrowBadImageFormatException();
                }
            }
            public void AddExternalTypeMapEntry(string typeName, TypeDesc type, TypeDesc trimmingTarget)
            {
                if (!_externalTypeMap.TryAdd(typeName, (type, trimmingTarget)))
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
                _externalTypeMapExceptionStub ??= new ThrowingMethodStub(stubModule.GetGlobalModuleType(), TypeMapGroup, externalTypeMap: true, exception);
            }

            public void SetAssociatedTypeMapException(ModuleDesc stubModule, TypeSystemException exception)
            {
                if (_associatedTypeMapExceptionStub?.Exception is TypeSystemException.FileNotFoundException)
                {
                    // FileNotFound exception takes precedence.
                    return;
                }
                _associatedTypeMapExceptionStub ??= new ThrowingMethodStub(stubModule.GetGlobalModuleType(), TypeMapGroup, externalTypeMap: false, exception);
            }

            public void MergePendingMap(Map pendingMap)
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
                        foreach (KeyValuePair<TypeDesc, TypeDesc> kvp in pendingMap._associatedTypeMap)
                        {
                            AddAssociatedTypeMapEntry(kvp.Key, kvp.Value);
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
                        foreach (KeyValuePair<string, (TypeDesc type, TypeDesc trimmingTarget)> kvp in pendingMap._externalTypeMap)
                        {
                            AddExternalTypeMapEntry(kvp.Key, kvp.Value.type, kvp.Value.trimmingTarget);
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
            }

            public IExternalTypeMapNode GetExternalTypeMapNode()
            {
                if (_externalTypeMapExceptionStub is not null)
                {
                    return new InvalidExternalTypeMapNode(TypeMapGroup, _externalTypeMapExceptionStub);
                }
                return new ExternalTypeMapNode(TypeMapGroup, _externalTypeMap);
            }

            public IProxyTypeMapNode GetProxyTypeMapNode()
            {
                if (_associatedTypeMapExceptionStub is not null)
                {
                    return new InvalidProxyTypeMapNode(TypeMapGroup, _associatedTypeMapExceptionStub);
                }
                return new ProxyTypeMapNode(TypeMapGroup, _associatedTypeMap);
            }

            public void AddTargetModule(ModuleDesc targetModule)
            {
                _targetModules.Add(targetModule);
            }

            /// <summary>
            /// The modules targeted with TypeMapAssemblyTarget attributes for this type map group. This is only populated when TypeMapMetadata is created with TypeMapAssemblyTargetsMode.Record. When TypeMapMetadata is created with TypeMapAssemblyTargetsMode.Traverse, this will be empty as the target assemblies will be traversed to include their type maps instead of just being recorded as targets.
            /// </summary>
            public IEnumerable<ModuleDesc> TargetModules => _targetModules;
        }

        public static readonly TypeMapMetadata Empty = new TypeMapMetadata(new Dictionary<TypeDesc, Map>(), "No type maps");

        private readonly IReadOnlyDictionary<TypeDesc, Map> _states;

        private TypeMapMetadata(IReadOnlyDictionary<TypeDesc, Map> states, string diagnosticName)
        {
            _states = states;
            DiagnosticName = diagnosticName;
        }

        internal Map this[TypeDesc typeMapGroup] => _states[typeMapGroup];

        public bool IsEmpty => _states.Count == 0;

        internal IEnumerable<KeyValuePair<TypeDesc, Map>> Maps => _states;

        public string DiagnosticName { get; }

        public static TypeMapMetadata CreateFromAssembly(EcmaAssembly assembly, ModuleDesc throwHelperEmitModule, TypeMapAssemblyTargetsMode assemblyTargetsMode)
        {
            Dictionary<TypeDesc, Map> typeMapStates = [];
            // The pendingMaps collection represents assemblies that have been scanned, but the provided assembly
            // has not been added as a target yet for the specified type map group.
            // This can occur when an assembly is the target of a TypeMapAssemblyTarget attribute for a different group.
            Dictionary<(EcmaAssembly assembly, TypeDesc typeMapGroup), (TypeDesc scannedDuringGroup, Map map)> pendingMaps = [];
            HashSet<(EcmaAssembly assembly, TypeDesc typeMapGroup)> scannedAssemblies = [];

            Queue<(EcmaAssembly assembly, TypeDesc typeMapGroup)> assembliesToScan = [];
            assembliesToScan.Enqueue((assembly, null));

            while (assembliesToScan.Count > 0)
            {
                (EcmaAssembly currentAssembly, TypeDesc currentTypeMapGroup) = assembliesToScan.Dequeue();

                // A null currentTypeMapGroup means we're looking at all type maps in the assembly, not just traversing one.
                // Otherwise, we're searching for a specific one.
                // Don't rescan specific assembly + type map group combos as the results will be identical.
                if (currentTypeMapGroup is not null)
                {
                    if (scannedAssemblies.Contains((currentAssembly, currentTypeMapGroup)))
                    {
                        if (pendingMaps.TryGetValue((currentAssembly, currentTypeMapGroup), out var pendingMap))
                        {
                            // We have already scanned this assembly, but we haven't added the results to the type map state for the specified type map group.
                            // Merge in the results.
                            if (!typeMapStates.TryGetValue(currentTypeMapGroup, out Map typeMapState))
                            {
                                typeMapStates[currentTypeMapGroup] = typeMapState = new Map(currentTypeMapGroup);
                            }
                            typeMapState.MergePendingMap(pendingMap.map);
                            foreach (ModuleDesc targetModule in pendingMap.map.TargetModules)
                            {
                                Debug.Assert(assemblyTargetsMode == TypeMapAssemblyTargetsMode.Traverse, "We should only have pending maps with target modules when we're traversing for type map groups, as opposed to just recording targets.");
                                // If the pending map has target modules,
                                // then we need to ensure that they also get included in the metadata.
                                assembliesToScan.Enqueue(((EcmaAssembly)targetModule, currentTypeMapGroup));
                            }
                            pendingMaps.Remove((currentAssembly, currentTypeMapGroup));
                        }
                        // We've already scanned this assembly for this type map group.
                        // There's no more work to do for this case.
                        continue;
                    }
                }

                // Either we haven't seen this assembly + group combo before
                // or we are scanning all type map groups in the assembly.
                foreach (CustomAttributeHandle attrHandle in currentAssembly.MetadataReader.GetCustomAttributes(EntityHandle.AssemblyDefinition))
                {
                    CustomAttribute attr = currentAssembly.MetadataReader.GetCustomAttribute(attrHandle);

                    if (!MetadataExtensions.GetAttributeTypeAndConstructor(currentAssembly.MetadataReader, attrHandle, out EntityHandle attributeType, out _))
                    {
                        continue;
                    }

                    TypeDesc type = (TypeDesc)currentAssembly.GetObject(attributeType, NotFoundBehavior.ReturnNull);
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

                    CustomAttributeValue<TypeDesc> attrValue = attr.DecodeValue(new CustomAttributeTypeProvider(currentAssembly));

                    TypeDesc typeMapGroup = type.Instantiation[0];

                    Map typeMapState;

                    if (currentTypeMapGroup is null || typeMapGroup == currentTypeMapGroup)
                    {
                        // This attribute is definitely included in the type map.
                        if (!typeMapStates.TryGetValue(typeMapGroup, out typeMapState))
                        {
                            typeMapStates[typeMapGroup] = typeMapState = new Map(typeMapGroup);
                        }
                    }
                    else
                    {
                        // This attribute may be included in the type map, but this type map group isn't the one that triggered us to scan this assembly,
                        // so we need to save it as a pending map in case its type map group is triggered later when we scan another assembly.
                        if (!pendingMaps.TryGetValue((currentAssembly, typeMapGroup), out var pendingMap))
                        {
                            typeMapState = new Map(typeMapGroup);
                            pendingMaps[(currentAssembly, typeMapGroup)] = (currentTypeMapGroup, typeMapState);
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

                    // Mark this assembly + type map group as scanned to avoid redundant work in the future.
                    scannedAssemblies.Add((currentAssembly, typeMapGroup));

                    try
                    {
                        switch (attrKind)
                        {
                            case TypeMapAttributeKind.TypeMapAssemblyTarget:
                                ProcessTypeMapAssemblyTargetAttribute(attrValue, typeMapState);
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
                        if (!typeMapStates.TryGetValue(typeMapGroup, out Map value))
                        {
                            value = new Map(typeMapGroup);
                            typeMapStates[typeMapGroup] = value;
                        }

                        if (attrKind is TypeMapAttributeKind.TypeMapAssemblyTarget or TypeMapAttributeKind.TypeMap)
                        {
                            value.SetExternalTypeMapException(throwHelperEmitModule, ex);
                        }

                        if (attrKind is TypeMapAttributeKind.TypeMapAssemblyTarget or TypeMapAttributeKind.TypeMapAssociation)
                        {
                            value.SetAssociatedTypeMapException(throwHelperEmitModule, ex);
                        }
                    }
                }

                if (currentTypeMapGroup is not null)
                {
                    // Mark this assembly + type map group as scanned to avoid redundant work in the future.
                    // We can hit this case when the type map group has no entries in the current assembly.
                    scannedAssemblies.Add((currentAssembly, currentTypeMapGroup));
                }

                void ProcessTypeMapAssemblyTargetAttribute(CustomAttributeValue<TypeDesc> attrValue, Map typeMapState)
                {
                    if (attrValue.FixedArguments is not [{ Value: string assemblyName }])
                    {
                        ThrowHelper.ThrowBadImageFormatException();
                        return;
                    }

                    EcmaAssembly targetAssembly = (EcmaAssembly)assembly.Context.ResolveAssembly(AssemblyNameInfo.Parse(assemblyName), throwIfNotFound: true);
                    typeMapState.AddTargetModule(targetAssembly);

                    if (assemblyTargetsMode == TypeMapAssemblyTargetsMode.Traverse
                        && (currentTypeMapGroup is null || currentTypeMapGroup == typeMapState.TypeMapGroup))
                    {
                        // If we're traversing for the current type map group, enqueue the target to be scanned.
                        // Otherwise, we'll pull the targets to be scanned when the pending group is scanned.
                        assembliesToScan.Enqueue((targetAssembly, typeMapState.TypeMapGroup));
                    }
                }

                void ProcessTypeMapAttribute(CustomAttributeValue<TypeDesc> attrValue, Map typeMapState)
                {
                    switch (attrValue.FixedArguments)
                    {
                        case [{ Value: string typeName }, { Value: TypeDesc targetType }]:
                        {
                            typeMapState.AddExternalTypeMapEntry(typeName, targetType, null);
                            break;
                        }

                        case [{ Value: string typeName }, { Value: TypeDesc targetType }, { Value: TypeDesc trimTargetType }]:
                        {
                            typeMapState.AddExternalTypeMapEntry(typeName, targetType, trimTargetType);
                            break;
                        }

                        default:
                            ThrowHelper.ThrowBadImageFormatException();
                            return;
                    }
                }

                void ProcessTypeMapAssociationAttribute(CustomAttributeValue<TypeDesc> attrValue, Map typeMapState)
                {
                    // If attribute is TypeMapAssociationAttribute, we need to extract the generic argument (type map group)
                    // and process it.
                    if (attrValue.FixedArguments is not [{ Value: TypeDesc type }, { Value: TypeDesc associatedType }])
                    {
                        ThrowHelper.ThrowBadImageFormatException();
                        return;
                    }

                    typeMapState.AddAssociatedTypeMapEntry(type, associatedType);
                }
            }

            return new TypeMapMetadata(typeMapStates, $"Type maps rooted at {assembly}");
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
