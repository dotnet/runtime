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
using static System.Collections.Specialized.BitVector32;
using System.Xml;
using System.IO;

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
    class DGMLGraphProcessing
    {
        
        public static DGMLGraphProcessing Singleton;

        ConcurrentQueue<GraphEvent> events = new ConcurrentQueue<GraphEvent>();

        public DGMLGraphProcessing(string filePath)
        {
            Thread t = new Thread(FileReadingThread);
            t.Start(filePath);

        }
        public System.IO.Stream FindXML(string fp)
        {
            var fileContent = string.Empty;
            var filePath = string.Empty;
            var fileStream = System.IO.Stream.Null;

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = fp;
                openFileDialog.Filter = @"All Files|*.*|DGML(*.dgml)|*.dgml|XML(*.xml)|*xml";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (!filePath.EndsWith(".dgml") && !filePath.EndsWith(".xml"))
                {
                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        //Get the path of specified file
                        filePath = openFileDialog.FileName;
                    }
                }
                fileStream = openFileDialog.OpenFile();
            }
            return fileStream;
        }
        public void FileReadingThread(System.IO.Stream fileStream)
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreWhitespace = true;

            using (XmlReader reader = XmlReader.Create(fileStream, settings))
            {
                while (reader.Read())
                {
                    if (!(reader.Name.StartsWith("P")))
                    {
                        switch (reader.NodeType)
                        {
                            case XmlNodeType.Element:
                                if (reader.Name.EndsWith("s"))
                                {
                                    Console.WriteLine("Begin {0}", reader.Name);
                                }
                                else
                                {
                                    //String id = reader.GetAttribute("Id");
                                    //String src = reader.GetAttribute("Source");
                                    //Console.WriteLine("{0}: {1} {2}", reader.Name, id, src);

                                }
                                break;
                            case XmlNodeType.EndElement:
                                Console.WriteLine("End {0}", reader.Name);
                                break;
                            default:
                                break;
                        }
                    }

                }
            }
        }
    }
    class ETWGraphProcessing
    {
        public static ETWGraphProcessing Singleton;

        ConcurrentQueue<GraphEvent> events = new ConcurrentQueue<GraphEvent>();
        TraceEventSession session;
        volatile bool stopped;

        public ETWGraphProcessing()
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
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                string argPath = "c:\\Users";
            }
            else
            {
                string argPath = args[0];
            }
            
            string message = "Use ETW events?";
            string caption = "";
            MessageBoxButtons buttons = MessageBoxButtons.YesNo;
            DialogResult result;
            result = MessageBox.Show(message, caption, buttons);
            
            // Today you have to be Admin to turn on ETW events (anyone can write ETW events).
            
            if (!(TraceEventSession.IsElevated() ?? false) && result == System.Windows.Forms.DialogResult.Yes)
            {
                MessageBox.Show("To turn on ETW events you need to be Administrator, please run from an Admin process.");
                return -1;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            GraphCollection.DependencyGraphsUI = new DependencyGraphs();
            if (result == System.Windows.Forms.DialogResult.Yes)
            {
                ETWGraphProcessing.Singleton = new ETWGraphProcessing();
            }
            else
            {
                DGMLGraphProcessing.Singleton = new DGMLGraphProcessing(argPath);
            }
            

            Application.Run(GraphCollection.DependencyGraphsUI);

            return 0;
        }
    }
}
