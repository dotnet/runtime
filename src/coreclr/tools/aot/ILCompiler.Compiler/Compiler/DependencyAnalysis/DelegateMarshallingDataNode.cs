// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents an entry in the <see cref="DelegateMarshallingStubMapNode"/> table.
    /// </summary>
    public class DelegateMarshallingDataNode : DependencyNodeCore<NodeFactory>
    {
        private readonly DefType _type;

        public DefType Type => _type;

        public DelegateMarshallingDataNode(DefType type)
        {
            Debug.Assert(type.IsDelegate);
            _type = type;
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            InteropStateManager stateManager = ((CompilerGeneratedInteropStubManager)factory.InteropStubManager)._interopStateManager;

            return new DependencyListEntry[]
            {
                new DependencyListEntry(factory.NecessaryTypeSymbol(_type), "Delegate Marshalling Stub"),
                new DependencyListEntry(factory.MethodEntrypoint(stateManager.GetOpenStaticDelegateMarshallingThunk(_type)), "Delegate Marshalling Stub"),
                new DependencyListEntry(factory.MethodEntrypoint(stateManager.GetClosedDelegateMarshallingThunk(_type)), "Delegate Marshalling Stub"),
                new DependencyListEntry(factory.MethodEntrypoint(stateManager.GetForwardDelegateCreationThunk(_type)), "Delegate Marshalling Stub"),
            };
        }

        protected override string GetName(NodeFactory context)
        {
            return $"Delegate marshaling data for {_type}";
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => null;
    }
}
