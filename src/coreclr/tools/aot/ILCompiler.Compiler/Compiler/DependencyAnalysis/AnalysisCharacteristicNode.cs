// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

namespace ILCompiler.DependencyAnalysis
{
    public class AnalysisCharacteristicNode : DependencyNodeCore<NodeFactory>
    {
        public AnalysisCharacteristicNode(string characteristic)
            => Characteristic = characteristic;

        public string Characteristic { get; }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override void AddConditionalDependencies(DependencySink<NodeFactory> sink, NodeFactory context) { }
        public override void AddStaticDependencies(DependencySink<NodeFactory> sink, NodeFactory context) { }
        public override void SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, DependencySink<NodeFactory> sink, NodeFactory context) { }
        protected override string GetName(NodeFactory context) => $"Analysis characteristic: {Characteristic}";
    }
}
