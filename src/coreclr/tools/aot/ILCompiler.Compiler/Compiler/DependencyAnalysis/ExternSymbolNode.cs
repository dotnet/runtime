// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Internal.Text;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a symbol that is defined externally and statically linked to the output obj file.
    /// </summary>
    public class ExternSymbolNode : SortableDependencyNode, ISortableSymbolNode
    {
        private readonly Utf8String _name;
        private readonly bool _isIndirection;

        public ExternSymbolNode(Utf8String name, bool isIndirection = false)
        {
            _name = name;
            _isIndirection = isIndirection;
        }

        protected override string GetName(NodeFactory factory) => $"ExternSymbol {_name}{(_isIndirection ? " (indirected)" : "")}";

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(_name);
        }
        public int Offset => 0;
        public virtual bool RepresentsIndirectionCell => _isIndirection;

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory) => null;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory) => null;

#if !SUPPORT_JIT
        public override int ClassCode => 1092559304;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return _name.CompareTo(((ExternSymbolNode)other)._name);
        }
#endif

        public override string ToString()
        {
            return _name.ToString();
        }
    }
}
