// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    public class ExactMethodInstantiationsEntryNode : DependencyNodeCore<NodeFactory>
    {
        private readonly MethodDesc _method;

        public ExactMethodInstantiationsEntryNode(MethodDesc method)
        {
            Debug.Assert(!method.IsAbstract);
            Debug.Assert(method.HasInstantiation);
            Debug.Assert(!method.GetCanonMethodTarget(CanonicalFormKind.Specific).IsCanonicalMethod(CanonicalFormKind.Any));
            _method = method;
        }

        public MethodDesc Method => _method;

        public override void AddStaticDependencies(DependencySink<NodeFactory> sink, NodeFactory factory)
        {
            DependencySink<NodeFactory> dependencies = sink;
            ExactMethodInstantiationsNode.GetExactMethodInstantiationDependenciesForMethod(dependencies, factory, _method);
            Debug.Assert(dependencies != null);
        }
        protected override string GetName(NodeFactory factory)
        {
            return "Exact methods hashtable entry: " + _method.ToString();
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override void AddConditionalDependencies(DependencySink<NodeFactory> sink, NodeFactory factory) { }
        public override void SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, DependencySink<NodeFactory> sink, NodeFactory factory) { }
    }
}
