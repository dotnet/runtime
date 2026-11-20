// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    // A type equality check is not, by itself, evidence that its literal type is needed.
    // Independent handle or metadata reachability upgrades the check. Otherwise its uses
    // must be removed from the IL supplied to code generation.
    internal sealed class ConditionalTypeReferenceNode(MetadataType type) : DependencyNodeCore<NodeFactory>
    {
        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => true;
        public override bool StaticDependenciesAreComputed => true;
        public override void AddStaticDependencies(DependencySink sink, NodeFactory context) { }

        public override void AddConditionalDependencies(DependencySink sink, NodeFactory context)
        {
            if (!context.MetadataManager.IsReflectionBlocked(type))
                sink.AddConditional(context.NecessaryTypeSymbol(type), context.TypeMetadata(type),
                    "Type comparison upgraded by independently reachable metadata");
        }

        public override void SearchDynamicDependencies(
            List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, DependencySink sink, NodeFactory context) { }
        protected override string GetName(NodeFactory context) => $"Conditional type reference: {type}";
    }
}
