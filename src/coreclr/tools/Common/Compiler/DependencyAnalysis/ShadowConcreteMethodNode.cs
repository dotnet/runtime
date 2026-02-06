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
    /// Represents a concrete method (fully instantiated) for the purpose of
    /// tracking dependencies of inlinees.
    /// </summary>
    public class ShadowConcreteMethodNode : ShadowMethodNode, IMethodNode, ISymbolNodeWithLinkage
    {
        public ShadowConcreteMethodNode(MethodDesc method, IMethodNode canonicalMethod)
            : base(method, canonicalMethod)
        {
            Debug.Assert(!method.IsSharedByGenericInstantiations);
        }

        protected override int ClassCode => -1440570971;

        protected override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            var compare = comparer.Compare(Method, ((ShadowConcreteMethodNode)other).Method);
            if (compare != 0)
                return compare;

            return comparer.Compare(CanonicalMethodNode, ((ShadowConcreteMethodNode)other).CanonicalMethodNode);
        }
    }
}
