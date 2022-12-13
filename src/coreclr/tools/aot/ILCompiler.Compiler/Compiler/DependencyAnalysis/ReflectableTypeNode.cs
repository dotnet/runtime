// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a type that is visible from reflection.
    /// </summary>
    public class ReflectableTypeNode : DependencyNodeCore<NodeFactory>
    {
        private readonly TypeDesc _type;

        public ReflectableTypeNode(TypeDesc type)
        {
            Debug.Assert(!type.IsCanonicalSubtype(CanonicalFormKind.Any)
                || type.ConvertToCanonForm(CanonicalFormKind.Specific) == type);
            _type = type;
        }

        public TypeDesc Type => _type;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            return new DependencyListEntry[]
                {
                    new DependencyListEntry(factory.MaximallyConstructableType(_type), "Reflection target"),
                };
        }
        protected override string GetName(NodeFactory factory)
        {
            return "Reflectable type: " + _type.ToString();
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory) => null;
    }
}
