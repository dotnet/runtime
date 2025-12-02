// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Internal.Text;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a symbol that is defined externally and statically linked to the output obj file.
    /// When making a new node, do not derive from this class directly, derive from one of its subclasses
    /// (ExternFunctionSymbolNode / ExternDataSymbolNode) instead.
    /// </summary>
    public abstract class ExternSymbolNode : SortableDependencyNode, ISortableSymbolNode
    {
        private readonly Utf8String _name;
        private readonly bool _isIndirection;

        protected ExternSymbolNode(Utf8String name, bool isIndirection = false)
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

    /// <summary>
    /// Represents a function symbol that is defined externally and statically linked to the output obj file.
    /// </summary>
    public class ExternFunctionSymbolNode(Utf8String name, bool isIndirection = false) : ExternSymbolNode(name, isIndirection)
    {
        public override int ClassCode => 1452455506;
    }

    public class AddressTakenExternFunctionSymbolNode(Utf8String name) : ExternFunctionSymbolNode(name)
    {
        public override int ClassCode => -45645737;
    }

    /// <summary>
    /// Represents a data symbol that is defined externally and statically linked to the output obj file.
    /// </summary>
    public class ExternDataSymbolNode(Utf8String name) : ExternSymbolNode(name)
    {
        public override int ClassCode => 1428609964;

        protected override string GetName(NodeFactory factory) => $"ExternDataSymbolNode {ToString()}";
    }
}
