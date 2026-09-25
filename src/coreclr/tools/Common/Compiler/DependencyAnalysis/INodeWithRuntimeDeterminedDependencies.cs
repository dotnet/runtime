// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Internal.TypeSystem;
using ILCompiler.DependencyAnalysisFramework;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a node whose dependencies are runtime determined (they depend on the generic context)
    /// and which provides means to compute concrete dependencies when given the generic context.
    /// </summary>
    public interface INodeWithRuntimeDeterminedDependencies
    {
        /// <summary>
        /// Instantiates runtime determined dependencies of this node using the
        /// supplied generic context. If <paramref name="otherReasonNode"/> is
        /// not null, the dependencies are considered conditional.
        /// </summary>
        void AddDependencies(
            DependencySink<NodeFactory> sink,
            NodeFactory factory,
            Instantiation typeInstantiation,
            Instantiation methodInstantiation,
            bool isConcreteInstantiation,
            DependencyNodeCore<NodeFactory>? otherReasonNode);
    }
}
