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

namespace ILCompiler
{
    public sealed class UsageBasedTypeMapManager : TypeMapManager
    {
        private sealed class AllTypeMapsNode(TypeMapMetadata typeMapState) : DependencyNodeCore<NodeFactory>
        {
            public override bool InterestingForDynamicDependencyAnalysis => false;

            public override bool HasDynamicDependencies => false;

            public override bool HasConditionalStaticDependencies => true;

            public override bool StaticDependenciesAreComputed => true;

            public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context)
            {
                List<CombinedDependencyListEntry> entries = [];
                foreach ((TypeDesc typeMapGroup, TypeMapMetadata.Map typeMap) in typeMapState.Maps)
                {
                    entries.Add(new CombinedDependencyListEntry(typeMap.GetExternalTypeMapNode(), context.ExternalTypeMapRequest(typeMapGroup), "ExternalTypeMap"));
                    entries.Add(new CombinedDependencyListEntry(typeMap.GetProxyTypeMapNode(), context.ProxyTypeMapRequest(typeMapGroup), "ProxyTypeMap"));
                }

                return entries;
            }

            public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context) => Array.Empty<DependencyListEntry>();
            public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => Array.Empty<CombinedDependencyListEntry>();
            protected override string GetName(NodeFactory context) => $"Type maps root node: {typeMapState.DiagnosticName}";
        }

        private readonly HashSet<TypeDesc> _requestedExternalTypeMaps = [];
        private readonly HashSet<TypeDesc> _requestedProxyTypeMaps = [];
        private readonly SortedSet<IExternalTypeMapNode> _externalTypeMaps = new SortedSet<IExternalTypeMapNode>(CompilerComparer.Instance);
        private readonly SortedSet<IProxyTypeMapNode> _proxyTypeMaps = new SortedSet<IProxyTypeMapNode>(CompilerComparer.Instance);

        private readonly TypeMapMetadata _typeMaps;

        public UsageBasedTypeMapManager(TypeMapMetadata state)
        {
            _typeMaps = state;
        }

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

            rootProvider.AddCompilationRoot(new AllTypeMapsNode(_typeMaps), "TypeMapManager");
        }
    }
}
