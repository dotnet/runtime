// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Internal.Text;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a method on a generic type (or a generic method) that doesn't
    /// have code emitted in the executable because it's physically backed by a canonical
    /// method body. The purpose of this node is to track the dependencies of the generic
    /// method body, as if it was generated. The node acts as a symbol for the canonical
    /// method for convenience.
    /// </summary>
    public abstract class ShadowMethodNode : DependencyNodeCore<NodeFactory>, IMethodNode, ISymbolNodeWithLinkage
    {
        /// <summary>
        /// Gets the canonical method body that defines the dependencies of this node.
        /// </summary>
        public IMethodBodyNode CanonicalMethodNode { get; }

        /// <summary>
        /// Gets the generic method represented by this node.
        /// </summary>
        public MethodDesc Method { get; }

        // Implementation of ISymbolNode that makes this node act as a symbol for the canonical body
        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            CanonicalMethodNode.AppendMangledName(nameMangler, sb);
        }
        public int Offset => CanonicalMethodNode.Offset;
        public bool RepresentsIndirectionCell => CanonicalMethodNode.RepresentsIndirectionCell;

        public override bool StaticDependenciesAreComputed
            => CanonicalMethodNode.StaticDependenciesAreComputed;

        public ShadowMethodNode(MethodDesc method, IMethodBodyNode canonicalMethod)
        {
            Debug.Assert(!method.IsRuntimeDeterminedExactMethod);
            Debug.Assert(canonicalMethod.Method == method.GetCanonMethodTarget(CanonicalFormKind.Specific));
            Debug.Assert(canonicalMethod.Method.IsSharedByGenericInstantiations);
            Method = method;
            CanonicalMethodNode = canonicalMethod;
        }

        public ISymbolNode NodeForLinkage(NodeFactory factory)
        {
            return CanonicalMethodNode;
        }

        public override void AddStaticDependencies(DependencySink<NodeFactory> sink, NodeFactory factory)
        {
            // Make sure the canonical body gets generated
            sink.Add(new DependencyListEntry(CanonicalMethodNode, "Canonical body"));

            CanonicalMethodNode.AddRuntimeDeterminedStaticDependencies(sink, factory, Method);
        }

        public sealed override void AddConditionalDependencies(DependencySink<NodeFactory> sink, NodeFactory factory)
        {
            CanonicalMethodNode.AddRuntimeDeterminedConditionalDependencies(sink, factory, Method);
        }


        protected override string GetName(NodeFactory factory) => $"{Method} backed by {CanonicalMethodNode.GetMangledName(factory.NameMangler)}";

        public sealed override bool HasConditionalStaticDependencies => CanonicalMethodNode.HasConditionalStaticDependencies;
        public sealed override bool HasDynamicDependencies => false;
        public sealed override bool InterestingForDynamicDependencyAnalysis => false;

        public sealed override void SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, DependencySink<NodeFactory> sink, NodeFactory factory) { }

        int ISortableNode.ClassCode => ClassCode;

        protected abstract int ClassCode { get; }

        int ISortableNode.CompareToImpl(ISortableNode other, CompilerComparer comparer) => CompareToImpl(other, comparer);

        protected abstract int CompareToImpl(ISortableNode other, CompilerComparer comparer);
    }
}
