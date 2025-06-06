// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
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
    public abstract class TypeMapManager : ICompilationRootProvider
    {
        protected internal sealed class TypeMapState
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

            public object GetProxyTypeMapNode(TypeDesc group)
            {
                if (_associatedTypeMapExceptionStub is not null)
                {
                    return new InvalidProxyTypeMapNode(group, _associatedTypeMapExceptionStub);
                }
                return new ProxyTypeMapNode(group, _associatedTypeMap);
            }
        }

        public sealed class TypeMapStates
        {
            public static readonly TypeMapStates Empty = new TypeMapStates(new Dictionary<TypeDesc, TypeMapState>());

            private readonly IReadOnlyDictionary<TypeDesc, TypeMapState> _states;

            internal TypeMapStates(IReadOnlyDictionary<TypeDesc, TypeMapState> states)
            {
                _states = states;
            }

            internal TypeMapState this[TypeDesc typeMapGroup] => _states[typeMapGroup];

            public bool IsEmpty => _states.Count == 0;

            internal IEnumerable<KeyValuePair<TypeDesc, TypeMapState>> States => _states;
        }

        protected internal sealed class ThrowingMethodStub(TypeDesc owningType, TypeDesc typeMapGroup, bool externalTypeMap, TypeSystemException ex) : ILStubMethod
        {
            public TypeSystemException Exception => ex;
            public override string Name => $"InvalidTypeMapStub_{typeMapGroup}_{(externalTypeMap ? "External" : "Proxy")}";
            public override MethodIL EmitIL()
            {
                return TypeSystemThrowingILEmitter.EmitIL(this, Exception);
            }

            protected override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer) => other is ThrowingMethodStub otherStub ? Name.CompareTo(otherStub.Name, StringComparison.Ordinal) : -1;

            public override bool IsPInvoke => false;

            public override string DiagnosticName => Name;

            protected override int ClassCode => 1744789196;

            public override TypeDesc OwningType => owningType;

            public override MethodSignature Signature => new MethodSignature(MethodSignatureFlags.Static, 0, Context.GetWellKnownType(WellKnownType.Void), []);

            public override TypeSystemContext Context => owningType.Context;
        }

        protected readonly TypeMapStates _typeMaps;

        protected TypeMapManager(TypeMapStates typeMapStates)
        {
            _typeMaps = typeMapStates;
        }

        public enum TypeMapAttributeKind
        {
            None,
            TypeMapAssemblyTarget,
            TypeMap,
            TypeMapAssociation
        }
        public static TypeMapAttributeKind LookupTypeMapType(TypeDesc attrType)
        {
            TypeDesc typeDef = attrType.GetTypeDefinition();
            return typeDef switch
            {
                MetadataType { Namespace: "System.Runtime.InteropServices", Name: "TypeMapAssemblyTargetAttribute`1", Instantiation.Length: 1 } => TypeMapAttributeKind.TypeMapAssemblyTarget,
                MetadataType { Namespace: "System.Runtime.InteropServices", Name: "TypeMapAttribute`1", Instantiation.Length: 1 } => TypeMapAttributeKind.TypeMap,
                MetadataType { Namespace: "System.Runtime.InteropServices", Name: "TypeMapAssociationAttribute`1", Instantiation.Length: 1 } => TypeMapAttributeKind.TypeMapAssociation,
                _ => TypeMapAttributeKind.None,
            };
        }

        public static TypeMapStates CreateTypeMapStateRootedAtAssembly(EcmaAssembly assembly)
        {
            Dictionary<TypeDesc, TypeMapState> typeMapStates = [];
            HashSet<EcmaAssembly> scannedAssemblies = [];

            Queue<EcmaAssembly> assembliesToScan = new Queue<EcmaAssembly>();
            assembliesToScan.Enqueue(assembly);

            while (assembliesToScan.Count > 0)
            {
                EcmaAssembly currentAssembly = assembliesToScan.Dequeue();
                if (scannedAssemblies.Contains(currentAssembly))
                    continue;

                scannedAssemblies.Add(currentAssembly);

                foreach (CustomAttributeHandle attrHandle in currentAssembly.MetadataReader.GetCustomAttributes(EntityHandle.AssemblyDefinition))
                {
                    CustomAttribute attr = currentAssembly.MetadataReader.GetCustomAttribute(attrHandle);

                    if (!MetadataExtensions.GetAttributeTypeAndConstructor(currentAssembly.MetadataReader, attrHandle, out EntityHandle attributeType, out _))
                    {
                        continue;
                    }

                    TypeDesc type = (TypeDesc)currentAssembly.GetObject(attributeType);

                    TypeMapAttributeKind attrKind = LookupTypeMapType(type);

                    if (attrKind == TypeMapAttributeKind.None)
                    {
                        // Not a type map attribute, skip it
                        continue;
                    }

                    CustomAttributeValue<TypeDesc> attrValue = attr.DecodeValue(new CustomAttributeTypeProvider(currentAssembly));

                    TypeDesc typeMapGroup = type.Instantiation[0];

                    try
                    {
                        switch (attrKind)
                        {
                            case TypeMapAttributeKind.TypeMapAssemblyTarget:
                                ProcessTypeMapAssemblyTargetAttribute(attrValue);
                                break;

                            case TypeMapAttributeKind.TypeMap:
                                ProcessTypeMapAttribute(attrValue, typeMapGroup);
                                break;

                            case TypeMapAttributeKind.TypeMapAssociation:
                                ProcessTypeMapAssociationAttribute(attrValue, typeMapGroup);
                                break;

                            default:
                                Debug.Fail($"Unexpected TypeMapAttributeKind: {attrKind}");
                                break;
                        }
                    }
                    catch (TypeSystemException ex)
                    {
                        if (!typeMapStates.TryGetValue(typeMapGroup, out TypeMapState value))
                        {
                            value = new TypeMapState();
                            typeMapStates[typeMapGroup] = value;
                        }

                        if (attrKind is TypeMapAttributeKind.TypeMapAssemblyTarget or TypeMapAttributeKind.TypeMap)
                        {
                            value.SetExternalTypeMapStub(new ThrowingMethodStub(assembly.GetGlobalModuleType(), typeMapGroup, externalTypeMap: true, ex));
                        }

                        if (attrKind is TypeMapAttributeKind.TypeMapAssemblyTarget or TypeMapAttributeKind.TypeMapAssociation)
                        {
                            value.SetAssociatedTypeMapStub(new ThrowingMethodStub(assembly.GetGlobalModuleType(), typeMapGroup, externalTypeMap: false, ex));
                        }
                    }
                }

                void ProcessTypeMapAssemblyTargetAttribute(CustomAttributeValue<TypeDesc> attrValue)
                {
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
                            if (!typeMapStates.TryGetValue(typeMapGroup, out TypeMapState typeMapState))
                            {
                                typeMapStates[typeMapGroup] = typeMapState = new TypeMapState();
                            }
                            typeMapState.AddExternalTypeMapEntry(typeName, targetType, targetType);
                            break;
                        }

                        case [{ Value: string typeName }, { Value: TypeDesc targetType }, { Value: TypeDesc trimTargetType }]:
                        {
                            if (!typeMapStates.TryGetValue(typeMapGroup, out TypeMapState typeMapState))
                            {
                                typeMapStates[typeMapGroup] = typeMapState = new TypeMapState();
                            }
                            typeMapState.AddExternalTypeMapEntry(typeName, targetType, trimTargetType);
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

                    if (!typeMapStates.TryGetValue(typeMapGroup, out TypeMapState typeMapState))
                    {
                        typeMapStates[typeMapGroup] = typeMapState = new TypeMapState();
                    }

                    typeMapState.AddAssociatedTypeMapEntry(type, associatedType);
                }
            }

            return new TypeMapStates(typeMapStates);
        }

        public void AttachToDependencyGraph(DependencyAnalyzerBase<NodeFactory> graph)
        {
            graph.NewMarkedNode += Graph_NewMarkedNode;
        }

        private readonly SortedSet<ExternalTypeMapNode> _externalTypeMaps = new SortedSet<ExternalTypeMapNode>(CompilerComparer.Instance);
        private readonly SortedSet<InvalidExternalTypeMapNode> _invalidExternalTypeMaps = new SortedSet<InvalidExternalTypeMapNode>(CompilerComparer.Instance);
        private readonly SortedSet<ProxyTypeMapNode> _proxyTypeMaps = new SortedSet<ProxyTypeMapNode>(CompilerComparer.Instance);
        private readonly SortedSet<InvalidProxyTypeMapNode> _invalidProxyTypeMaps = new SortedSet<InvalidProxyTypeMapNode>(CompilerComparer.Instance);

        protected virtual void Graph_NewMarkedNode(DependencyNodeCore<NodeFactory> obj)
        {
            if (obj is ExternalTypeMapNode externalTypeMapNode)
            {
                _externalTypeMaps.Add(externalTypeMapNode);
            }

            if (obj is InvalidExternalTypeMapNode invalidExternalTypeMapNode)
            {
                _invalidExternalTypeMaps.Add(invalidExternalTypeMapNode);
            }

            if (obj is ProxyTypeMapNode proxyTypeMapNode)
            {
                _proxyTypeMaps.Add(proxyTypeMapNode);
            }

            if (obj is InvalidProxyTypeMapNode invalidProxyTypeMapNode)
            {
                _invalidProxyTypeMaps.Add(invalidProxyTypeMapNode);
            }
        }

        internal abstract IEnumerable<ExternalTypeMapNode> GetExternalTypeMaps();

        internal abstract IEnumerable<InvalidExternalTypeMapNode> GetInvalidExternalTypeMaps();

        internal abstract IEnumerable<ProxyTypeMapNode> GetProxyTypeMaps();

        internal abstract IEnumerable<InvalidProxyTypeMapNode> GetInvalidProxyTypeMaps();

        public abstract void AddCompilationRoots(IRootingServiceProvider rootProvider);

        public void AddToReadyToRunHeader(ReadyToRunHeaderNode header, NodeFactory nodeFactory, ExternalReferencesTableNode commonFixupsTableNode)
        {
            if (_typeMaps.IsEmpty)
            {
                return; // No type maps to emit
            }

            header.Add(MetadataManager.BlobIdToReadyToRunSection(ReflectionMapBlob.ExternalTypeMap), new ExternalTypeMapObjectNode(commonFixupsTableNode));
            header.Add(MetadataManager.BlobIdToReadyToRunSection(ReflectionMapBlob.ProxyTypeMap), new ProxyTypeMapObjectNode(commonFixupsTableNode));
        }
    }
}
