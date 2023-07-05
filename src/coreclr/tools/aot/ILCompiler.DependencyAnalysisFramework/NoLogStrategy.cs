// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace ILCompiler.DependencyAnalysisFramework
{
    /// <summary>
    /// Very memory efficient, and potentially faster mark strategy that eschews keeping track of what caused what to exist
    /// </summary>
    /// <typeparam name="DependencyContextType"></typeparam>
    public struct NoLogStrategy<DependencyContextType> : IDependencyAnalysisMarkStrategy<DependencyContextType>
    {
        private static object s_singleton = new object();

        bool IDependencyAnalysisMarkStrategy<DependencyContextType>.MarkNode(
            DependencyNodeCore<DependencyContextType> node,
            DependencyNodeCore<DependencyContextType> reasonNode,
            DependencyNodeCore<DependencyContextType> reasonNode2,
            string reason)
        {
            if (node.Marked)
                return false;

            node.SetMark(s_singleton);
            return true;
        }

        void IDependencyAnalysisMarkStrategy<DependencyContextType>.VisitLogEdges(IEnumerable<DependencyNodeCore<DependencyContextType>> nodeList, IDependencyAnalyzerLogEdgeVisitor<DependencyContextType> logEdgeVisitor)
        {
            // This marker does not permit logging.
            return;
        }

        void IDependencyAnalysisMarkStrategy<DependencyContextType>.VisitLogNodes(IEnumerable<DependencyNodeCore<DependencyContextType>> nodeList, IDependencyAnalyzerLogNodeVisitor<DependencyContextType> logNodeVisitor)
        {
            // This marker does not permit logging.
            return;
        }

        void IDependencyAnalysisMarkStrategy<DependencyContextType>.AttachContext(DependencyContextType context)
        {
            // This logger does not need to use the context
        }
    }
}
