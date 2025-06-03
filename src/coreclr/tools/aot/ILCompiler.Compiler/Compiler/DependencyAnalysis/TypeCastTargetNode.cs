// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a type that is the target of a type cast operation.
    /// Types that are targets of type casts preserve entries in type maps.
    /// </summary>
    public class TypeCastTargetNode : DependencyNodeCore<NodeFactory>
    {
        private readonly TypeDesc _type;

        public TypeCastTargetNode(TypeDesc type)
        {
            Debug.Assert(!type.IsCanonicalSubtype(CanonicalFormKind.Any)
                || type.ConvertToCanonForm(CanonicalFormKind.Specific) == type);
            _type = type;
        }

        public TypeDesc Type => _type;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            var result = new DependencyList
            {
                new DependencyListEntry(factory.ConstructedTypeSymbol(_type), "Type map cast target"),
            };

            return result;
        }
        protected override string GetName(NodeFactory factory)
        {
            return "Cast target type: " + _type.ToString();
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory) => null;
    }
}
