// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;
using System.Collections.Concurrent;
using System.Threading;

namespace DependencyLogViewer
{
    public class Node
    {
        public Node(int index, string name)
        {
            Index = index;
            Name = name;
        }

        public override string ToString()
        {
            return "Index:" + Index.ToString() + " Name:" + Name;
        }

        public readonly int Index;
        public readonly string Name;
        public readonly List<KeyValuePair<Node, string>> Dependencies = new List<KeyValuePair<Node, string>>();
        public readonly List<KeyValuePair<Node, string>> Dependents = new List<KeyValuePair<Node, string>>();
    }

    public class Graph
    {
        public override string ToString()
        {
            return "PID:" + PID.ToString() + " Id:" + ID.ToString() + " Name: " + Name;
        }

        public int NextConditionalNodeIndex = Int32.MaxValue;

        public int PID;
        public int ID;
        public string Name;
        public Dictionary<int, Node> Nodes = new Dictionary<int, Node>();
    }

    class GraphCollection
    {
        public static readonly GraphCollection Singleton = new GraphCollection();
        public static DependencyGraphs DependencyGraphsUI;

        public List<Graph> Graphs = new List<Graph>();

        public void AddGraph(int pid, int id, string name)
        {
            Graph g = new Graph();
            g.ID = id;
            g.PID = pid;
            g.Name = name;
            Graphs.Add(g);

            DependencyGraphsUI.ForceRefresh();
        }

        public Graph GetGraph(int pid, int id)
        {
            foreach (Graph g in Graphs)
            {
                if ((g.PID == pid) && (g.ID == id))
                    return g;
            }

            return null;
        }

        public void AddNodeToGraph(int pid, int id, int index, string name)
        {
            Graph g = GetGraph(pid, id);
            if (g == null)
                return;

            Node n = new Node(index, name);
            g.Nodes.Add(index, n);
        }

        public void AddEdgeToGraph(int pid, int id, int edge1, int edge2, string reason)
        {
            Graph g = GetGraph(pid, id);
            if (g == null)
                return;

            Node dependent = g.Nodes[edge1];
            Node dependee = g.Nodes[edge2];

            dependent.Dependencies.Add(new KeyValuePair<Node, string>(dependee, reason));
            dependee.Dependents.Add(new KeyValuePair<Node, string>(dependent, reason));
        }

        public void AddConditionalEdgeToGraph(int pid, int id, int reason1, int reason2, int target, string reason)
        {
            Graph g = GetGraph(pid, id);
            if (g == null)
                return;

            Node reason1Node = g.Nodes[reason1];
            Node reason2Node = g.Nodes[reason2];
            Node dependee = g.Nodes[target];

            int conditionalNodeIndex = g.NextConditionalNodeIndex--;
            Node conditionalNode = new Node(conditionalNodeIndex, String.Format("Conditional({0} - {1})", reason1Node.ToString(), reason2Node.ToString()));
            g.Nodes.Add(conditionalNodeIndex, conditionalNode);

            conditionalNode.Dependencies.Add(new KeyValuePair<Node, string>(dependee, reason));
            dependee.Dependents.Add(new KeyValuePair<Node, string>(conditionalNode, reason));

            reason1Node.Dependencies.Add(new KeyValuePair<Node, string>(conditionalNode, "Reason1Conditional - " + reason));
            conditionalNode.Dependents.Add(new KeyValuePair<Node, string>(reason1Node, "Reason1Conditional - " + reason));

            reason2Node.Dependencies.Add(new KeyValuePair<Node, string>(conditionalNode, "Reason2Conditional - " + reason));
            conditionalNode.Dependents.Add(new KeyValuePair<Node, string>(reason2Node, "Reason2Conditional - " + reason));
        }
    }

    enum GraphEventType
    {
        NewGraph,
        NewNode,
        NewEdge,
        NewConditionalEdge,
    }

    struct GraphEvent
    {
        public int Pid;
        public int Id;
        public GraphEventType EventType;
        public int Num1;
        public int Num2;
        public int Num3;
        public string Str;
    }

    class GraphProcessing
    {
        public static GraphProcessing Singleton;

        ConcurrentQueue<GraphEvent> events = new ConcurrentQueue<GraphEvent>();
        TraceEventSession session;
        volatile bool stopped;

        public GraphProcessing()
        {
            var sessionName = "GraphETWEventProcessingSession";

            session = new TraceEventSession(sessionName);

            Thread t = new Thread(EventProcessingThread);
            t.Start();

            Thread t2 = new Thread(ETWImportingThread);
            t2.Start();
        }

