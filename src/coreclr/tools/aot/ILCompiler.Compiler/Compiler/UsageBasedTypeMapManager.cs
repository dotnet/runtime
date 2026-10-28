// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using DependencySink = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.DependencySink;
using static ILCompiler.TypeMapMetadata;

namespace ILCompiler
{
    public sealed class UsageBasedTypeMapManager : TypeMapManager
    {
        private readonly SortedSet<IExternalTypeMapNode> _externalTypeMaps = new SortedSet<IExternalTypeMapNode>(CompilerComparer.Instance);
        private readonly SortedSet<IProxyTypeMapNode> _proxyTypeMaps = new SortedSet<IProxyTypeMapNode>(CompilerComparer.Instance);
        private readonly Dictionary<TypeDesc, Map> _typeMaps = [];
        private readonly EcmaAssembly _entryAssembly;

        public UsageBasedTypeMapManager(EcmaAssembly entryAssembly = null)
        {
            _entryAssembly = entryAssembly;
        }

        // Header sections are registered before request-driven discovery takes place.
        protected override bool IsEmpty => false;

        private Map GetMap(NodeFactory factory, TypeDesc typeMapGroup)
        {
            lock (_typeMaps)
            {
                if (!_typeMaps.TryGetValue(typeMapGroup, out Map map))
                {
                    map = TypeMapMetadata.CreateForGroup(_entryAssembly, factory.TypeSystemContext.GeneratedAssembly, typeMapGroup);
                    _typeMaps.Add(typeMapGroup, map);
                }
                return map;
            }
        }

        internal override void AddExternalTypeMapDependencies(DependencySink sink, NodeFactory factory, TypeDesc typeMapGroup)
        {
            IExternalTypeMap map = GetMap(factory, typeMapGroup);
            IExternalTypeMapNode node = map.ThrowingMethodStub is not null
                ? new InvalidExternalTypeMapNode(typeMapGroup, map.ThrowingMethodStub)
                : new ExternalTypeMapNode(typeMapGroup, map.TypeMap);
            sink.Add(node, "ExternalTypeMap");
        }

        internal override void AddProxyTypeMapDependencies(DependencySink sink, NodeFactory factory, TypeDesc typeMapGroup)
        {
            IProxyTypeMap map = GetMap(factory, typeMapGroup);
            IProxyTypeMapNode node = map.ThrowingMethodStub is not null
                ? new InvalidProxyTypeMapNode(typeMapGroup, map.ThrowingMethodStub)
                : new ProxyTypeMapNode(typeMapGroup, map.TypeMap);
            sink.Add(node, "ProxyTypeMap");
        }

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
            => _externalTypeMaps;

        internal override IEnumerable<IProxyTypeMapNode> GetProxyTypeMaps()
            => _proxyTypeMaps;

        public override void AddCompilationRoots(IRootingServiceProvider rootProvider)
        {
        }
    }
}
