// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a custom attribute that has metadata generated in the current compilation.
    /// </summary>
    /// <remarks>
    /// Only expected to be used during ILScanning when scanning for reflection.
    /// </remarks>
    internal sealed class CustomAttributeMetadataNode : DependencyNodeCore<NodeFactory>
    {
        private readonly ReflectableCustomAttribute _customAttribute;

        public CustomAttributeMetadataNode(ReflectableCustomAttribute customAttribute)
        {
            _customAttribute = customAttribute;
        }

        public ReflectableCustomAttribute CustomAttribute => _customAttribute;

        // The metadata that the attribute depends on gets injected into the entity that owns the attribute.
        // This makes the dependency graph less "nice", but it avoids either having to walk the attribute
        // blob twice, or wasting memory holding on to dependencies here.
        //
        // We need to walk the dependencies before placing the node into the graph to find out whether
        // the attribute even can be generated (does it refer to blocked types or something like that?).
        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory) => null;

        protected override string GetName(NodeFactory factory)
        {
            return $"Reflectable custom attribute {_customAttribute.CustomAttributeHandle} in {_customAttribute.Module}";
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory) => null;
    }
}
