// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    internal sealed class InterfaceGVMMetadataNode : SortableDependencyNode
    {
        public MethodDesc CallingMethod { get; }
        public MethodDesc ImplementationMethod { get; }
        public TypeDesc ImplementationType { get; }
        public DefaultInterfaceMethodResolution DefaultResolution { get; }

        public InterfaceGVMMetadataNode(MethodDesc callingMethod, MethodDesc implementationMethod,
                TypeDesc implementationType, DefaultInterfaceMethodResolution defaultResolution)
            => (CallingMethod, ImplementationMethod, ImplementationType, DefaultResolution)
            = (callingMethod, implementationMethod, implementationType, defaultResolution);

        protected override string GetName(NodeFactory context) =>
            $"GVM interface method: {CallingMethod} on {ImplementationType}: {ImplementationMethod}, {DefaultResolution}";

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            var list = new DependencyList();
            InterfaceGenericVirtualMethodTableNode.GetGenericVirtualMethodImplementationDependencies(ref list, factory, CallingMethod, ImplementationType, ImplementationMethod);
            return list;
        }

        public override int ClassCode => 0x48bcaa1;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            var otherNode = (InterfaceGVMMetadataNode)other;

            int result = comparer.Compare(ImplementationType, otherNode.ImplementationType);
            if (result != 0)
                return result;

            DefType[] interfaceList = ImplementationType.RuntimeInterfaces;
            int thisIndex = Array.IndexOf(interfaceList, CallingMethod.OwningType);
            int thatIndex = Array.IndexOf(interfaceList, otherNode.CallingMethod.OwningType);

            Debug.Assert(thisIndex >= 0 && thatIndex >= 0);

            result = Comparer<int>.Default.Compare(thisIndex, thatIndex);
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
