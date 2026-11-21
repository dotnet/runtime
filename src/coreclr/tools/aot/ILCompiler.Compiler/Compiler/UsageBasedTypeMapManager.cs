// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
        private sealed class AllTypeMapsNode(TypeMapMetadata typeMapState) : DependencyNodeCore<NodeFactory>
        {
            public override bool InterestingForDynamicDependencyAnalysis => false;

            public override bool HasDynamicDependencies => false;

            public override bool HasConditionalStaticDependencies => true;

            public override bool StaticDependenciesAreComputed => true;

            public override void AddConditionalDependencies(DependencySink sink, NodeFactory context)
            {
                foreach ((TypeDesc typeMapGroup, TypeMapMetadata.Map typeMap) in typeMapState.Maps)
                {
                    // The mapped types are only generated if the program asks for them. The typed values are requested separately (see ExternalTypeMapValuesRequestNode).
                    sink.Add(new CombinedDependencyListEntry(GetExternalTypeMapNode(typeMapGroup, typeMap), context.ExternalTypeMapRequest(typeMapGroup), "ExternalTypeMap"));
                    sink.Add(new CombinedDependencyListEntry(GetProxyTypeMapNode(typeMapGroup, typeMap), context.ProxyTypeMapRequest(typeMapGroup), "ProxyTypeMap"));
                }
            }

            public override void AddStaticDependencies(DependencySink sink, NodeFactory context) { }
            public override void SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, DependencySink sink, NodeFactory context) { }
            protected override string GetName(NodeFactory context) => $"Type maps root node: {typeMapState.DiagnosticName}";

            private static IExternalTypeMapNode GetExternalTypeMapNode(TypeDesc typeMapGroup, IExternalTypeMap map)
            {
                return map.ThrowingMethodStub is not null
                    ? new InvalidExternalTypeMapNode(typeMapGroup, map.ThrowingMethodStub)
                    : new ExternalTypeMapNode(typeMapGroup, map.TypeMap);
            }

            private static IProxyTypeMapNode GetProxyTypeMapNode(TypeDesc typeMapGroup, IProxyTypeMap map)
            {
                return map.ThrowingMethodStub is not null
                    ? new InvalidProxyTypeMapNode(typeMapGroup, map.ThrowingMethodStub)
                    : new ProxyTypeMapNode(typeMapGroup, map.TypeMap);
            }
        }

        private readonly Dictionary<TypeDesc, TypeMapFlavors> _requestedExternalTypeMaps = [];
        private readonly Dictionary<TypeDesc, TypeMapFlavors> _requestedProxyTypeMaps = [];
        private readonly Dictionary<TypeDesc, SortedSet<TypeDesc>> _requestedExternalValueTypes = [];
        private readonly Dictionary<TypeDesc, SortedSet<TypeDesc>> _requestedProxyValueTypes = [];

        // The values of the typed value flavors are evaluated once, by whatever needs them first. What is evaluated is what ends up in the image.
        private readonly ConcurrentDictionary<(TypeDesc Target, TypeDesc ValueType), TypeMapValueEvaluation> _valueEvaluations = new();
        private readonly ConcurrentDictionary<(TypeDesc TypeMapGroup, TypeDesc ValueType), DependencyNodeCore<NodeFactory>> _externalValueFlavorNodes = new();
        private readonly ConcurrentDictionary<(TypeDesc TypeMapGroup, TypeDesc ValueType), DependencyNodeCore<NodeFactory>> _proxyValueFlavorNodes = new();

        private readonly SortedSet<IExternalTypeMapNode> _externalTypeMaps = new SortedSet<IExternalTypeMapNode>(CompilerComparer.Instance);
        private readonly SortedSet<IProxyTypeMapNode> _proxyTypeMaps = new SortedSet<IProxyTypeMapNode>(CompilerComparer.Instance);

        private readonly TypeMapMetadata _typeMaps;

        // Where to report the getters of typed values that are not evaluated at compile time. The getters are always reported in verbose mode.
        private readonly Logger _logger;

        public UsageBasedTypeMapManager(TypeMapMetadata state, Logger logger = null)
        {
            _typeMaps = state;
            _logger = logger;
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
                _requestedExternalTypeMaps[externalTypeMapRequestNode.TypeMapGroup] =
                    _requestedExternalTypeMaps.GetValueOrDefault(externalTypeMapRequestNode.TypeMapGroup) | TypeMapFlavors.Types;
            }

            if (obj is ProxyTypeMapRequestNode proxyTypeMapRequestNode)
            {
                _requestedProxyTypeMaps[proxyTypeMapRequestNode.TypeMapGroup] =
                    _requestedProxyTypeMaps.GetValueOrDefault(proxyTypeMapRequestNode.TypeMapGroup) | TypeMapFlavors.Types;
            }

            if (obj is ExternalTypeMapValuesRequestNode externalValuesRequest)
            {
                _requestedExternalTypeMaps[externalValuesRequest.TypeMapGroup] =
                    _requestedExternalTypeMaps.GetValueOrDefault(externalValuesRequest.TypeMapGroup) | TypeMapFlavors.Values;
                if (!_requestedExternalValueTypes.TryGetValue(externalValuesRequest.TypeMapGroup, out SortedSet<TypeDesc> valueTypes))
                    _requestedExternalValueTypes.Add(externalValuesRequest.TypeMapGroup, valueTypes = new SortedSet<TypeDesc>(TypeSystemComparer.Instance));
                valueTypes.Add(externalValuesRequest.ValueType);
            }

            if (obj is ProxyTypeMapValuesRequestNode proxyValuesRequest)
            {
                _requestedProxyTypeMaps[proxyValuesRequest.TypeMapGroup] =
                    _requestedProxyTypeMaps.GetValueOrDefault(proxyValuesRequest.TypeMapGroup) | TypeMapFlavors.Values;
                if (!_requestedProxyValueTypes.TryGetValue(proxyValuesRequest.TypeMapGroup, out SortedSet<TypeDesc> valueTypes))
                    _requestedProxyValueTypes.Add(proxyValuesRequest.TypeMapGroup, valueTypes = new SortedSet<TypeDesc>(TypeSystemComparer.Instance));
                valueTypes.Add(proxyValuesRequest.ValueType);
            }
        }

        private TypeMapValueEvaluation GetValueEvaluation(NodeFactory factory, TypeDesc target, TypeDesc valueType)
        {
            return _valueEvaluations.GetOrAdd((target, valueType), key =>
            {
                TypeMapValueEvaluation evaluation = TypeMapValueSupport.Evaluate(factory, key.Target, key.ValueType);
                if (evaluation.FallbackReason is not null && _logger is { IsVerbose: true })
                {
                    _logger.LogMessage($"Type map value getter of '{key.Target}' for '{key.ValueType}' is not evaluated at compile time: {evaluation.FallbackReason}");
                }

                return evaluation;
            });
        }

        private Dictionary<TypeDesc, TypeMapMetadata.Map> _mapsByGroup;

        private bool TryGetMap(TypeDesc typeMapGroup, out TypeMapMetadata.Map map)
        {
            _mapsByGroup ??= _typeMaps.Maps.ToDictionary(m => m.Key, m => m.Value);
            return _mapsByGroup.TryGetValue(typeMapGroup, out map);
        }

        private TypeMapValueFlavor CreateValueFlavor(NodeFactory factory, TypeDesc valueType)
            => new TypeMapValueFlavor(valueType, target => GetValueEvaluation(factory, target, valueType));

        /// <summary>
        /// Gets the node that describes the values of the external type map of the group for <paramref name="valueType"/>.
        /// Returns null if the group doesn't have a type map.
        /// </summary>
        internal DependencyNodeCore<NodeFactory> GetExternalValueFlavorNode(NodeFactory factory, TypeDesc typeMapGroup, TypeDesc valueType)
        {
            if (!TryGetMap(typeMapGroup, out TypeMapMetadata.Map map))
                return null;

            return _externalValueFlavorNodes.GetOrAdd((typeMapGroup, valueType), key =>
            {
                IExternalTypeMap externalMap = map;
                return externalMap.ThrowingMethodStub is not null
                    ? (DependencyNodeCore<NodeFactory>)new InvalidExternalTypeMapNode(typeMapGroup, externalMap.ThrowingMethodStub)
                    : new ExternalTypeMapNode(typeMapGroup, externalMap.TypeMap, TypeMapFlavors.Values, [CreateValueFlavor(factory, valueType)]);
            });
        }

        /// <summary>
        /// Gets the node that describes the values of the proxy type map of the group for <paramref name="valueType"/>.
        /// Returns null if the group doesn't have a type map.
        /// </summary>
        internal DependencyNodeCore<NodeFactory> GetProxyValueFlavorNode(NodeFactory factory, TypeDesc typeMapGroup, TypeDesc valueType)
        {
            if (!TryGetMap(typeMapGroup, out TypeMapMetadata.Map map))
                return null;

            return _proxyValueFlavorNodes.GetOrAdd((typeMapGroup, valueType), key =>
            {
                IProxyTypeMap proxyMap = map;
                return proxyMap.ThrowingMethodStub is not null
                    ? (DependencyNodeCore<NodeFactory>)new InvalidProxyTypeMapNode(typeMapGroup, proxyMap.ThrowingMethodStub)
                    : new ProxyTypeMapNode(typeMapGroup, proxyMap.TypeMap, TypeMapFlavors.Values, [CreateValueFlavor(factory, valueType)]);
            });
        }

        // The graph has a node per flavor of a group. All flavors of a group have the same entries under the
        // same conditions, so for emission they are merged into a single node that makes one hash table serve all of them.
        // If there are no flavors other than the mapped types, the nodes are emitted as they are.
        internal override IEnumerable<IExternalTypeMapNode> GetExternalTypeMaps()
        {
            List<IExternalTypeMapNode> typeMaps = [.._externalTypeMaps];

            Dictionary<TypeDesc, TypeMapFlavors> groupFlavors = [];
            Dictionary<TypeDesc, List<TypeMapValueFlavor>> groupValueFlavors = [];
            foreach (IExternalTypeMapNode node in typeMaps)
            {
                groupFlavors[node.TypeMapGroup] = groupFlavors.GetValueOrDefault(node.TypeMapGroup) | ((node as ExternalTypeMapNode)?.Flavors ?? TypeMapFlavors.None);
                if (node is ExternalTypeMapNode { ValueFlavors.Count: > 0 } withValues)
                {
                    if (!groupValueFlavors.TryGetValue(withValues.TypeMapGroup, out List<TypeMapValueFlavor> valueFlavors))
                        groupValueFlavors.Add(withValues.TypeMapGroup, valueFlavors = []);
                    valueFlavors.AddRange(withValues.ValueFlavors);
                }
            }

            if (groupFlavors.Values.Any(f => (f & TypeMapFlavors.Values) != 0))
            {
                List<IExternalTypeMapNode> merged = [];
                HashSet<TypeDesc> mergedGroups = [];
                foreach (IExternalTypeMapNode node in typeMaps)
                {
                    if (node is ExternalTypeMapNode flavored && (groupFlavors[flavored.TypeMapGroup] != flavored.Flavors || groupValueFlavors.GetValueOrDefault(flavored.TypeMapGroup)?.Count > flavored.ValueFlavors.Count))
                    {
                        if (mergedGroups.Add(flavored.TypeMapGroup))
                            merged.Add(new ExternalTypeMapNode(flavored.TypeMapGroup, flavored.MapEntries, groupFlavors[flavored.TypeMapGroup], groupValueFlavors.GetValueOrDefault(flavored.TypeMapGroup)));
                    }
                    else
                    {
                        merged.Add(node);
                    }
                }

                typeMaps = merged;
            }

            SortedSet<TypeDesc> generatedMaps = new(TypeSystemComparer.Instance);
            foreach (var generatedMap in typeMaps)
            {
                generatedMaps.Add(generatedMap.TypeMapGroup);
            }

            SortedSet<TypeDesc> emptyMapsToGenerate = new SortedSet<TypeDesc>(_requestedExternalTypeMaps.Keys, TypeSystemComparer.Instance);
            emptyMapsToGenerate.ExceptWith(generatedMaps);

            foreach (var emptyMap in emptyMapsToGenerate)
            {
                typeMaps.Add(new ExternalTypeMapNode(emptyMap, [], _requestedExternalTypeMaps[emptyMap], GetEmptyMapValueFlavors(_requestedExternalValueTypes, emptyMap)));
            }
            return typeMaps;
        }

        internal override IEnumerable<IProxyTypeMapNode> GetProxyTypeMaps()
        {
            List<IProxyTypeMapNode> typeMaps = [.. _proxyTypeMaps];

            Dictionary<TypeDesc, TypeMapFlavors> groupFlavors = [];
            Dictionary<TypeDesc, List<TypeMapValueFlavor>> groupValueFlavors = [];
            foreach (IProxyTypeMapNode node in typeMaps)
            {
                groupFlavors[node.TypeMapGroup] = groupFlavors.GetValueOrDefault(node.TypeMapGroup) | ((node as ProxyTypeMapNode)?.Flavors ?? TypeMapFlavors.None);
                if (node is ProxyTypeMapNode { ValueFlavors.Count: > 0 } withValues)
                {
                    if (!groupValueFlavors.TryGetValue(withValues.TypeMapGroup, out List<TypeMapValueFlavor> valueFlavors))
                        groupValueFlavors.Add(withValues.TypeMapGroup, valueFlavors = []);
                    valueFlavors.AddRange(withValues.ValueFlavors);
                }
            }

            if (groupFlavors.Values.Any(f => (f & TypeMapFlavors.Values) != 0))
            {
                List<IProxyTypeMapNode> merged = [];
                HashSet<TypeDesc> mergedGroups = [];
                foreach (IProxyTypeMapNode node in typeMaps)
                {
                    if (node is ProxyTypeMapNode flavored && (groupFlavors[flavored.TypeMapGroup] != flavored.Flavors || groupValueFlavors.GetValueOrDefault(flavored.TypeMapGroup)?.Count > flavored.ValueFlavors.Count))
                    {
                        if (mergedGroups.Add(flavored.TypeMapGroup))
                            merged.Add(new ProxyTypeMapNode(flavored.TypeMapGroup, flavored.MapEntries, groupFlavors[flavored.TypeMapGroup], groupValueFlavors.GetValueOrDefault(flavored.TypeMapGroup)));
                    }
                    else
                    {
                        merged.Add(node);
                    }
                }

                typeMaps = merged;
            }

            SortedSet<TypeDesc> generatedMaps = new(TypeSystemComparer.Instance);
            foreach (var generatedMap in typeMaps)
            {
                generatedMaps.Add(generatedMap.TypeMapGroup);
            }

            SortedSet<TypeDesc> emptyMapsToGenerate = new SortedSet<TypeDesc>(_requestedProxyTypeMaps.Keys, TypeSystemComparer.Instance);
            emptyMapsToGenerate.ExceptWith(generatedMaps);

            foreach (var emptyMap in emptyMapsToGenerate)
            {
                typeMaps.Add(new ProxyTypeMapNode(emptyMap, [], _requestedProxyTypeMaps[emptyMap], GetEmptyMapValueFlavors(_requestedProxyValueTypes, emptyMap)));
            }
            return typeMaps;
        }

        // A group without entries still has to describe the typed value flavors the program asked for.
        private static IReadOnlyList<TypeMapValueFlavor> GetEmptyMapValueFlavors(Dictionary<TypeDesc, SortedSet<TypeDesc>> requestedValueTypes, TypeDesc typeMapGroup)
        {
            if (!requestedValueTypes.TryGetValue(typeMapGroup, out SortedSet<TypeDesc> valueTypes))
                return null;

            return [.. valueTypes.Select(valueType => new TypeMapValueFlavor(valueType, target => throw new InvalidOperationException("An empty type map does not have values")))];
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
