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
    /// Represents a concrete unboxing thunk on a generic type (or a generic method) that doesn't
    /// have code emitted in the executable because it's physically backed by a canonical
    /// method body. The purpose of this node is to track the dependencies of the concrete
    /// thunk, as if it was generated. The node acts as a symbol for the canonical thunk
    /// method for convenience.
    /// </summary>
    internal sealed class ShadowConcreteUnboxingThunkNode : DependencyNodeCore<NodeFactory>, IMethodNode
    {
        private IMethodNode _canonicalThunk;

        /// <summary>
        /// Gets the concrete method represented by this node.
        /// </summary>
        public MethodDesc Method { get; }

        // Implementation of ISymbolNode that makes this node act as a symbol for the canonical body
        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            _canonicalThunk.AppendMangledName(nameMangler, sb);
        }
        public int Offset => _canonicalThunk.Offset;
        public bool RepresentsIndirectionCell => _canonicalThunk.RepresentsIndirectionCell;

        public override bool StaticDependenciesAreComputed => true;
        public ShadowConcreteUnboxingThunkNode(MethodDesc method, IMethodNode canonicalMethod)
        {
            Debug.Assert(!method.IsSharedByGenericInstantiations);
            Debug.Assert(!method.IsRuntimeDeterminedExactMethod);
            Debug.Assert(canonicalMethod.Method.IsSharedByGenericInstantiations);
            Debug.Assert(canonicalMethod.Method == method.GetCanonMethodTarget(CanonicalFormKind.Specific));
            Method = method;
            _canonicalThunk = canonicalMethod;
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            DependencyList dependencies = new DependencyList();

            // Make sure the canonical body gets generated
            dependencies.Add(new DependencyListEntry(_canonicalThunk, "Canonical body"));

            // Make sure the target of the thunk gets modeled as a dependency
            dependencies.Add(new DependencyListEntry(factory.ShadowConcreteMethod(Method), "Unboxing thunk target"));

            return dependencies;
        }

        protected override string GetName(NodeFactory factory) => $"{Method} backed by {_canonicalThunk.GetMangledName(factory.NameMangler)}";

        public sealed override bool HasConditionalStaticDependencies => false;
        public sealed override bool HasDynamicDependencies => false;
        public sealed override bool InterestingForDynamicDependencyAnalysis => false;

        public sealed override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory) => null;
        public sealed override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory) => null;

        int ISortableNode.ClassCode => -501699818;

        int ISortableNode.CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            var compare = comparer.Compare(Method, ((ShadowConcreteUnboxingThunkNode)other).Method);
            if (compare != 0)
                return compare;

            return comparer.Compare(_canonicalThunk, ((ShadowConcreteUnboxingThunkNode)other)._canonicalThunk);
        }
    }
}
