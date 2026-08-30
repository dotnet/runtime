// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents an interface that is considered used at runtime (e.g. there's a cast to it
    /// or a virtual method on it is called).
    /// </summary>
    public class InterfaceUseNode : DependencyNodeCore<NodeFactory>
    {
        private readonly EcmaType _type;

        public InterfaceUseNode(EcmaType type)
        {
            Debug.Assert(type.IsInterface);
            _type = type;
        }

        protected override string GetName(NodeFactory factory)
        {
            return $"{_type} interface used";
        }

        public override void AddStaticDependencies(DependencySink<NodeFactory> sink, NodeFactory factory) { }
        public override void AddConditionalDependencies(DependencySink<NodeFactory> sink, NodeFactory factory) { }
        public override bool HasConditionalStaticDependencies => false;
        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override void SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, DependencySink<NodeFactory> sink, NodeFactory factory) { }
    }
}
