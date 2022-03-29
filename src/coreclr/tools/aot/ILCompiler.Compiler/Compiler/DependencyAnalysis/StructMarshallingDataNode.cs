// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem;
using Internal.TypeSystem.Interop;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents an entry in the <see cref="StructMarshallingStubMapNode"/> table.
    /// </summary>
    public class StructMarshallingDataNode : DependencyNodeCore<NodeFactory>
    {
        private readonly DefType _type;

        public DefType Type => _type;

        public StructMarshallingDataNode(DefType type)
        {
            _type = type;
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            InteropStateManager stateManager = ((CompilerGeneratedInteropStubManager)factory.InteropStubManager)._interopStateManager;

            yield return new DependencyListEntry(factory.NecessaryTypeSymbol(_type), "Struct Marshalling Stub");

            // Not all StructMarshalingDataNodes require marshalling - some are only present because we want to
            // generate field offset information for Marshal.OffsetOf.
            if (MarshalHelpers.IsStructMarshallingRequired(_type))
            {
                yield return new DependencyListEntry(factory.MethodEntrypoint(stateManager.GetStructMarshallingManagedToNativeThunk(_type)), "Struct Marshalling stub");
                yield return new DependencyListEntry(factory.MethodEntrypoint(stateManager.GetStructMarshallingNativeToManagedThunk(_type)), "Struct Marshalling stub");
                yield return new DependencyListEntry(factory.MethodEntrypoint(stateManager.GetStructMarshallingCleanupThunk(_type)), "Struct Marshalling stub");
            }
        }

        protected override string GetName(NodeFactory context)
        {
            return $"Struct marshaling data for {_type}";
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => null;
    }
}
