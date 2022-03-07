// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics.Tracing;
using System.Runtime.InteropServices;

namespace ILCompiler.DependencyAnalysisFramework
{
    [EventSource(Name = "Microsoft-ILCompiler-DependencyGraph")]
    class GraphEventSource : EventSource
    {
        public class Keywords
        {
            public const EventKeywords Graph = (EventKeywords)1;
        }

        // Notice that the bodies of the events follow a pattern:  WriteEvent(ID, <args>) where 
        //     ID is a unique ID starting at 1 and incrementing for each new event method. and
        //     <args> is every argument for the method.  
        // WriteEvent then takes care of all the details of actually writing out the values complete
        // with the name of the event (method name) as well as the names and types of all the parameters. 
        [Event(1, Keywords = Keywords.Graph, Level = EventLevel.Informational)]
        public void Graph(int id, string name) { WriteEvent(1, id, name); }
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
           Justification = "Parameters to this method are primitive and are trimmer safe")]
        [Event(2, Keywords = Keywords.Graph, Level = EventLevel.Informational)]
        public void Node(int id, int index, string name) { WriteEvent(2, id, index, name); }
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
           Justification = "Parameters to this method are primitive and are trimmer safe")]
        [Event(3, Keywords = Keywords.Graph, Level = EventLevel.Informational)]
        public void Edge(int id, int dependentIndex, int dependencyIndex, string reason) { WriteEvent(3, id, dependentIndex, dependencyIndex, reason); }
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
           Justification = "Parameters to this method are primitive and are trimmer safe")]
        [Event(4, Keywords = Keywords.Graph, Level = EventLevel.Informational)]
        public void ConditionalEdge(int id, int dependentIndex1, int dependentIndex2, int dependencyIndex, string reason) { WriteEvent(4, id, dependentIndex1, dependentIndex2, dependencyIndex, reason); }

        // Typically you only create one EventSource and use it throughout your program.  Thus a static field makes sense.  
        public static GraphEventSource Log = new GraphEventSource();
    }

    public struct EventSourceLogStrategy<DependencyContextType> : IDependencyAnalysisMarkStrategy<DependencyContextType>
    {
        private static int s_GraphIds = 0;

        private int GraphId;
        private int RootIndex;
        private int ObjectIndex;
        private DependencyContextType _context;

        public static bool IsEventSourceEnabled
        {
            get
            {
                return 
#if !ALWAYS_SUPPORT_EVENTSOURCE_LOG
                       RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && // Processing these event source events is only implemented on Windows
#endif
                       GraphEventSource.Log.IsEnabled();
            }
        }

        bool IDependencyAnalysisMarkStrategy<DependencyContextType>.MarkNode(
            DependencyNodeCore<DependencyContextType> node,
            DependencyNodeCore<DependencyContextType> reasonNode,
            DependencyNodeCore<DependencyContextType> reasonNode2,
            string reason)
        {
            bool retVal = false;

            int nodeIndex;

            if (!node.Marked)
            {
                nodeIndex = Interlocked.Increment(ref ObjectIndex);
                node.SetMark(nodeIndex);

                if (GraphId == 0)
                {
                    lock (GraphEventSource.Log)
                    {
                        if (GraphId == 0)
                        {
                            GraphId = Interlocked.Increment(ref s_GraphIds);
                            GraphEventSource.Log.Graph(GraphId, "");
                            RootIndex = Interlocked.Increment(ref ObjectIndex);
                            GraphEventSource.Log.Node(GraphId, RootIndex, "roots");
                        }
                    }
                }

                retVal = true;

                GraphEventSource.Log.Node(GraphId, nodeIndex, node.GetNameInternal(_context));
            }
            else
            {
                nodeIndex = (int)node.GetMark();
            }

            if (reasonNode != null)
            {
                if (reasonNode2 != null)
                {
                    GraphEventSource.Log.ConditionalEdge(GraphId, (int)reasonNode.GetMark(), (int)reasonNode2.GetMark(), nodeIndex, reason);
                }
                else
                {
                    GraphEventSource.Log.Edge(GraphId, (int)reasonNode.GetMark(), nodeIndex, reason);
                }
            }
            else
            {
                GraphEventSource.Log.Edge(GraphId, RootIndex, nodeIndex, reason);
            }
            return retVal;
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
            _context = context;
        }
    }
}
