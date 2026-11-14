// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;
using Internal.IL;
using Internal.IL.Stubs;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using static ILCompiler.TypeMapMetadata;

namespace ILCompiler
{
    public sealed class UsageBasedTypeMapManager : TypeMapManager
    {
        private sealed class AllTypeMapsNode(TypeMapMetadata typeMapState, HashSet<TypeDesc> staticKeyGroups) : DependencyNodeCore<NodeFactory>
        {
            public override bool InterestingForDynamicDependencyAnalysis => false;

            public override bool HasDynamicDependencies => false;

            public override bool HasConditionalStaticDependencies => true;

            public override bool StaticDependenciesAreComputed => true;

            public override void AddConditionalDependencies(DependencySink sink, NodeFactory context)
            {
                foreach ((TypeDesc typeMapGroup, TypeMapMetadata.Map typeMap) in typeMapState.Maps)
                {
                    bool staticKeys = staticKeyGroups.Contains(typeMapGroup);
                    sink.Add(new CombinedDependencyListEntry(GetExternalTypeMapNode(typeMapGroup, typeMap, staticKeys), context.ExternalTypeMapRequest(typeMapGroup), "ExternalTypeMap"));
                    sink.Add(new CombinedDependencyListEntry(GetProxyTypeMapNode(typeMapGroup, typeMap, staticKeys), context.ProxyTypeMapRequest(typeMapGroup), "ProxyTypeMap"));
                }
            }

            public override void AddStaticDependencies(DependencySink sink, NodeFactory context) { }
            public override void SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, DependencySink sink, NodeFactory context) { }
            protected override string GetName(NodeFactory context) => $"Type maps root node: {typeMapState.DiagnosticName}";

            private static IExternalTypeMapNode GetExternalTypeMapNode(TypeDesc typeMapGroup, IExternalTypeMap map, bool staticKeys)
            {
                return map.ThrowingMethodStub is not null
                    ? new InvalidExternalTypeMapNode(typeMapGroup, map.ThrowingMethodStub)
                    : new ExternalTypeMapNode(typeMapGroup, map.TypeMap, staticKeys);
            }

            private static IProxyTypeMapNode GetProxyTypeMapNode(TypeDesc typeMapGroup, IProxyTypeMap map, bool staticKeys)
            {
                return map.ThrowingMethodStub is not null
                    ? new InvalidProxyTypeMapNode(typeMapGroup, map.ThrowingMethodStub)
                    : new ProxyTypeMapNode(typeMapGroup, map.TypeMap, staticKeys);
            }
        }

        private readonly HashSet<TypeDesc> _requestedExternalTypeMaps = [];
        private readonly HashSet<TypeDesc> _requestedProxyTypeMaps = [];
        private readonly SortedSet<IExternalTypeMapNode> _externalTypeMaps = new SortedSet<IExternalTypeMapNode>(CompilerComparer.Instance);
        private readonly SortedSet<IProxyTypeMapNode> _proxyTypeMaps = new SortedSet<IProxyTypeMapNode>(CompilerComparer.Instance);

        private readonly TypeMapMetadata _typeMaps;

        private readonly HashSet<TypeDesc> _staticKeyGroups = [];
        private readonly List<string> _unmatchedStaticKeyGroupSelectors = [];

        /// <param name="state">The type maps.</param>
        /// <param name="staticKeyGroupSelector">
        /// Selects type map groups whose keys are assumed to be created only by code in the compilation.
        /// For these groups, entries are not kept merely because the type loader could create the key
        /// (or the trim target) at runtime.
        /// </param>
        public UsageBasedTypeMapManager(TypeMapMetadata state, TypeMapGroupSelector staticKeyGroupSelector = null)
        {
            _typeMaps = state;

            if (staticKeyGroupSelector is not null)
            {
                bool[] matched = new bool[staticKeyGroupSelector.Selectors.Count];
                foreach ((TypeDesc typeMapGroup, _) in state.Maps)
                {
                    foreach (int selector in staticKeyGroupSelector.GetMatchingSelectors(typeMapGroup))
                    {
                        _staticKeyGroups.Add(typeMapGroup);
                        matched[selector] = true;
                    }
                }

                for (int i = 0; i < matched.Length; i++)
                {
                    string selector = staticKeyGroupSelector.Selectors[i];
                    if (!matched[i] && selector != TypeMapGroupSelector.AllGroups)
                        _unmatchedStaticKeyGroupSelectors.Add(selector);
                }
            }
        }

