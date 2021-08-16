// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace ILCompiler.DependencyAnalysisFramework
{
    public interface IDependencyAnalyzerLogNodeVisitor<DependencyContextType>
    {
        void VisitCombinedNode(Tuple<DependencyNodeCore<DependencyContextType>, DependencyNodeCore<DependencyContextType>> node);
        void VisitNode(DependencyNodeCore<DependencyContextType> node);
        void VisitRootNode(string rootName);
    }
}
