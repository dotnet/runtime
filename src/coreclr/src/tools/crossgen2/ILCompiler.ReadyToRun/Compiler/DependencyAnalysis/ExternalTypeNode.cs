// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    class ExternalTypeNode : DependencyNodeCore<NodeFactory>, IEETypeNode
    {
        private readonly TypeDesc _type;

        public ExternalTypeNode(NodeFactory factory, TypeDesc type)
        {
            _type = type;
        }

        public TypeDesc Type => _type;

        public int Offset => 0;

        public bool RepresentsIndirectionCell => false;

        public int ClassCode => -1044459;

        public override bool InterestingForDynamicDependencyAnalysis => false;

        public override bool HasDynamicDependencies => false;

        public override bool HasConditionalStaticDependencies => false;

        public override bool StaticDependenciesAreComputed => true;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.GetMangledTypeName(_type));
        }

        public int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return comparer.Compare(Type, ((ExternalTypeNode)other).Type);
        }

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => null;
        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context) => null;

        protected override string GetName(NodeFactory factory) => $"Externally referenced type {Type.ToString()}";
    }
}
