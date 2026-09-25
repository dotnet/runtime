// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a compiled method body whose dependencies can vary by generic instantiation.
    /// </summary>
    public interface IMethodBodyNode : IMethodNode, IPCodeSymbolNode
    {
        /// <summary>
        /// Specializes this canonical body's runtime-determined dependencies for <paramref name="concreteMethod"/>
        /// and streams the resulting static dependencies to <paramref name="sink"/>.
        /// </summary>
        void AddRuntimeDeterminedStaticDependencies(DependencySink<NodeFactory> sink, NodeFactory factory, MethodDesc concreteMethod);

        /// <summary>
        /// Specializes this canonical body's runtime-determined conditional dependencies for <paramref name="concreteMethod"/>
        /// and streams them to <paramref name="sink"/>, preserving their conditions.
        /// </summary>
        void AddRuntimeDeterminedConditionalDependencies(DependencySink<NodeFactory> sink, NodeFactory factory, MethodDesc concreteMethod);
    }
}
