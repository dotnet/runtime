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
        }

        internal override IEnumerable<IExternalTypeMapNode> GetExternalTypeMaps()
        {
            return _externalTypeMaps;
        }

        internal override IEnumerable<IProxyTypeMapNode> GetProxyTypeMaps()
        {
            return _proxyTypeMaps;
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