        /// <summary>
        /// Selectors passed to the constructor that did not select any type map group.
        /// </summary>
        public IReadOnlyList<string> UnmatchedStaticKeyGroupSelectors => _unmatchedStaticKeyGroupSelectors;

        protected override bool IsEmpty => _typeMaps.IsEmpty;

        public override void AttachToDependencyGraph(DependencyAnalyzerBase<NodeFactory> graph)
        {
            base.AttachToDependencyGraph(graph);

            graph.NewMarkedNode += Graph_NewMarkedNode;
        }

        private void Graph_NewMarkedNode(DependencyNodeCore<NodeFactory> obj)
        {
            if (obj is IExternalTypeMapNode externalTypeMapNode)
            {
                _externalTypeMaps.Add(externalTypeMapNode);
            }

            if (obj is IProxyTypeMapNode proxyTypeMapNode)
            {
                _proxyTypeMaps.Add(proxyTypeMapNode);
            }

            if (obj is ExternalTypeMapRequestNode externalTypeMapRequestNode)
            {
                _requestedExternalTypeMaps.Add(externalTypeMapRequestNode.TypeMapGroup);
            }

            if (obj is ProxyTypeMapRequestNode proxyTypeMapRequestNode)
            {
                _requestedProxyTypeMaps.Add(proxyTypeMapRequestNode.TypeMapGroup);
            }
        }

        internal override IEnumerable<IExternalTypeMapNode> GetExternalTypeMaps()
        {
            List<IExternalTypeMapNode> typeMaps = [.._externalTypeMaps];
            SortedSet<TypeDesc> generatedMaps = new(TypeSystemComparer.Instance);
            foreach (var generatedMap in typeMaps)
            {
                generatedMaps.Add(generatedMap.TypeMapGroup);
            }

            SortedSet<TypeDesc> emptyMapsToGenerate = new SortedSet<TypeDesc>(_requestedExternalTypeMaps, TypeSystemComparer.Instance);
            emptyMapsToGenerate.ExceptWith(generatedMaps);

            foreach (var emptyMap in emptyMapsToGenerate)
            {
                typeMaps.Add(new ExternalTypeMapNode(emptyMap, []));
            }
            return typeMaps;
        }

        internal override IEnumerable<IProxyTypeMapNode> GetProxyTypeMaps()
        {
            List<IProxyTypeMapNode> typeMaps = [.. _proxyTypeMaps];
            SortedSet<TypeDesc> generatedMaps = new(TypeSystemComparer.Instance);
            foreach (var generatedMap in typeMaps)
            {
                generatedMaps.Add(generatedMap.TypeMapGroup);
            }

            SortedSet<TypeDesc> emptyMapsToGenerate = new SortedSet<TypeDesc>(_requestedProxyTypeMaps, TypeSystemComparer.Instance);
            emptyMapsToGenerate.ExceptWith(generatedMaps);

            foreach (var emptyMap in emptyMapsToGenerate)
            {
                typeMaps.Add(new ProxyTypeMapNode(emptyMap, []));
            }
            return typeMaps;
        }

        public override void AddCompilationRoots(IRootingServiceProvider rootProvider)
        {
            if (_typeMaps.IsEmpty)
            {
                return; // No type maps to process
            }

            rootProvider.AddCompilationRoot(new AllTypeMapsNode(_typeMaps, _staticKeyGroups), "TypeMapManager");
        }
    }
}
