// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a parameter that has metadata generated in the current compilation.
    /// </summary>
    /// <remarks>
    /// Only expected to be used during ILScanning when scanning for reflection.
    /// </remarks>
    internal sealed class MethodParameterMetadataNode : DependencyNodeCore<NodeFactory>
    {
        private readonly ReflectableParameter _parameter;

        public MethodParameterMetadataNode(ReflectableParameter parameter)
        {
            _parameter = parameter;
        }

        public ReflectableParameter Parameter => _parameter;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory) => null;

        protected override string GetName(NodeFactory factory)
        {
            return $"Reflectable parameter {_parameter.ParameterHandle} in {_parameter.Module}";
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory) => null;
    }
}
