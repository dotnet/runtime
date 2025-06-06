// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    public sealed class UsageBasedTypeMapManager(TypeMapManager.TypeMapStates state) : TypeMapManager(state)
    {
        private sealed class TypeMapsNode(TypeMapStates typeMapState) : DependencyNodeCore<NodeFactory>
        {
            public override bool InterestingForDynamicDependencyAnalysis => false;

            public override bool HasDynamicDependencies => false;

            public override bool HasConditionalStaticDependencies => true;

            public override bool StaticDependenciesAreComputed => true;

            public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context)
            {
                List<CombinedDependencyListEntry> entries = [];
                foreach ((TypeDesc typeMapGroup, TypeMapState typeMapState) in typeMapState.States)
                {
                    entries.Add(new CombinedDependencyListEntry(typeMapState.GetExternalTypeMapNode(typeMapGroup), context.ExternalTypeMapRequest(typeMapGroup), "ExternalTypeMap"));
                    entries.Add(new CombinedDependencyListEntry(typeMapState.GetProxyTypeMapNode(typeMapGroup), context.ProxyTypeMapRequest(typeMapGroup), "ProxyTypeMap"));
                }

                return entries;
            }

            public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context) => Array.Empty<DependencyListEntry>();
            public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => Array.Empty<CombinedDependencyListEntry>();
            protected override string GetName(NodeFactory context) => "TypeMapsNode";
        }

        private readonly List<TypeDesc> _usedExternalTypeMap = new List<TypeDesc>();
        private readonly List<TypeDesc> _usedProxyTypeMap = new List<TypeDesc>();

        private readonly SortedSet<ExternalTypeMapNode> _externalTypeMaps = new SortedSet<ExternalTypeMapNode>(CompilerComparer.Instance);
        private readonly SortedSet<InvalidExternalTypeMapNode> _invalidExternalTypeMaps = new SortedSet<InvalidExternalTypeMapNode>(CompilerComparer.Instance);
        private readonly SortedSet<ProxyTypeMapNode> _proxyTypeMaps = new SortedSet<ProxyTypeMapNode>(CompilerComparer.Instance);
        private readonly SortedSet<InvalidProxyTypeMapNode> _invalidProxyTypeMaps = new SortedSet<InvalidProxyTypeMapNode>(CompilerComparer.Instance);

        protected override void Graph_NewMarkedNode(DependencyNodeCore<NodeFactory> obj)
        {
            base.Graph_NewMarkedNode(obj);

            if (obj is ExternalTypeMapRequestNode externalTypeMapRequest)
            {
                _usedExternalTypeMap.Add(externalTypeMapRequest.TypeMapGroup);
            }

            if (obj is ProxyTypeMapRequestNode proxyTypeMapRequestNode)
            {
                _usedProxyTypeMap.Add(proxyTypeMapRequestNode.TypeMapGroup);
            }

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
        internal override IEnumerable<ExternalTypeMapNode> GetExternalTypeMaps()
        {
            return _externalTypeMaps;
        }

        internal override IEnumerable<InvalidExternalTypeMapNode> GetInvalidExternalTypeMaps()
        {
            return _invalidExternalTypeMaps;
        }

        internal override IEnumerable<ProxyTypeMapNode> GetProxyTypeMaps()
        {
            return _proxyTypeMaps;
        }

        internal override IEnumerable<InvalidProxyTypeMapNode> GetInvalidProxyTypeMaps()
        {
            return _invalidProxyTypeMaps;
        }

        public override void AddCompilationRoots(IRootingServiceProvider rootProvider)
        {
            if (_typeMaps.IsEmpty)
            {
                return; // No type maps to process
            }

            rootProvider.AddCompilationRoot(new TypeMapsNode(_typeMaps), "TypeMapManager");
        }

        public AnalysisBasedTypeMapManager ToAnalysisBasedTypeMapManager()
        {
            return new AnalysisBasedTypeMapManager(_typeMaps, _usedExternalTypeMap, _usedProxyTypeMap);
        }
    }
}
