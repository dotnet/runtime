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
        public readonly Dictionary<Node, string> Targets = new Dictionary<Node, string>();
        public readonly Dictionary<Node, string> Sources = new Dictionary<Node, string>();
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
        public bool isValid = true;
        public Dictionary<int, Node> Nodes = new Dictionary<int, Node>();
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

        public Graph GetGraph(int pid, int id)
        {
            foreach (Graph g in Graphs)
            {
                if ((g.PID == pid) && (g.ID == id))
                    return g;
            }
            return null;
        }

        public void AddNodeToGraph(int pid, int id, int index, string name, Graph g = null)
        {
            if (g == null)
            {
                g = GetGraph(pid, id);
            }
            if (g == null)
                return;

            Node n = new Node(index, name);
            g.Nodes.Add(index, n);
        }

        bool IsValidNode (Graph g, int nodeID)
        {
            if (!g.Nodes.ContainsKey(nodeID))
            {
                if (DependencyGraphsUI != null)
                    MessageBox.Show($"The .dgml is invalid. Node with ID {nodeID} is specified by a link but does not appear in node list.");
                g.isValid = false;
                return false;
            }
            return true;
        }

        public void AddEdgeToGraph(int pid, int id, int source, int target, string reason, Graph g = null)
        {
            if (g == null)
            {
                g = GetGraph(pid, id);
            }
            if (g == null)
                return;

            if (IsValidNode(g, source) && IsValidNode(g, target))
            {
                Node A = g.Nodes[source];
                Node B = g.Nodes[target];

                if (!A.Targets.ContainsKey(B)) A.Targets.Add(B, reason);
                if (!B.Sources.ContainsKey(A)) B.Sources.Add(A, reason);
            }
        }

        public void AddConditionalEdgeToGraph(int pid, int id, int reason1, int reason2, int target, string reason, Graph g = null)
        {
            if (g == null)
            {
                g = GetGraph(pid, id);
            }
            if (g == null)
                return;

            Node reason1Node = g.Nodes[reason1];
            Node reason2Node = g.Nodes[reason2];
            Node dependee = g.Nodes[target];

            int conditionalNodeIndex = g.NextConditionalNodeIndex--;
            Node conditionalNode = new Node(conditionalNodeIndex, String.Format("Conditional({0} - {1})", reason1Node.ToString(), reason2Node.ToString()));
            g.Nodes.Add(conditionalNodeIndex, conditionalNode);

            conditionalNode.Targets.Add(dependee, reason);
            dependee.Sources.Add(conditionalNode, reason);

            reason1Node.Targets.Add(conditionalNode, "Reason1Conditional - " + reason);
            conditionalNode.Sources.Add(reason1Node, "Reason1Conditional - " + reason);

            reason2Node.Targets.Add(conditionalNode, "Reason2Conditional - " + reason);
            conditionalNode.Sources.Add(reason2Node, "Reason2Conditional - " + reason);
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
                if (!dgml.g.isValid)
                    return;
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
            var writer = (DGMLGraphProcessing)obj;
            writer.ParseXML(writer._stream, writer._name);
        }


        internal void ParseXML(Stream fileStream, string name)
        {
            Debug.Assert(fileStream is Stream);
            //FileStream fileContents = (FileStream)fileStream;
            GraphCollection collection = GraphCollection.Singleton;
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
                            collection.AddNodeToGraph(FileID, FileID, id, reader.GetAttribute("Label"), g);
                            break;
                        case "Link":
                            int source = int.Parse(reader.GetAttribute("Source"));
                            int target = int.Parse(reader.GetAttribute("Target"));
                            collection.AddEdgeToGraph(FileID, FileID, source, target, reader.GetAttribute("Reason"), g);
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
        ///

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