        private void EventProcessingThread()
        {
            GraphCollection collection = GraphCollection.Singleton;

            while (!stopped)
            {
                Thread.Sleep(1);

                lock (collection)
                {
                    GraphEvent eventRead;
                    while (events.TryDequeue(out eventRead))
                    {
                        try
                        {
                            switch (eventRead.EventType)
                            {
                                case GraphEventType.NewEdge:
                                    collection.AddEdgeToGraph(eventRead.Pid, eventRead.Id, eventRead.Num1, eventRead.Num2, eventRead.Str);
                                    break;
                                case GraphEventType.NewNode:
                                    collection.AddNodeToGraph(eventRead.Pid, eventRead.Id, eventRead.Num1, eventRead.Str);
                                    break;
                                case GraphEventType.NewGraph:
                                    collection.AddGraph(eventRead.Pid, eventRead.Id, eventRead.Str);
                                    break;
                                case GraphEventType.NewConditionalEdge:
                                    collection.AddConditionalEdgeToGraph(eventRead.Pid, eventRead.Id, eventRead.Num1, eventRead.Num2, eventRead.Num3, eventRead.Str);
                                    break;
                            }
                        }
                        catch
                        {
                            // Ignore bad input
                        }
                    }
                }
            }
        }

        private void ETWImportingThread()
        {
            
            using (session)
            {
                session.BufferSizeMB = 1024;
                session.Source.Dynamic.AddCallbackForProviderEvent("Microsoft-ILCompiler-DependencyGraph", "Graph", delegate (TraceEvent data)
                {
                    GraphEvent ge = new GraphEvent();
                    ge.EventType = GraphEventType.NewGraph;
                    ge.Pid = data.ProcessID;
                    ge.Id = (int)data.PayloadValue(0);
                    ge.Str = (string)data.PayloadValue(1);
                    events.Enqueue(ge);
                });
                session.Source.Dynamic.AddCallbackForProviderEvent("Microsoft-ILCompiler-DependencyGraph", "Node", delegate (TraceEvent data)
                {
                    GraphEvent ge = new GraphEvent();
                    ge.EventType = GraphEventType.NewNode;
                    ge.Pid = data.ProcessID;
                    ge.Id = (int)data.PayloadValue(0);
                    ge.Num1 = (int)data.PayloadValue(1);
                    ge.Str = (string)data.PayloadValue(2);
                    events.Enqueue(ge);
                });
                session.Source.Dynamic.AddCallbackForProviderEvent("Microsoft-ILCompiler-DependencyGraph", "Edge", delegate (TraceEvent data)
                {
                    GraphEvent ge = new GraphEvent();
                    ge.EventType = GraphEventType.NewEdge;
                    ge.Pid = data.ProcessID;
                    ge.Id = (int)data.PayloadValue(0);
                    ge.Num1 = (int)data.PayloadValue(1);
                    ge.Num2 = (int)data.PayloadValue(2);
                    ge.Str = (string)data.PayloadValue(3);
                    events.Enqueue(ge);
                });
                session.Source.Dynamic.AddCallbackForProviderEvent("Microsoft-ILCompiler-DependencyGraph", "ConditionalEdge", delegate (TraceEvent data)
                {
                    GraphEvent ge = new GraphEvent();
                    ge.EventType = GraphEventType.NewConditionalEdge;
                    ge.Pid = data.ProcessID;
                    ge.Id = (int)data.PayloadValue(0);
                    ge.Num1 = (int)data.PayloadValue(1);
                    ge.Num2 = (int)data.PayloadValue(2);
                    ge.Num3 = (int)data.PayloadValue(3);
                    ge.Str = (string)data.PayloadValue(4);
                    events.Enqueue(ge);
                });

                var restarted = session.EnableProvider("Microsoft-ILCompiler-DependencyGraph");
                session.Source.Process();
            }
        }

        public void Stop()
        {
            session.Source.Dispose();
            stopped = true;
        }
    }

    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main()
        {
            // Today you have to be Admin to turn on ETW events (anyone can write ETW events).   
            if (!(TraceEventSession.IsElevated() ?? false))
            {
                MessageBox.Show("To turn on ETW events you need to be Administrator, please run from an Admin process.");
                return -1;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            GraphCollection.DependencyGraphsUI = new DependencyGraphs();
            GraphProcessing.Singleton = new GraphProcessing();

            Application.Run(GraphCollection.DependencyGraphsUI);

            return 0;
        }
    }
}
