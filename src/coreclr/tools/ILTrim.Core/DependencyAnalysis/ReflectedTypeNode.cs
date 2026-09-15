// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem.Ecma;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a type that is visible through reflection.
    /// </summary>
    public sealed class ReflectedTypeNode : DependencyNodeCore<NodeFactory>
    {
        private readonly EcmaType _type;

        public ReflectedTypeNode(EcmaType type)
        {
            _type = type;
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            yield return new(
                factory.TypeDefinition(_type.Module, _type.Handle),
                "Reflected type definition");

            if (!_type.IsValueType && LayoutTypeNode.IsLayoutType(_type))
            {
                yield return new(
                    factory.LayoutType(_type),
                    "Instance fields of a reflected type with sequential or explicit layout");
            }
        }

        protected override string GetName(NodeFactory factory)
        {
            return $"{_type} reflected";
        }

        public override bool HasConditionalStaticDependencies => false;
        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory) => null;
    }
}
