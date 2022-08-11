// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using DependencyLogViewer;
using System.Windows.Forms.Design.Behavior;
using System.Xml.Linq;
using System.Windows.Forms;


namespace DependecyGraphViewer.Tests
{
    public class TestFileParsing
    {
        GraphCollection collection = GraphCollection.Singleton;

        public static IEnumerable<object[]> GetDgml()
        {
            // no nodes, no edges
            yield return new object[] { """
                                        <?xml version="1.0" encoding="utf-8"?>
                                        <DirectedGraph Layout="ForceDirected" xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                                        </DirectedGraph>
                                        """, 0, true, 0 };
            // only nodes, no edges
            yield return new object[] { """
                                        <?xml version="1.0" encoding="utf-8"?>
                                        <DirectedGraph Layout="ForceDirected" xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                                          <Nodes>
                                            <Node Id="0" Bounds="116.735416024609,55.9601260459141,59.1066666666667,25.96" Label="Node 0" />
                                            <Node Id="1" Bounds="27.6286493579427,55.96,59.1066666666667,25.96" Label="Node 1" />
                                            <Node Id="2" Bounds="0,111.92,59.1066666666667,25.96" Label="Node 2" />
                                            <Node Id="3" Bounds="1.92761207529202E-06,0,59.1066666666667,25.96" Label="Node 3" />
                                          </Nodes>
                                        </DirectedGraph>
                                        """, 4, true, 0 };
            // nodes and edges
            yield return new object[] { """
                                        <?xml version="1.0" encoding="utf-8"?>
                                        <DirectedGraph Layout="ForceDirected" xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                                          <Nodes>
                                            <Node Id="0" Bounds="116.735416024609,55.9601260459141,59.1066666666667,25.96" Label="Node 0" />
                                            <Node Id="1" Bounds="27.6286493579427,55.96,59.1066666666667,25.96" Label="Node 1" />
                                            <Node Id="2" Bounds="0,111.92,59.1066666666667,25.96" Label="Node 2" />
                                            <Node Id="3" Bounds="1.92761207529202E-06,0,59.1066666666667,25.96" Label="Node 3" />
                                          </Nodes>
                                          <Links>
                                            <Link Source="1" Target="0" Bounds="86.7353160246094,68.9400418046469,21.000100000009,2.97056766669357E-05" Reason="" />
                                            <Link Source="2" Target="1" Bounds="35.9618370621771,89.9900100194777,10.827305282256,21.9299899805223" Reason="" />
                                            <Link Source="3" Target="1" Bounds="35.9618385426768,25.96,10.8273044723694,21.9299898701751" Reason="reloc" Stroke="#FFFF0000" />
                                            <Link Source="3" Target="2" Bounds="25.9704322814941,25.9599990844727,2.85780334472656,76.9909744262695" Reason="reloc" Stroke="#FFFF0000" />
                                          </Links>
                                          <Properties>
                                            <Property Id="Bounds" DataType="System.Windows.Rect" />
                                            <Property Id="Label" Label="Label" Description="Displayable label of an Annotatable object" DataType="System.String" />
                                            <Property Id="Layout" DataType="System.String" />
                                            <Property Id="Reason" Label="Reason" DataType="System.String" />
                                            <Property Id="Stroke" DataType="System.Windows.Media.Brush" />
                                          </Properties>
                                        </DirectedGraph>
                                        """, 4, true, 4 };
            // invalid graph
            yield return new object[] { """
                                        <?xml version="1.0" encoding="utf-8"?>
                                        <DirectedGraph Layout="ForceDirected" xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                                            <Nodes>
                                            </Nodes>
                                            <Links>
                                            <Link Source="1" Target="0" Bounds="86.7353160246094,68.9400418046469,21.000100000009,2.97056766669357E-05" Reason="" />
                                            </Links>
                                            <Properties>
                                            </Properties>
                                        </DirectedGraph>
                                        """, 0, false, 0 };
        }

        [Theory]
        [MemberData(nameof(GetDgml))]
        public void NumberOfNodes(string fileContents, int nodeCount, bool isValid, int linkCount)
        {
            var stream = GenerateStreamFromString(fileContents);
            DGMLGraphProcessing testParser = new DGMLGraphProcessing(-1);
            testParser.ParseXML(stream, "testFile");
            Assert.Equal(testParser.g.Nodes.Count, nodeCount);
        }

        [Theory]
        [MemberData(nameof(GetDgml))]
        public void NumberOfLinks(string fileContents, int nodeCount, bool isValid, int linkCount)
        {
            int sumLinks = 0;
            var stream = GenerateStreamFromString(fileContents);
            DGMLGraphProcessing testParser = new DGMLGraphProcessing(-1);
            testParser.ParseXML(stream, "testFile");
            foreach (int ID in testParser.g.Nodes.Keys)
            {
                sumLinks += testParser.g.Nodes[ID].Targets.Count;
            }
            Assert.Equal(sumLinks, linkCount);
        }

