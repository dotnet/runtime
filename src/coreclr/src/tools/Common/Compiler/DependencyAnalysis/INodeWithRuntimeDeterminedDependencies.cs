// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    using DependencyListEntry = DependencyAnalysisFramework.DependencyNodeCore<NodeFactory>.DependencyListEntry;

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
