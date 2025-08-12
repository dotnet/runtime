// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    internal sealed class GVMMetadataNode : SortableDependencyNode
    {
        public MethodDesc CallingMethod { get; }
        public MethodDesc ImplementationMethod { get; }
        public GVMMetadataNode(MethodDesc callingMethod, MethodDesc implementationMethod)
            => (CallingMethod, ImplementationMethod) = (callingMethod, implementationMethod);

        protected override string GetName(NodeFactory context) =>
            $"GVM method: {CallingMethod}: {ImplementationMethod}";

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            var list = new DependencyList();
            GenericVirtualMethodTableNode.GetGenericVirtualMethodImplementationDependencies(ref list, factory, CallingMethod, ImplementationMethod);
            return list;
        }

        public override int ClassCode => 0x2898423;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            var otherNode = (GVMMetadataNode)other;

            int result = comparer.Compare(CallingMethod, otherNode.CallingMethod);
            if (result != 0)
                return result;

            return comparer.Compare(ImplementationMethod, otherNode.ImplementationMethod);
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => null;
    }
}
