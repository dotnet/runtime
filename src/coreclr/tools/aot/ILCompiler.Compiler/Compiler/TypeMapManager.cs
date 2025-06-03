// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection.Metadata;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;
using Internal.IL;
using Internal.IL.Stubs;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using DependencyList = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.DependencyList;
using ReflectionMapBlob = Internal.Runtime.ReflectionMapBlob;

namespace ILCompiler
{
    /// <summary>
    /// This class is responsible for managing emitted data for type maps.
    /// </summary>
    public sealed class TypeMapManager : ICompilationRootProvider
    {
        private sealed class TypeMapState
        {
            private readonly Dictionary<TypeDesc, TypeDesc> _associatedTypeMap = [];
            private readonly Dictionary<string, (TypeDesc type, TypeDesc trimmingTarget)> _externalTypeMap = [];
            private ThrowingMethodStub _externalTypeMapExceptionStub;
            private ThrowingMethodStub _associatedTypeMapExceptionStub;

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

            public void SetExternalTypeMapStub(ThrowingMethodStub stub)
            {
                if (_externalTypeMapExceptionStub?.Exception is TypeSystemException.FileNotFoundException)
                {
                    // FileNotFound exception takes precedence.
                    return;
                }
                _externalTypeMapExceptionStub ??= stub;
            }

            public void SetAssociatedTypeMapStub(ThrowingMethodStub stub)
            {
                if (_associatedTypeMapExceptionStub?.Exception is TypeSystemException.FileNotFoundException)
                {
                    // FileNotFound exception takes precedence.
                    return;
                }
                _associatedTypeMapExceptionStub ??= stub;
            }

            public object GetExternalTypeMapNode(TypeDesc group)
            {
                if (_externalTypeMapExceptionStub is not null)
                {
                    return new InvalidExternalTypeMapNode(group, _externalTypeMapExceptionStub);
                }
                return new ExternalTypeMapNode(group, _externalTypeMap);
            }
        }

        private sealed class ThrowingMethodStub(TypeDesc owningType, TypeSystemException ex) : ILStubMethod
        {
            public TypeSystemException Exception { get; } = ex;

            public override string Name => "InvalidTypeMapStub";
            public override MethodIL EmitIL()
            {
                return TypeSystemThrowingILEmitter.EmitIL(this, Exception);
            }

            protected override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer) => other is ThrowingMethodStub otherStub ? Exception.Message.CompareTo(otherStub.Exception.Message, StringComparison.Ordinal) : -1;

            public override bool IsPInvoke => false;

            public override string DiagnosticName => "InvalidTypeMap";

            protected override int ClassCode => 1744789196;

            public override TypeDesc OwningType => owningType;

            public override MethodSignature Signature => new MethodSignature(MethodSignatureFlags.Static, 0, Context.GetWellKnownType(WellKnownType.Void), []);

            public override TypeSystemContext Context => owningType.Context;
        }

        private Dictionary<TypeDesc, TypeMapState> _typeMapStates = new Dictionary<TypeDesc, TypeMapState>();
        private Dictionary<TypeSystemException, ThrowingMethodStub> _exceptionStubs = new Dictionary<TypeSystemException, ThrowingMethodStub>();

        private enum TypeMapAttributeKind
        {
            None,
            TypeMapAssemblyTarget,
            TypeMap,
            TypeMapAssociation
        }

        private ModuleDesc _entryModule;

