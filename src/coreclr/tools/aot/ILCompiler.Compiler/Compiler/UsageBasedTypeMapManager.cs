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
    public sealed class UsageBasedTypeMapManager : TypeMapManager
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

        public UsageBasedTypeMapManager(TypeMapStates state) : base(state) { }

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
