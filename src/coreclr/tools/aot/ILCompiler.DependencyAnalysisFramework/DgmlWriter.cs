// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Diagnostics;

namespace ILCompiler.DependencyAnalysisFramework
{
    public class DgmlWriter
    {
        public static void WriteDependencyGraphToStream<DependencyContextType>(Stream stream, DependencyAnalyzerBase<DependencyContextType> analysis, DependencyContextType context)
        {
            DgmlWriter<DependencyContextType>.WriteDependencyGraphToStream(stream, analysis, context);
        }
    }

    internal sealed class DgmlWriter<DependencyContextType> : IDisposable, IDependencyAnalyzerLogEdgeVisitor<DependencyContextType>, IDependencyAnalyzerLogNodeVisitor<DependencyContextType>
    {
        private XmlWriter _xmlWrite;
        private bool _done;
        private DependencyContextType _context;

        public DgmlWriter(XmlWriter xmlWrite, DependencyContextType context)
        {
            _xmlWrite = xmlWrite;
            _xmlWrite.WriteStartDocument();
            _xmlWrite.WriteStartElement("DirectedGraph", "http://schemas.microsoft.com/vs/2009/dgml");
            _context = context;
        }

        public void WriteNodesAndEdges(Action nodeWriter, Action edgeWriter)
        {
            _xmlWrite.WriteStartElement("Nodes");
            {
                nodeWriter();
            }
            _xmlWrite.WriteEndElement();

            _xmlWrite.WriteStartElement("Links");
            {
                edgeWriter();
            }
            _xmlWrite.WriteEndElement();
        }

        public static void WriteDependencyGraphToStream(Stream stream, DependencyAnalyzerBase<DependencyContextType> analysis, DependencyContextType context)
        {
            XmlWriterSettings writerSettings = new XmlWriterSettings();
            writerSettings.Indent = true;
            writerSettings.IndentChars = " ";

            using (XmlWriter xmlWriter = XmlWriter.Create(stream, writerSettings))
            {
                using (var dgmlWriter = new DgmlWriter<DependencyContextType>(xmlWriter, context))
                {
                    dgmlWriter.WriteNodesAndEdges(() =>
                    {
                        analysis.VisitLogNodes(dgmlWriter);
                    },
                    () =>
                    {
                        analysis.VisitLogEdges(dgmlWriter);
                    }
                    );
                }
            }
        }

        public void Close()
        {
            if (!_done)
            {
                _done = true;
                _xmlWrite.WriteStartElement("Properties");
                {
                    _xmlWrite.WriteStartElement("Property");
                    _xmlWrite.WriteAttributeString("Id", "Label");
                    _xmlWrite.WriteAttributeString("Label", "Label");
                    _xmlWrite.WriteAttributeString("DataType", "String");
                    _xmlWrite.WriteEndElement();

                    _xmlWrite.WriteStartElement("Property");
                    _xmlWrite.WriteAttributeString("Id", "Reason");
                    _xmlWrite.WriteAttributeString("Label", "Reason");
                    _xmlWrite.WriteAttributeString("DataType", "String");
                    _xmlWrite.WriteEndElement();
                }
                _xmlWrite.WriteEndElement();

                _xmlWrite.WriteEndElement();
                _xmlWrite.WriteEndDocument();
            }
        }

        void IDisposable.Dispose()
        {
            Close();
        }

        private Dictionary<object, int> _nodeMappings = new Dictionary<object, int>();
        private int _nodeNextId;

        private void AddNode(DependencyNodeCore<DependencyContextType> node)
        {
            AddNode(node, node.GetNameInternal(_context));
        }

        private void AddNode(object node, string label)
        {
            int nodeId = _nodeNextId++;
            Debug.Assert(!_nodeMappings.ContainsKey(node));

            _nodeMappings.Add(node, nodeId);

            _xmlWrite.WriteStartElement("Node");
            _xmlWrite.WriteAttributeString("Id", nodeId.ToString());
            _xmlWrite.WriteAttributeString("Label", label);
            _xmlWrite.WriteEndElement();
        }

        private void AddReason(object nodeA, object nodeB, string reason)
        {
            _xmlWrite.WriteStartElement("Link");
            _xmlWrite.WriteAttributeString("Source", _nodeMappings[nodeA].ToString());
            _xmlWrite.WriteAttributeString("Target", _nodeMappings[nodeB].ToString());
            _xmlWrite.WriteAttributeString("Reason", reason);
            _xmlWrite.WriteEndElement();
        }