        public TypeMapManager(ModuleDesc entryModule)
        {
            _entryModule = entryModule;

            if (entryModule is not { Assembly: EcmaAssembly assembly })
            {
                // We can only process EcmaAssembly-based modules as we can only read custom attributes from them.
                return;
            }

            HashSet<EcmaAssembly> scannedAssemblies = [];

            Queue<EcmaAssembly> assembliesToScan = new Queue<EcmaAssembly>();
            assembliesToScan.Enqueue(assembly);

            TypeDesc typeMapAssemblyTargetType = assembly.Context.SystemModule.GetTypeByCustomAttributeTypeName("System.Runtime.InteropServices.TypeMapAssemblyTargetAttribute`1");

            while (assembliesToScan.Count > 0)
            {
                EcmaAssembly currentAssembly = assembliesToScan.Dequeue();
                if (scannedAssemblies.Contains(currentAssembly))
                    continue;

                scannedAssemblies.Add(currentAssembly);

                foreach (CustomAttributeHandle attrHandle in assembly.MetadataReader.GetCustomAttributes(EntityHandle.AssemblyDefinition))
                {
                    CustomAttribute attr = assembly.MetadataReader.GetCustomAttribute(attrHandle);

                    if (!MetadataExtensions.GetAttributeTypeAndConstructor(assembly.MetadataReader, attrHandle, out EntityHandle attributeType, out _))
                    {
                        continue;
                    }

                    TypeDesc type = (TypeDesc)assembly.GetObject(attributeType);

                    TypeMapAttributeKind attrKind = TypeMapAttributeKind.None;

                    if (typeMapAssemblyTargetType == type.GetTypeDefinition())
                    {
                        attrKind = TypeMapAttributeKind.TypeMapAssemblyTarget;
                    }

                    if (attrKind == TypeMapAttributeKind.None)
                    {
                        continue;
                    }

                    CustomAttributeValue<TypeDesc> attrValue = attr.DecodeValue(new CustomAttributeTypeProvider(currentAssembly));

                    TypeDesc typeMapGroup = type.Instantiation[0];

                    try
                    {
                        switch (attrKind)
                        {
                            case TypeMapAttributeKind.TypeMapAssemblyTarget:
                                ProcessTypeMapAssemblyTargetAttribute(attrValue, typeMapGroup);
                                break;

                            case TypeMapAttributeKind.TypeMap:
                                ProcessTypeMapAttribute(attrValue, typeMapGroup);
                                break;

                            case TypeMapAttributeKind.TypeMapAssociation:
                                ProcessTypeMapAssociationAttribute(attrValue, typeMapGroup);
                                break;

                            default:
                                break;
                        }
                    }
                    catch (TypeSystemException ex)
                    {
                        if (!_exceptionStubs.TryGetValue(ex, out ThrowingMethodStub stub))
                        {
                            _exceptionStubs[ex] = stub = new ThrowingMethodStub(typeMapGroup, ex);
                        }
                        _typeMapStates[typeMapGroup] ??= new TypeMapState();
                        if (attrKind is TypeMapAttributeKind.TypeMapAssemblyTarget or TypeMapAttributeKind.TypeMap)
                        {
                            _typeMapStates[typeMapGroup].SetExternalTypeMapStub(stub);
                        }

                        if (attrKind is TypeMapAttributeKind.TypeMapAssemblyTarget or TypeMapAttributeKind.TypeMapAssociation)
                        {
                            _typeMapStates[typeMapGroup].SetAssociatedTypeMapStub(stub);
                        }
                    }
                }

                void ProcessTypeMapAssemblyTargetAttribute(CustomAttributeValue<TypeDesc> attrValue, TypeDesc typeMapGroup)
                {
                    // If attribute is TypeMapAssemblyTargetAttribute, we need to extract the generic argument (type map group)
                    // and process it.
                    if (attrValue.FixedArguments is not [{ Value: string assemblyName }])
                    {
                        ThrowHelper.ThrowBadImageFormatException();
                        return;
                    }

                    EcmaAssembly targetAssembly = (EcmaAssembly)assembly.Context.ResolveAssembly(AssemblyNameInfo.Parse(assemblyName), throwIfNotFound: true);

                    assembliesToScan.Enqueue(targetAssembly);
                }

                void ProcessTypeMapAttribute(CustomAttributeValue<TypeDesc> attrValue, TypeDesc typeMapGroup)
                {
                    switch (attrValue.FixedArguments)
                    {
                        case [{ Value: string typeName }, { Value: TypeDesc targetType }]:
                        {
                            if (!_typeMapStates.TryGetValue(typeMapGroup, out TypeMapState typeMapState))
                            {
                                _typeMapStates[typeMapGroup] = typeMapState = new TypeMapState();
                            }
                            typeMapState.AddExternalTypeMapEntry(typeName, targetType, targetType);
                            break;
                        }

                        case [{ Value: string typeName }, { Value: TypeDesc targetType }, { Value: TypeDesc trimTargetType }]:
                        {
                            if (!_typeMapStates.TryGetValue(typeMapGroup, out TypeMapState typeMapState))
                            {
                                _typeMapStates[typeMapGroup] = typeMapState = new TypeMapState();
                            }
                            typeMapState.AddExternalTypeMapEntry(typeName, targetType, targetType);
                            break;
                        }

                        default:
                            ThrowHelper.ThrowBadImageFormatException();
                            return;
                    }
                }

                void ProcessTypeMapAssociationAttribute(CustomAttributeValue<TypeDesc> attrValue, TypeDesc typeMapGroup)
                {
                    // If attribute is TypeMapAssociationAttribute, we need to extract the generic argument (type map group)
                    // and process it.
                    if (attrValue.FixedArguments is not [{ Value: TypeDesc type }, { Value: TypeDesc associatedType }])
                    {
                        ThrowHelper.ThrowBadImageFormatException();
                        return;
                    }

                    if (!_typeMapStates.TryGetValue(typeMapGroup, out TypeMapState typeMapState))
                    {
                        _typeMapStates[typeMapGroup] = typeMapState = new TypeMapState();
                    }

                    typeMapState.AddAssociatedTypeMapEntry(type, associatedType);
                }
            }
        }

        private sealed class TypeMapsNode(IReadOnlyDictionary<TypeDesc, TypeMapState> typeMapState) : DependencyNodeCore<NodeFactory>
        {
            public override bool InterestingForDynamicDependencyAnalysis => false;

            public override bool HasDynamicDependencies => false;

            public override bool HasConditionalStaticDependencies => true;

            public override bool StaticDependenciesAreComputed => true;

            public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context)
            {
                List<CombinedDependencyListEntry> entries = [];
                foreach ((TypeDesc typeMapGroup, TypeMapState typeMapState) in typeMapState)
                {
                    entries.Add(new CombinedDependencyListEntry(typeMapState.GetExternalTypeMapNode(typeMapGroup), context.ExternalTypeMapRequest(typeMapGroup), "ExternalTypeMap"));
                }

                return entries;
            }

            public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context) => Array.Empty<DependencyListEntry>();
            public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => Array.Empty<CombinedDependencyListEntry>();
            protected override string GetName(NodeFactory context) => "TypeMapsNode";
        }

        public void AddCompilationRoots(IRootingServiceProvider rootProvider)
        {
            if (_typeMapStates.Count == 0)
            {
                return; // No type maps to process
            }

            Debug.Assert(_entryModule is not null, "We should only find type map entries if we have an entry module.");

            rootProvider.AddCompilationRoot(new TypeMapsNode(_typeMapStates), "TypeMapManager");
        }

        public void AddToReadyToRunHeader(ReadyToRunHeaderNode header, NodeFactory nodeFactory, ExternalReferencesTableNode commonFixupsTableNode)
        {
            if (_typeMapStates.Count == 0)
            {
                return; // No type maps to emit
            }

            ExternalTypeMapObjectNode externalTypeMap = new ExternalTypeMapObjectNode(commonFixupsTableNode);
            header.Add(MetadataManager.BlobIdToReadyToRunSection(ReflectionMapBlob.ExternalTypeMap), externalTypeMap);
        }
    }
}
