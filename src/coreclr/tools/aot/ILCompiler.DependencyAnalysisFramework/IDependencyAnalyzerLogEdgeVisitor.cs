// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ILCompiler.DependencyAnalysisFramework
{
    public interface IDependencyAnalyzerLogEdgeVisitor<DependencyContextType>
    {
        void VisitEdge(DependencyNodeCore<DependencyContextType> nodeDepender, DependencyNodeCore<DependencyContextType> nodeDependedOn, string reason);
        void VisitEdge(string root, DependencyNodeCore<DependencyContextType> dependedOn);
        void VisitEdge(DependencyNodeCore<DependencyContextType> nodeDepender, DependencyNodeCore<DependencyContextType> nodeDependerOther, DependencyNodeCore<DependencyContextType> nodeDependedOn, string reason);
    }
}
