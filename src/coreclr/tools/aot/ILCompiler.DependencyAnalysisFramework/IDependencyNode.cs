// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace ILCompiler.DependencyAnalysisFramework
{
    public interface IDependencyNode
    {
        bool Marked
        {
            get;
        }
    }

    public interface IDependencyNode<DependencyContextType> : IDependencyNode
    {
        bool InterestingForDynamicDependencyAnalysis
        {
            get;
        }

        bool HasDynamicDependencies
        {
            get;
        }

        bool HasConditionalStaticDependencies
        {
            get;
        }

        bool StaticDependenciesAreComputed
        {
            get;
        }

        IEnumerable<DependencyNodeCore<DependencyContextType>.DependencyListEntry> GetStaticDependencies(DependencyContextType context);

        IEnumerable<DependencyNodeCore<DependencyContextType>.CombinedDependencyListEntry> GetConditionalStaticDependencies(DependencyContextType context);

        IEnumerable<DependencyNodeCore<DependencyContextType>.CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<DependencyContextType>> markedNodes, int firstNode, DependencyContextType context);
    }
}
