// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// The program asks for the typed values of type <paramref name="valueType"/> of the external type map of a group (TypeMapping.TryGetValue).
    /// </summary>
    public sealed class ExternalTypeMapValuesRequestNode(TypeDesc typeMapGroup, TypeDesc valueType) : DependencyNodeCore<NodeFactory>
    {
        public TypeDesc TypeMapGroup { get; } = typeMapGroup;
        public TypeDesc ValueType { get; } = valueType;
        public override bool InterestingForDynamicDependencyAnalysis => false;

        public override bool HasDynamicDependencies => false;

        public override bool HasConditionalStaticDependencies => false;

        public override bool StaticDependenciesAreComputed => true;

        public override void AddConditionalDependencies(DependencySink sink, NodeFactory context) { }
        public override void AddStaticDependencies(DependencySink sink, NodeFactory context)
        {
            // The type map tables refer to the group type, and the image refers to the value type (the data table of the flavor is described by it).
            sink.Add(context.NecessaryTypeSymbol(TypeMapGroup), "Type map group");
            sink.Add(context.NecessaryTypeSymbol(ValueType), "Type map value type");

            DependencyNodeCore<NodeFactory> valueFlavorNode = (context.TypeMapManager as UsageBasedTypeMapManager)?.GetExternalValueFlavorNode(context, TypeMapGroup, ValueType);
            if (valueFlavorNode is not null)
                sink.Add(valueFlavorNode, "Type map group typed values");
        }
        public override void SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, DependencySink sink, NodeFactory context) { }
        protected override string GetName(NodeFactory context) => $"External type map values request: {TypeMapGroup}, {ValueType}";
    }

    /// <summary>
    /// The program asks for the typed values of type <paramref name="valueType"/> of the proxy type map of a group (TypeMapping.TryGetValue).
    /// </summary>
    public sealed class ProxyTypeMapValuesRequestNode(TypeDesc typeMapGroup, TypeDesc valueType) : DependencyNodeCore<NodeFactory>
    {
        public TypeDesc TypeMapGroup { get; } = typeMapGroup;
        public TypeDesc ValueType { get; } = valueType;
        public override bool InterestingForDynamicDependencyAnalysis => false;

        public override bool HasDynamicDependencies => false;

        public override bool HasConditionalStaticDependencies => false;

        public override bool StaticDependenciesAreComputed => true;

        public override void AddConditionalDependencies(DependencySink sink, NodeFactory context) { }
        public override void AddStaticDependencies(DependencySink sink, NodeFactory context)
        {
            // The type map tables refer to the group type, and the image refers to the value type (the data table of the flavor is described by it).
            sink.Add(context.NecessaryTypeSymbol(TypeMapGroup), "Type map group");
            sink.Add(context.NecessaryTypeSymbol(ValueType), "Type map value type");

            DependencyNodeCore<NodeFactory> valueFlavorNode = (context.TypeMapManager as UsageBasedTypeMapManager)?.GetProxyValueFlavorNode(context, TypeMapGroup, ValueType);
            if (valueFlavorNode is not null)
                sink.Add(valueFlavorNode, "Type map group typed values");
        }
        public override void SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, DependencySink sink, NodeFactory context) { }
        protected override string GetName(NodeFactory context) => $"Proxy type map values request: {TypeMapGroup}, {ValueType}";
    }
}
