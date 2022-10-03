// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;
using System.Diagnostics;

namespace ILCompiler.DependencyAnalysisFramework
{
    public struct FirstMarkLogStrategy<DependencyContextType> : IDependencyAnalysisMarkStrategy<DependencyContextType>
    {
        private class MarkData
        {
            public MarkData(string reason, DependencyNodeCore<DependencyContextType> reason1, DependencyNodeCore<DependencyContextType> reason2)
            {
                Reason = reason;
                Reason1 = reason1;
                Reason2 = reason2;
            }

            public string Reason
            {
                get;
            }

            public DependencyNodeCore<DependencyContextType> Reason1
            {
                get;
            }

            public DependencyNodeCore<DependencyContextType> Reason2
            {
                get;
            }
        }

        private HashSet<string> _reasonStringOnlyNodes;

        bool IDependencyAnalysisMarkStrategy<DependencyContextType>.MarkNode(
            DependencyNodeCore<DependencyContextType> node,
            DependencyNodeCore<DependencyContextType> reasonNode,
            DependencyNodeCore<DependencyContextType> reasonNode2,
            string reason)
        {
            if (node.Marked)
                return false;

            if ((reasonNode == null) && (reasonNode2 == null))
            {
                Debug.Assert(reason != null);
                if (_reasonStringOnlyNodes == null)
                    _reasonStringOnlyNodes = new HashSet<string>();

                _reasonStringOnlyNodes.Add(reason);
            }

            node.SetMark(new MarkData(reason, reasonNode, reasonNode2));
            return true;
        }

        void IDependencyAnalysisMarkStrategy<DependencyContextType>.VisitLogNodes(IEnumerable<DependencyNodeCore<DependencyContextType>> nodeList, IDependencyAnalyzerLogNodeVisitor<DependencyContextType> logNodeVisitor)
        {
            var combinedNodesReported = new HashSet<Tuple<DependencyNodeCore<DependencyContextType>, DependencyNodeCore<DependencyContextType>>>();

            if (_reasonStringOnlyNodes != null)
            {
                foreach (string reasonOnly in _reasonStringOnlyNodes)
                {
                    logNodeVisitor.VisitRootNode(reasonOnly);
                }
            }

            foreach (DependencyNodeCore<DependencyContextType> node in nodeList)
            {
                if (node.Marked)
                {
                    MarkData markData = (MarkData)node.GetMark();

                    if (markData.Reason2 != null)
                    {
                        var combinedNode = new Tuple<DependencyNodeCore<DependencyContextType>, DependencyNodeCore<DependencyContextType>>(markData.Reason1, markData.Reason2);

                        if (combinedNodesReported.Add(combinedNode))
                        {
                            logNodeVisitor.VisitCombinedNode(combinedNode);
                        }
                    }
                }
            }
        }

        void IDependencyAnalysisMarkStrategy<DependencyContextType>.VisitLogEdges(IEnumerable<DependencyNodeCore<DependencyContextType>> nodeList, IDependencyAnalyzerLogEdgeVisitor<DependencyContextType> logEdgeVisitor)
        {
            foreach (DependencyNodeCore<DependencyContextType> node in nodeList)
            {
                if (node.Marked)
                {
                    MarkData markData = (MarkData)node.GetMark();

                    if (markData.Reason2 != null)
                    {
                        Debug.Assert(markData.Reason1 != null);
                        logEdgeVisitor.VisitEdge(markData.Reason1, markData.Reason2, node, markData.Reason);
                    }
                    else if (markData.Reason1 != null)
                    {
                        logEdgeVisitor.VisitEdge(markData.Reason1, node, markData.Reason);
                    }
                    else
                    {
                        Debug.Assert(markData.Reason != null);
                        logEdgeVisitor.VisitEdge(markData.Reason, node);
                    }
                }
            }
        }

        void IDependencyAnalysisMarkStrategy<DependencyContextType>.AttachContext(DependencyContextType context)
        {
            // This logger does not need to use the context
        }
    }
}
