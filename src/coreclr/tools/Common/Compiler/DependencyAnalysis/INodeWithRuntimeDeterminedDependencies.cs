// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.TypeSystem;
using DependencyListEntry = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.DependencyListEntry;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a node whose dependencies are runtime determined (they depend on the generic context)
    /// and which provides means to compute concrete dependencies when given the generic context.
    /// </summary>
    public interface INodeWithRuntimeDeterminedDependencies
    {
        /// <summary>
        /// Instantiates runtime determined dependencies of this node using the supplied generic context.
        /// </summary>
        IEnumerable<DependencyListEntry> InstantiateDependencies(NodeFactory factory, Instantiation typeInstantiation, Instantiation methodInstantiation);
    }
}
