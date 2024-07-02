// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

using ILCompiler.DependencyAnalysisFramework;

using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a method with address taken. Under normal circumstances, this node is not emitted
    /// into the object file and instead references to it are replaced to refer to the underlying method body.
    /// This is achieved through <see cref="ShouldSkipEmittingObjectNode(NodeFactory)"/> and <see cref="NodeForLinkage(NodeFactory)"/>.
    /// However, if the underlying method body got folded together with another method due to identical method body folding
    /// optimization, this node is not skipped and instead emits a jump stub. The purpose of the jump stub is to provide a
    /// unique code address for the address taken method.
    /// </summary>
    internal sealed class AddressTakenMethodNode : JumpStubNode, IMethodNode, ISymbolNodeWithLinkage
    {
        private readonly IMethodNode _methodNode;

        public IMethodNode RealBody => _methodNode;

        public AddressTakenMethodNode(IMethodNode methodNode)
            : base(methodNode)
        {
            _methodNode = methodNode;
        }

        public MethodDesc Method => _methodNode.Method;

        protected override string GetName(NodeFactory factory)
        {
            return "Address taken method: " + _methodNode.GetMangledName(factory.NameMangler);
        }

        public override bool ShouldSkipEmittingObjectNode(NodeFactory factory)
        {
            return factory.ObjectInterner.GetDeduplicatedSymbol(factory, RealBody) == RealBody;
        }

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => null;
        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            // We use the same mangled name as the underlying real method body.
            // This is okay since this node will go out of the way if the real body is marked
            // and part of the graph.
            _methodNode.AppendMangledName(nameMangler, sb);
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return _methodNode.CompareToImpl(((AddressTakenMethodNode)other)._methodNode, comparer);
        }

        public ISymbolNode NodeForLinkage(NodeFactory factory)
        {
            // If someone refers to this node but the target method still has a unique body,
            // refer to the target method.
            return factory.ObjectInterner.GetDeduplicatedSymbol(factory, RealBody) == RealBody ? RealBody : this;
        }

        public override bool RepresentsIndirectionCell
        {
            get
            {
                Debug.Assert(!_methodNode.RepresentsIndirectionCell);
                return false;
            }
        }

        public override int ClassCode => 0xfab0355;

        public override bool IsShareable => ((ObjectNode)_methodNode).IsShareable;
    }
}
