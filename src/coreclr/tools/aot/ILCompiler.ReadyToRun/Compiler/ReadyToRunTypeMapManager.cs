// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysis.ReadyToRun;
using ILCompiler.DependencyAnalysisFramework;
using ILCompiler.ReadyToRun.TypeSystem;
using Internal.Runtime;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.ReadyToRun
{
    public sealed class ReadyToRunTypeMapManager(ModuleDesc triggeringModule, TypeMapMetadata assemblyTypeMaps) : TypeMapManager
    {
        private ImportReferenceProvider _importReferenceProvider;
        private readonly HashSet<TypeDesc> _externalTypeMapsRequiringRuntimeProcessing = [];
        private readonly HashSet<TypeDesc> _proxyTypeMapsRequiringRuntimeProcessing = [];

        public override ModuleDesc AssociatedModule => triggeringModule;

        public override void AttachToDependencyGraph(DependencyAnalyzerBase<NodeFactory> graph)
        {
            if (IsEmpty)
                return;

            base.AttachToDependencyGraph(graph);
            foreach (var map in GetExternalTypeMaps())
            {
                graph.AddRoot(map, "External type map");
            }
            foreach (var map in GetProxyTypeMaps())
            {
                graph.AddRoot(map, "Proxy type map");
            }
        }

        protected override bool IsEmpty => assemblyTypeMaps.IsEmpty;

        public override void AddCompilationRoots(IRootingServiceProvider rootProvider)
        {
        }

        internal override IEnumerable<IExternalTypeMapNode> GetExternalTypeMaps()
        {
            foreach (var map in assemblyTypeMaps.Maps)
            {
                yield return new ReadyToRunExternalTypeMapNode(
                    triggeringModule,
                    map.Key,
                    map.Value,
                    _importReferenceProvider,
                    _externalTypeMapsRequiringRuntimeProcessing.Contains(map.Key));
            }
        }

        internal override IEnumerable<IProxyTypeMapNode> GetProxyTypeMaps()
        {
            foreach (var map in assemblyTypeMaps.Maps)
            {
                yield return new ReadyToRunProxyTypeMapNode(
                    triggeringModule,
                    map.Key,
                    map.Value,
                    _importReferenceProvider,
                    _proxyTypeMapsRequiringRuntimeProcessing.Contains(map.Key));
            }
        }

        public void AddToReadyToRunHeader(ReadyToRunHeaderNode header, NodeFactory nodeFactory, ImportReferenceProvider importReferenceProvider)
        {
            _importReferenceProvider = importReferenceProvider;

            if (IsEmpty)
                return;

            PrepareTypeMapsForEncoding(nodeFactory);

            header.Add(ReadyToRunSectionType.ExternalTypeMaps, new ExternalTypeMapObjectNode(this, importReferenceProvider));
            header.Add(ReadyToRunSectionType.ProxyTypeMaps, new ProxyTypeMapObjectNode(this, importReferenceProvider));
            header.Add(ReadyToRunSectionType.TypeMapAssemblyTargets, new TypeMapAssemblyTargetsNode(assemblyTypeMaps, importReferenceProvider));
        }

        // Some types referenced by TypeMap attributes may not have an existing TypeRef/AssemblyRef relationship to the
        // module that declared the attribute. Mark just the affected (TypeMapGroup, ProxyOrExternalMap) entry for
        // runtime attribute processing.
        private void PrepareTypeMapsForEncoding(NodeFactory nodeFactory)
        {
            foreach (var mapEntry in assemblyTypeMaps.Maps)
            {
                TypeMapMetadata.Map map = mapEntry.Value;

                // The generic TypeMap attribute TypeSpec necessarily provides an encodable metadata
                // reference to its group type.
                Debug.Assert(CanEncodeReferenceToType(mapEntry.Key));

                TypeMapMetadata.IExternalTypeMap externalTypeMap = map;
                if (externalTypeMap.ThrowingMethodStub is null)
                {
                    foreach ((TypeDesc type, _) in externalTypeMap.TypeMap.Values)
                    {
                        if (!CanEncodeReferenceToType(type))
                        {
                            _externalTypeMapsRequiringRuntimeProcessing.Add(mapEntry.Key);
                            break;
                        }
                    }
                }

                TypeMapMetadata.IProxyTypeMap proxyTypeMap = map;
                if (proxyTypeMap.ThrowingMethodStub is null)
                {
                    foreach (KeyValuePair<TypeDesc, TypeDesc> typeMapEntry in proxyTypeMap.TypeMap)
                    {
                        if (!CanEncodeReferenceToType(typeMapEntry.Key) ||
                            !CanEncodeReferenceToType(typeMapEntry.Value))
                        {
                            _proxyTypeMapsRequiringRuntimeProcessing.Add(mapEntry.Key);
                            break;
                        }
                    }
                }
            }

            bool CanEncodeReferenceToType(TypeDesc type)
            {
                if (nodeFactory.CompilationModuleGroup.VersionsWithTypeReference(type))
                    return true;

                if (type is EcmaType ecmaType)
                {
                    return MutableModule.CanCreateReferenceToType(
                        triggeringModule,
                        ecmaType,
                        (ReadyToRunCompilationModuleGroupBase)nodeFactory.CompilationModuleGroup);
                }

                if (type.IsParameterizedType)
                {
                    return CanEncodeReferenceToType(((ParameterizedType)type).ParameterType);
                }

                if (type.IsFunctionPointer)
                {
                    MethodSignature signature = ((FunctionPointerType)type).Signature;

                    if (!CanEncodeReferenceToType(signature.ReturnType))
                        return false;

                    for (int i = 0; i < signature.Length; i++)
                    {
                        if (!CanEncodeReferenceToType(signature[i]))
                            return false;
                    }

                    return true;
                }

                if (type.HasInstantiation)
                {
                    if (!CanEncodeReferenceToType(type.GetTypeDefinition()))
                        return false;

                    foreach (TypeDesc instantiationArgument in type.Instantiation)
                    {
                        if (!CanEncodeReferenceToType(instantiationArgument))
                            return false;
                    }

                    return true;
                }

                // Generic parameters (encoded as ELEMENT_TYPE_VAR/MVAR) and other simple type
                // shapes that don't reference any other type by name always encode safely.
                // Anything else reaching here is an unexpected TypeDesc shape for a TypeMap
                // key or value; treat it as unencodable rather than assuming successful encoding.
                return type.IsSignatureVariable;
            }
        }
    }
}
