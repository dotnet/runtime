// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ILCompiler.DependencyAnalysisFramework
{
    public struct FullGraphLogStrategy<DependencyContextType> : IDependencyAnalysisMarkStrategy<DependencyContextType>
    {
        private sealed class MarkData : IEquatable<MarkData>
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

            private static int CombineHashCodes(int h1, int h2)
            {
                return (((h1 << 5) + h1) ^ h2);
            }

            private static int CombineHashCodes(int h1, int h2, int h3)
            {
                return CombineHashCodes(CombineHashCodes(h1, h2), h3);
            }

            public override int GetHashCode()
            {
                int reasonHashCode = Reason != null ? Reason.GetHashCode() : 0;
                int reason1HashCode = Reason1 != null ? Reason1.GetHashCode() : 0;
                int reason2HashCode = Reason2 != null ? Reason2.GetHashCode() : 0;

                return CombineHashCodes(reasonHashCode, reason1HashCode, reason2HashCode);
            }

            public override bool Equals(object obj)
            {
                MarkData other = obj as MarkData;
                if (other == null)
                    return false;

                return Equals(other);
            }

            public bool Equals(MarkData other)
            {
                if (Reason1 != other.Reason1)
                    return false;

                if (Reason2 != other.Reason2)
                    return false;

                if (Reason == other.Reason)
                    return true;

                if (Reason != null)
                {
                    return Reason.Equals(other.Reason);
                }
                return false;
            }
        }

        private sealed class MarkDataEqualityComparer : IEqualityComparer<MarkData>
        {
            public bool Equals(MarkData x, MarkData y)
            {
                return x.Equals(y);
            }

            public int GetHashCode(MarkData obj)
            {
                return obj.GetHashCode();
            }

            public static IEqualityComparer<MarkData> Default = new MarkDataEqualityComparer();
        }

        private HashSet<string> _reasonStringOnlyNodes;

        bool IDependencyAnalysisMarkStrategy<DependencyContextType>.MarkNode(
            DependencyNodeCore<DependencyContextType> node,
            DependencyNodeCore<DependencyContextType> reasonNode,
            DependencyNodeCore<DependencyContextType> reasonNode2,
            string reason)
        {
            bool newlyMarked = !node.Marked;

            HashSet<MarkData> associatedNodes;
            if (newlyMarked)
            {
                associatedNodes = new HashSet<MarkData>(MarkDataEqualityComparer.Default);
                node.SetMark(associatedNodes);
            }
            else
            {
                associatedNodes = (HashSet<MarkData>)node.GetMark();
            }

            if ((reasonNode == null) && (reasonNode2 == null))
            {
                Debug.Assert(reason != null);
                _reasonStringOnlyNodes ??= new HashSet<string>();

                _reasonStringOnlyNodes.Add(reason);
            }

            associatedNodes.Add(new MarkData(reason, reasonNode, reasonNode2));
            return newlyMarked;
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
                    HashSet<MarkData> nodeReasons = (HashSet<MarkData>)node.GetMark();
                    foreach (MarkData markData in nodeReasons)
                    {
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
        }

        void IDependencyAnalysisMarkStrategy<DependencyContextType>.VisitLogEdges(IEnumerable<DependencyNodeCore<DependencyContextType>> nodeList, IDependencyAnalyzerLogEdgeVisitor<DependencyContextType> logEdgeVisitor)
        {
            foreach (DependencyNodeCore<DependencyContextType> node in nodeList)
            {
                if (node.Marked)
                {
                    HashSet<MarkData> nodeReasons = (HashSet<MarkData>)node.GetMark();
                    foreach (MarkData markData in nodeReasons)
                    {
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
        }
        void IDependencyAnalysisMarkStrategy<DependencyContextType>.AttachContext(DependencyContextType context)
        {
            // This logger does not need to use the context
        }
    }
}