        [Fact]
        public void MultiLink()
        {
            string fileContents = """
                <?xml version="1.0" encoding="utf-8"?>
                <DirectedGraph Layout="ForceDirected" xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                  <Nodes>
                    <Node Id="0" Label="Node 0" />
                    <Node Id="1" Label="Node 1" />
                  </Nodes>
                  <Links>
                    <Link Source="1" Target="0" Reason="first" />
                    <Link Source="1" Target="0" Reason="second" />
                    <Link Source="0" Target="1" Reason="third" />
                  </Links>
                  <Properties>
                    <Property Id="Bounds" DataType="System.Windows.Rect" />
                  </Properties>
                </DirectedGraph>
                """;
            var stream = GenerateStreamFromString(fileContents);
            DGMLGraphProcessing testLink = new DGMLGraphProcessing(-1);
            testLink.ParseXML(stream, "testFile");

            Assert.Equal(testLink.g.Nodes[0].Sources.Count, 1);
            Node node0 = testLink.g.Nodes[0];
            Node node1 = testLink.g.Nodes[1];
            Assert.Equal(node0.Sources[node1], new List<string> { "first", "second" });
            Assert.Equal(node0.Targets[node1], new List<string> { "third" });
            Assert.Equal(node1.Sources[node0], new List<string> { "third" });
            Assert.Equal(node1.Targets[node0], new List<string> { "first", "second" });
        }

        [Fact]
        public void DependsOn()
        {
            string fileContents = """
                <?xml version="1.0" encoding="utf-8"?>
                <DirectedGraph Layout="ForceDirected" xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                  <Nodes>
                    <Node Id="0" Bounds="116.735416024609,55.9601260459141,59.1066666666667,25.96" Label="Node 0" />
                    <Node Id="1" Bounds="27.6286493579427,55.96,59.1066666666667,25.96" Label="Node 1" />
                    <Node Id="2" Bounds="0,111.92,59.1066666666667,25.96" Label="Node 2" />
                    <Node Id="3" Bounds="1.92761207529202E-06,0,59.1066666666667,25.96" Label="Node 3" />
                  </Nodes>
                  <Links>
                    <Link Source="1" Target="0" Bounds="86.7353160246094,68.9400418046469,21.000100000009,2.97056766669357E-05" Reason="" />
                    <Link Source="2" Target="1" Bounds="35.9618370621771,89.9900100194777,10.827305282256,21.9299899805223" Reason="" />
                    <Link Source="3" Target="1" Bounds="35.9618385426768,25.96,10.8273044723694,21.9299898701751" Reason="reloc" Stroke="#FFFF0000" />
                    <Link Source="3" Target="2" Bounds="25.9704322814941,25.9599990844727,2.85780334472656,76.9909744262695" Reason="reloc" Stroke="#FFFF0000" />
                  </Links>
                  <Properties>
                    <Property Id="Bounds" DataType="System.Windows.Rect" />
                    <Property Id="Label" Label="Label" Description="Displayable label of an Annotatable object" DataType="System.String" />
                    <Property Id="Layout" DataType="System.String" />
                    <Property Id="Reason" Label="Reason" DataType="System.String" />
                    <Property Id="Stroke" DataType="System.Windows.Media.Brush" />
                  </Properties>
                </DirectedGraph>
                """;
            var stream = GenerateStreamFromString(fileContents);
            DGMLGraphProcessing testParser = new DGMLGraphProcessing(-1);
            testParser.ParseXML(stream, "testFile");

            Assert.Equal(testParser.g.Nodes[0].Sources.Count, 1);
            Assert.Contains(testParser.g.Nodes[0].Sources.Keys, (s) => s.Name.Equals("Node 1"));

            Assert.Equal(testParser.g.Nodes[0].Targets.Count, 0);

            Assert.Equal(testParser.g.Nodes[1].Sources.Count, 2);
            Assert.Contains(testParser.g.Nodes[1].Sources.Keys, (s) => s.Name.Equals("Node 2"));
            Assert.Contains(testParser.g.Nodes[1].Sources.Keys, (s) => s.Name.Equals("Node 3"));

            Assert.Equal(testParser.g.Nodes[1].Targets.Count, 1);
            Assert.Contains(testParser.g.Nodes[1].Targets.Keys, (s) => s.Name.Equals("Node 0"));

            Assert.Equal(testParser.g.Nodes[2].Sources.Count, 1);
            Assert.Contains(testParser.g.Nodes[2].Sources.Keys, (s) => s.Name.Equals("Node 3"));

            Assert.Equal(testParser.g.Nodes[2].Targets.Count, 1);
            Assert.Contains(testParser.g.Nodes[2].Targets.Keys, (s) => s.Name.Equals("Node 1"));

            Assert.Equal(testParser.g.Nodes[3].Sources.Count, 0);

            Assert.Equal(testParser.g.Nodes[3].Targets.Count, 2);
            Assert.Contains(testParser.g.Nodes[3].Targets.Keys, (s) => s.Name.Equals("Node 1"));
            Assert.Contains(testParser.g.Nodes[3].Targets.Keys, (s) => s.Name.Equals("Node 2"));
        }

        static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}