        void IDependencyAnalyzerLogEdgeVisitor<DependencyContextType>.VisitEdge(DependencyNodeCore<DependencyContextType> nodeDepender, DependencyNodeCore<DependencyContextType> nodeDependedOn, string reason)
        {
            _xmlWrite.WriteStartElement("Link");
            _xmlWrite.WriteAttributeString("Source", _nodeMappings[nodeDepender].ToString());
            _xmlWrite.WriteAttributeString("Target", _nodeMappings[nodeDependedOn].ToString());
            _xmlWrite.WriteAttributeString("Reason", reason);
            _xmlWrite.WriteAttributeString("Stroke", "#FF0000");
            _xmlWrite.WriteEndElement();
        }

        void IDependencyAnalyzerLogEdgeVisitor<DependencyContextType>.VisitEdge(string root, DependencyNodeCore<DependencyContextType> dependedOn)
        {
            AddReason(root, dependedOn, null);
        }

        void IDependencyAnalyzerLogNodeVisitor<DependencyContextType>.VisitCombinedNode(Tuple<DependencyNodeCore<DependencyContextType>, DependencyNodeCore<DependencyContextType>> node)
        {
            string label1 = node.Item1.GetNameInternal(_context);
            string label2 = node.Item2.GetNameInternal(_context);

            AddNode(node, string.Concat("(", label1, ", ", label2, ")"));
        }

        private HashSet<Tuple<DependencyNodeCore<DependencyContextType>, DependencyNodeCore<DependencyContextType>>> _combinedNodesEdgeVisited = new HashSet<Tuple<DependencyNodeCore<DependencyContextType>, DependencyNodeCore<DependencyContextType>>>();

        void IDependencyAnalyzerLogEdgeVisitor<DependencyContextType>.VisitEdge(DependencyNodeCore<DependencyContextType> nodeDepender, DependencyNodeCore<DependencyContextType> nodeDependerOther, DependencyNodeCore<DependencyContextType> nodeDependedOn, string reason)
        {
            var combinedNode = new Tuple<DependencyNodeCore<DependencyContextType>, DependencyNodeCore<DependencyContextType>>(nodeDepender, nodeDependerOther);
            if (!_combinedNodesEdgeVisited.Contains(combinedNode))
            {
                _combinedNodesEdgeVisited.Add(combinedNode);

                _xmlWrite.WriteStartElement("Link");
                _xmlWrite.WriteAttributeString("Source", _nodeMappings[nodeDepender].ToString());
                _xmlWrite.WriteAttributeString("Target", _nodeMappings[combinedNode].ToString());
                _xmlWrite.WriteAttributeString("Reason", "Primary");
                _xmlWrite.WriteAttributeString("Stroke", "#00FF00");
                _xmlWrite.WriteEndElement();

                _xmlWrite.WriteStartElement("Link");
                _xmlWrite.WriteAttributeString("Source", _nodeMappings[nodeDependerOther].ToString());
                _xmlWrite.WriteAttributeString("Target", _nodeMappings[combinedNode].ToString());
                _xmlWrite.WriteAttributeString("Reason", "Secondary");
                _xmlWrite.WriteAttributeString("Stroke", "#00FF00");
                _xmlWrite.WriteEndElement();
            }

            _xmlWrite.WriteStartElement("Link");
            _xmlWrite.WriteAttributeString("Source", _nodeMappings[combinedNode].ToString());
            _xmlWrite.WriteAttributeString("Target", _nodeMappings[nodeDependedOn].ToString());
            _xmlWrite.WriteAttributeString("Reason", reason);
            _xmlWrite.WriteAttributeString("Stroke", "#0000FF");
            _xmlWrite.WriteEndElement();
        }

        void IDependencyAnalyzerLogNodeVisitor<DependencyContextType>.VisitNode(DependencyNodeCore<DependencyContextType> node)
        {
            AddNode(node);
        }

        void IDependencyAnalyzerLogNodeVisitor<DependencyContextType>.VisitRootNode(string rootName)
        {
            AddNode(rootName, rootName);
        }
    }
}
