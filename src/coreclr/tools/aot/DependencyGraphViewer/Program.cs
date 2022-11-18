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
using System.Xml;
using System.IO;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Security.Cryptography;
using System.Xml.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DependecyGraphViewer.Tests")]

namespace DependencyLogViewer
{
    public class Node
    {
        public readonly int Index;
        public readonly string Name;
        public readonly Dictionary<Node, List<string>> Targets = new Dictionary<Node, List<string>>();
        public readonly Dictionary<Node, List<string>> Sources = new Dictionary<Node, List<string>>();

        public Node(int index, string name)
        {
            Index = index;
            Name = name;
        }

        public override string ToString()
        {
            return $"Index: {Index}, Name: {Name}";
        }
    }

    public class Graph
    {
        public int NextConditionalNodeIndex = Int32.MaxValue;
        public int PID;
        public int ID;
        public string Name;
        public Dictionary<int, Node> Nodes = new Dictionary<int, Node>();

        public override string ToString()
        {
            return $"PID: {PID}, ID: {ID}, Name: {Name}";
        }

        public bool AddEdge(int source, int target, string reason)
        {
            if (Nodes.TryGetValue(source, out Node a) && Nodes.TryGetValue(target, out Node b))
            {
                AddReason(a.Targets, b, reason);
                AddReason(b.Sources, a, reason);
            }
            else
            {
                return false;
            }
            return true;
        }

        public void AddConditionalEdge(int reason1, int reason2, int target, string reason)
        {
            Node reason1Node = Nodes[reason1];
            Node reason2Node = Nodes[reason2];
            Node dependee = Nodes[target];

            int conditionalNodeIndex = NextConditionalNodeIndex--;
            Node conditionalNode = new Node(conditionalNodeIndex, String.Format("Conditional({0} - {1})", reason1Node.ToString(), reason2Node.ToString()));
            Nodes.Add(conditionalNodeIndex, conditionalNode);

            AddReason(conditionalNode.Targets, dependee, reason);
            AddReason(dependee.Sources, conditionalNode, reason);

            AddReason(reason1Node.Targets, conditionalNode, "Reason1Conditional - " + reason);
            AddReason(conditionalNode.Sources, reason1Node, "Reason1Conditional - " + reason);

            AddReason(reason2Node.Targets, conditionalNode, "Reason2Conditional - " + reason);
            AddReason(conditionalNode.Sources, reason2Node, "Reason2Conditional - " + reason);
        }

        public void AddNode(int index, string name)
        {
            Node n = new Node(index, name);
            this.Nodes.Add(index, n);
        }

        public void AddReason(Dictionary<Node, List<string>> dict, Node node, string reason)
        {
            if (dict.TryGetValue(node, out List<string> reasons))
            {
                reasons.Add(reason);
            }
            else
            {
                dict.Add(node, new List<string> { reason });
            }
        }

        bool IsValidNode(int nodeID)
        {
            return (Nodes.ContainsKey(nodeID));
        }
    }

    public class GraphCollection
    {
        public static readonly GraphCollection Singleton = new GraphCollection();
        public static DependencyGraphs DependencyGraphsUI;
        public List<Graph> Graphs = new List<Graph>();

        public void AddGraph(Graph g)
        {
            Graphs.Add(g);
            if (DependencyGraphsUI != null)
            {
                DependencyGraphsUI.ForceRefresh();
            }
        }

        public void RemoveGraph(Graph g)
        {
            Graphs.Remove(g);
            if (DependencyGraphsUI != null)
            {
                DependencyGraphsUI.ForceRefresh();
            }
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

            g.AddNode(index, name);
        }

        public bool AddEdgeToGraph(int pid, int id, int source, int target, string reason)
        {
            Graph g = GetGraph(pid, id);
            if (g == null)
                return false;
            return (g.AddEdge(source, target, reason));
        }

