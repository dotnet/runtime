// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a type that has callable instance methods.
    /// This is similar to <see cref="ConstructedEETypeNode"/>, with the key difference
    /// being that abstract types that are not seen as derived by another non-abstract
    /// <see cref="ConstructedEETypeNode"/> will not be represented by this node.
    /// </summary>
    internal class TypeWithInstanceMethodsNode : DependencyNodeCore<NodeFactory>
    {
        private readonly TypeDesc _type;

        public TypeWithInstanceMethodsNode(TypeDesc type)
        {
            Debug.Assert(type is not MetadataType mdType || !mdType.IsAbstract || !mdType.IsSealed);
            _type = type;
        }

        protected override string GetName(NodeFactory factory) => $"TypeOnTheHeap {_type}";

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            TypeDesc baseType = _type.BaseType;
            if (baseType != null)
                return new[] { new DependencyListEntry(factory.TypeWithInstanceMethods(baseType), "Base of a type on heap") };

            return null;
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory) => null;
    }
}
