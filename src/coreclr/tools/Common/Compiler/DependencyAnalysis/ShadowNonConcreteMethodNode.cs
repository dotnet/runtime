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
    /// Represents a non-concrete method (not fully instantiated) for the purpose of
    /// tracking dependencies.
    /// </summary>
    public class ShadowNonConcreteMethodNode : ShadowMethodNode, IMethodNode, ISymbolNodeWithLinkage
    {
        public ShadowNonConcreteMethodNode(MethodDesc method, IMethodNode canonicalMethod)
            : base(method, canonicalMethod)
        {
            Debug.Assert(method.IsSharedByGenericInstantiations);
        }

        protected override int ClassCode => 2120942405;

        protected override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            var compare = comparer.Compare(Method, ((ShadowNonConcreteMethodNode)other).Method);
            if (compare != 0)
                return compare;

            return comparer.Compare(CanonicalMethodNode, ((ShadowNonConcreteMethodNode)other).CanonicalMethodNode);
        }
    }
}