        public void AddConditionalEdgeToGraph(int pid, int id, int reason1, int reason2, int target, string reason)
        {
            Graph g = GetGraph(pid, id);
            if (g == null)
                return;
            g.AddConditionalEdge(reason1, reason2, target, reason);
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

    public class DGMLGraphProcessing
    {
        ConcurrentQueue<GraphEvent> events = new ConcurrentQueue<GraphEvent>();
        GraphCollection collection = GraphCollection.Singleton;
        public delegate void OnCompleted(int currFileID);
        public event OnCompleted Complete;
        public Graph g = null;
        private Stream _stream;
        private string _name;

        internal DGMLGraphProcessing(int file)
        {
            FileID = file;
            Graph g = new Graph();
        }

        public static bool StartProcess(int fileID, string argPath)
        {
            var dgml = new DGMLGraphProcessing(fileID);
            dgml._name = argPath;
            GraphCollection collection = GraphCollection.Singleton;
            dgml.Complete += (fileID) =>
            {
                lock (collection)
                {
                    collection.AddGraph(dgml.g);
                }
                Debug.Assert(fileID == dgml.FileID);
            };
            return dgml.FindXML(argPath);
        }

        public int FileID
        {
            get; init;
        }

        private bool FindXML(string argPath)
        {
            FileStream fileStream = null;
            if (argPath != null)
            {
                try
                {
                    fileStream = new FileStream(argPath, FileMode.Open);
                }
                catch (Exception e)
                {
                    string _errorMsg = $"Failure to open file {argPath} \n{e}";
                    Console.WriteLine(_errorMsg);
                    return false;
                }
            }
            else
            {
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Filter = @"XML (*.xml)|*.xml| DGML (*.dgml; *.dgml.xml)|*.dgml;*.dgml.xml";
                    openFileDialog.FilterIndex = 2;
                    openFileDialog.RestoreDirectory = true;

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        //Get the path of specified file
                        fileStream = (FileStream)openFileDialog.OpenFile();
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return FindXML(fileStream, fileStream.Name);
        }

        internal bool FindXML(Stream stream, string name)
        {
            if (stream != Stream.Null)
            {
                _stream = stream;
                _name = name;
                Thread th = new Thread(ProcessingMain);
                th.Start(this);
                return true;
            }
            return false;
        }

        public static void ProcessingMain(object obj)
        {
            GraphCollection collection = GraphCollection.Singleton;

            var writer = (DGMLGraphProcessing)obj;
            if (!writer.ParseXML(writer._stream, writer._name))
                DependencyGraphs.showError("Nonexistent nodes present in Links");
        }

        internal bool ParseXML(Stream fileStream, string name)
        {
            Debug.Assert(fileStream is Stream);
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreWhitespace = true;

            g = new Graph();

            using (XmlReader reader = XmlReader.Create(fileStream, settings))
            {
                while (reader.Read())
                {
                    if ((reader.Name == "Property" || reader.Name == "Properties"))
                        continue;

                    if (reader.NodeType != XmlNodeType.Element)
                        continue;
                    // fileID is the PID for the system, increments down with the number of files being read.
                    // There is the same PID and ID because each process will only have one graph, this is why the first two args are the same for each of the functions below.
                    switch (reader.Name)
                    {
                        case "Node":
                            int id = int.Parse(reader.GetAttribute("Id"));
                            g.AddNode(id, reader.GetAttribute("Label"));
                            break;
                        case "Link":
                            int source = int.Parse(reader.GetAttribute("Source"));
                            int target = int.Parse(reader.GetAttribute("Target"));
                            if (!g.AddEdge(source, target, reader.GetAttribute("Reason")))
                            {
                                return false;
                            }
                            break;
                        case "DirectedGraph":
                            g.ID = FileID;
                            g.PID = FileID;
                            g.Name = _name;
                            break;
                    }
                }
            }
            fileStream.Close();
            if (Complete != null)
            {
                Complete(FileID);
            }
            return true;
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
                                    Graph g = new Graph();
                                    g.PID = eventRead.Pid;
                                    g.ID = eventRead.Id;
                                    g.Name = eventRead.Str;
                                    collection.AddGraph(g);
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
#nullable enable
        [STAThread]
        static int Main(string[] args)
        {
            string? argPath = args.Length > 0 ? args[0] : null;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            GraphCollection.DependencyGraphsUI = new DependencyGraphs(argPath);

            Application.Run(GraphCollection.DependencyGraphsUI);

            return 0;
        }
#nullable restore
    }
}
