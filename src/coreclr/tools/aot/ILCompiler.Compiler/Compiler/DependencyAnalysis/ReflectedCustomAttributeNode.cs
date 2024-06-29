// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents dependencies necessary to activate a custom attribute at runtime.
    /// </summary>
    internal sealed class ReflectedCustomAttributeNode : DependencyNodeCore<NodeFactory>
    {
        private readonly ReflectableCustomAttribute _customAttribute;

        public ReflectedCustomAttributeNode(ReflectableCustomAttribute customAttribute)
        {
            _customAttribute = customAttribute;
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            DependencyList dependencies = null;
            CustomAttributeBasedDependencyAlgorithm.AddDependenciesDueToCustomAttributeActivation(ref dependencies, factory, _customAttribute.Module, _customAttribute.CustomAttributeHandle);
            return dependencies;
        }

        protected override string GetName(NodeFactory factory)
        {
            return $"Custom attribute activation {_customAttribute.CustomAttributeHandle} in {_customAttribute.Module}";
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory) => null;
    }
}
