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

            Node n = new Node(index, name);
            g.Nodes.Add(index, n);
        }

        public void AddEdgeToGraph(int pid, int id, int source, int target, string reason)
        {
            Graph g = GetGraph(pid, id);
            if (g == null)
                return;

            Node dependent = g.Nodes[source];
            Node dependee = g.Nodes[target];

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
        ConcurrentQueue<GraphEvent> events = new ConcurrentQueue<GraphEvent>();

        public delegate void OnCompleted(int currFileID);
        public event OnCompleted Complete;

        public DGMLGraphProcessing(int file)
        {
            FileID = file;
        }
        public int FileID
        {
            get; init;
        }

        public bool FindXML(string argPath)
        {
            var fileStream = Stream.Null;
            if (argPath != null) 
            {
                try
                {
                    fileStream = new FileStream(argPath, FileMode.Open);
                }
                catch (FileNotFoundException e)
                {
                    string _errorMsg = $"Unable to find file. Check file extension. \n {e}";
                    DependencyGraphs.ShowErrorMessage(DependencyGraphs.Destination.Console, _errorMsg);
                    return false;
                }
                catch (UnauthorizedAccessException e)
                {
                    string _errorMsg = $"Specify a file, not a path \n {e}";
                    DependencyGraphs.ShowErrorMessage(DependencyGraphs.Destination.Console, _errorMsg);
                    return false;
                } catch (Exception e)
                {
                    string _errorMsg = $"\n{e}";
                    DependencyGraphs.ShowErrorMessage(DependencyGraphs.Destination.Console, _errorMsg);
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
                        fileStream = openFileDialog.OpenFile();
                    }
                }
            }

            if (fileStream != Stream.Null)
            {
                Thread th = new Thread(ParseXML);
                th.Start(fileStream);
                return true;
            }
            return false;
        }

        private void ParseXML(object fileStream)
        {
            Debug.Assert(fileStream is FileStream);
            FileStream fileContents = (FileStream)fileStream;
            GraphCollection collection = GraphCollection.Singleton;
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreWhitespace = true;

            using (XmlReader reader = XmlReader.Create(fileContents, settings))
            {
                while (reader.Read())
                {
                    if (!(reader.Name.StartsWith("P"))) // skips over 'properties' in the .dgml file
                    {
                        IXmlLineInfo lineInfo = (IXmlLineInfo)reader;
                        int lineNumber = lineInfo.LineNumber;
                        switch (reader.NodeType)
                        // fileID is the PID for the system, increments down with the number of files being read.
                        // There is the same PID and ID because each process will only have one graph, this is why the first two args are the same for each of the functions below.
                        {
                            case XmlNodeType.Element:
                                if (reader.Name == "Node")
                                {
                                    int id = int.Parse(reader.GetAttribute("Id"));
                                    collection.AddNodeToGraph(FileID, FileID, id, reader.GetAttribute("Label"));
                                }
                                else if (reader.Name == "Link")
                                {
                                    int source = int.Parse(reader.GetAttribute("Source"));
                                    int target = int.Parse(reader.GetAttribute("Target"));
                                    collection.AddEdgeToGraph(FileID, FileID, source, target, reader.GetAttribute("Reason"));
                                }
                                else if (reader.Name == "DirectedGraph")
                                {
                                    collection.AddGraph(FileID, FileID, fileContents.Name);
                                    break;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            fileContents.Close();
            Complete(FileID);
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
