// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.TypeSystem;

using ILCompiler.DependencyAnalysisFramework;

namespace ILCompiler.DependencyAnalysis
{
    internal sealed class TypeWithGenericVirtualMethodsNode : DependencyNodeCore<NodeFactory>
    {
        private readonly TypeDesc _type;

        public TypeWithGenericVirtualMethodsNode(TypeDesc type)
        {
            _type = type;
        }

        public TypeDesc Type => _type;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            TypeDesc canonType = _type.ConvertToCanonForm(CanonicalFormKind.Specific);
            if (_type != canonType)
                return [new DependencyListEntry(factory.TypeWithGenericVirtualMethods(canonType), "Canonical form")];
            return null;
        }

        public override bool HasConditionalStaticDependencies => false;
        public override bool HasDynamicDependencies => false;
        public override bool InterestingForDynamicDependencyAnalysis => true;
        public override bool StaticDependenciesAreComputed => true;
        protected override string GetName(NodeFactory factory) => $"Generic virtual method analysis for {_type}";
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => null;
    }
}
