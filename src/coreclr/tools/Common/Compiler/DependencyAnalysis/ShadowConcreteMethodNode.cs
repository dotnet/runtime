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
    /// Represents a concrete method on a generic type (or a generic method) that doesn't
    /// have code emitted in the executable because it's physically backed by a canonical
    /// method body. The purpose of this node is to track the dependencies of the concrete
    /// method body, as if it was generated. The node acts as a symbol for the canonical
    /// method for convenience.
    /// </summary>
    public class ShadowConcreteMethodNode : DependencyNodeCore<NodeFactory>, IMethodNode, ISymbolNodeWithLinkage
    {
        /// <summary>
        /// Gets the canonical method body that defines the dependencies of this node.
        /// </summary>
        public IMethodNode CanonicalMethodNode { get; }

        /// <summary>
        /// Gets the concrete method represented by this node.
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

        public ShadowConcreteMethodNode(MethodDesc method, IMethodNode canonicalMethod)
        {
            Debug.Assert(!method.IsSharedByGenericInstantiations);
            Debug.Assert(!method.IsRuntimeDeterminedExactMethod);
            Debug.Assert(canonicalMethod.Method.IsSharedByGenericInstantiations);
            Debug.Assert(canonicalMethod.Method == method.GetCanonMethodTarget(CanonicalFormKind.Specific));
            Method = method;
            CanonicalMethodNode = canonicalMethod;
        }

        public ISymbolNode NodeForLinkage(NodeFactory factory)
        {
            return CanonicalMethodNode;
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            DependencyList dependencies = new DependencyList();

            // Make sure the canonical body gets generated
            dependencies.Add(new DependencyListEntry(CanonicalMethodNode, "Canonical body"));

            // Instantiate the runtime determined dependencies of the canonical method body
            // with the concrete instantiation of the method to get concrete dependencies.
            Instantiation typeInst = Method.OwningType.Instantiation;
            Instantiation methodInst = Method.Instantiation;
            IEnumerable<DependencyListEntry> staticDependencies = CanonicalMethodNode.GetStaticDependencies(factory);

            if (staticDependencies != null)
            {
                foreach (DependencyListEntry canonDep in staticDependencies)
                {
                    var runtimeDep = canonDep.Node as INodeWithRuntimeDeterminedDependencies;
                    if (runtimeDep != null)
                    {
                        dependencies.AddRange(runtimeDep.InstantiateDependencies(factory, typeInst, methodInst));
                    }
                }
            }

            return dependencies;
        }

        protected override string GetName(NodeFactory factory) => $"{Method} backed by {CanonicalMethodNode.GetMangledName(factory.NameMangler)}";

        public sealed override bool HasConditionalStaticDependencies => false;
        public sealed override bool HasDynamicDependencies => false;
        public sealed override bool InterestingForDynamicDependencyAnalysis => false;

        public sealed override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory) => null;
        public sealed override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory) => null;

        int ISortableNode.ClassCode => -1440570971;

        int ISortableNode.CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            var compare = comparer.Compare(Method, ((ShadowConcreteMethodNode)other).Method);
            if (compare != 0)
                return compare;

            return comparer.Compare(CanonicalMethodNode, ((ShadowConcreteMethodNode)other).CanonicalMethodNode);
        }
    }
}
