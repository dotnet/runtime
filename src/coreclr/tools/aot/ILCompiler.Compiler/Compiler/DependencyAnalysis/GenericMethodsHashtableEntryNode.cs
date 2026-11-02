// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    public class GenericMethodsHashtableEntryNode : DependencyNodeCore<NodeFactory>
    {
        private readonly MethodDesc _method;

        public GenericMethodsHashtableEntryNode(MethodDesc method)
        {
            _method = method;
        }

        public MethodDesc Method => _method;

        public override void AddStaticDependencies(DependencySink sink, NodeFactory factory)
        {
            GenericMethodsHashtableNode.GetGenericMethodsHashtableDependenciesForMethod(sink, factory, _method);
        }
        protected override string GetName(NodeFactory factory)
        {
            return "Generic methods hashtable entry: " + _method.ToString();
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override void AddConditionalDependencies(DependencySink sink, NodeFactory factory) { }
        public override void SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, DependencySink sink, NodeFactory factory) { }
    }
}
