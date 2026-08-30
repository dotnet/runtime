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

        void AddStaticDependencies(DependencySink<DependencyContextType> sink, DependencyContextType context);

        void AddConditionalDependencies(DependencySink<DependencyContextType> sink, DependencyContextType context);

        void SearchDynamicDependencies(List<DependencyNodeCore<DependencyContextType>> markedNodes, int firstNode, DependencySink<DependencyContextType> sink, DependencyContextType context);

        string GetName(DependencyContextType context);
    }
}
