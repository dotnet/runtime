// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Internal.IL;
using Internal.Text;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a stand-in for a real method body that can turn into the real method
    /// body at object emission phase if the real method body was marked.
    /// It the real method body wasn't marked, this stub will tail-call into a throw helper.
    /// </summary>
    public partial class TentativeMethodNode : AssemblyStubNode, IMethodNode, ISymbolNodeWithLinkage
    {
        private readonly IMethodBodyNode _methodNode;

        public IMethodBodyNode RealBody => _methodNode;

        public TentativeMethodNode(IMethodBodyNode methodNode)
        {
            _methodNode = methodNode;
        }

        protected virtual ISymbolNode GetTarget(NodeFactory factory)
        {
            // If the class library doesn't provide this helper, the optimization is disabled.
            MethodDesc helper = factory.TypeSystemContext.GetOptionalHelperEntryPoint("ThrowHelpers", "ThrowBodyRemoved");
            return helper == null ? RealBody : factory.MethodEntrypoint(helper);
        }

        public MethodDesc Method => _methodNode.Method;

        protected override string GetName(NodeFactory factory)
        {
            return "Tentative method: " + _methodNode.GetMangledName(factory.NameMangler);
        }

        public override bool ShouldSkipEmittingObjectNode(NodeFactory factory)
        {
            // If the real body was marked, don't emit this assembly stub.
            return _methodNode.Marked;
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
            return _methodNode.CompareToImpl(((TentativeMethodNode)other)._methodNode, comparer);
        }

        public ISymbolNode NodeForLinkage(NodeFactory factory)
        {
            // If someone refers to this node but the real method was marked, emit relocs to this
            // as relocs to the real method.
            return _methodNode.Marked ? _methodNode : (ISymbolNode)this;
        }

        public override bool RepresentsIndirectionCell
        {
            get
            {
                Debug.Assert(!_methodNode.RepresentsIndirectionCell);
                return false;
            }
        }

        public override int ClassCode => 0x562912;

        public override bool IsShareable => ((ObjectNode)_methodNode).IsShareable;
    }
}
