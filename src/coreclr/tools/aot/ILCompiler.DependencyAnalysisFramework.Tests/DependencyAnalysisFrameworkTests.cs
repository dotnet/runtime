// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;

using ILCompiler.DependencyAnalysisFramework;

using Xunit;

namespace ILCompiler.DependencyAnalysisFramework.Tests
{
    public class DependencyTests
    {
        public DependencyTests()
        {
        }

        /// <summary>
        /// Test on every graph type. Used to ensure that the behavior of the various markers is consistent
        /// </summary>
        /// <param name="testGraph"></param>
        private void TestOnGraphTypes(Action<TestGraph, DependencyAnalyzerBase<TestGraph>> testGraph)
        {
            // Test using the full logging strategy
            TestGraph testGraphFull = new TestGraph();
            DependencyAnalyzerBase<TestGraph> analyzerFull = new DependencyAnalyzer<FullGraphLogStrategy<TestGraph>, TestGraph>(testGraphFull, null);
            testGraphFull.AttachToDependencyAnalyzer(analyzerFull);
            testGraph(testGraphFull, analyzerFull);

            TestGraph testGraphFirstMark = new TestGraph();
            DependencyAnalyzerBase<TestGraph> analyzerFirstMark = new DependencyAnalyzer<FirstMarkLogStrategy<TestGraph>, TestGraph>(testGraphFirstMark, null);
            testGraphFirstMark.AttachToDependencyAnalyzer(analyzerFirstMark);
            testGraph(testGraphFirstMark, analyzerFirstMark);

            TestGraph testGraphNoLog = new TestGraph();
            DependencyAnalyzerBase<TestGraph> analyzerNoLog = new DependencyAnalyzer<NoLogStrategy<TestGraph>, TestGraph>(testGraphNoLog, null);
            testGraphNoLog.AttachToDependencyAnalyzer(analyzerNoLog);
            testGraph(testGraphNoLog, analyzerNoLog);
        }

        [Fact]
        public void TestADependsOnB()
        {
            TestOnGraphTypes((TestGraph testGraph, DependencyAnalyzerBase<TestGraph> analyzer) =>
            {
                testGraph.AddStaticRule("A", "B", "A depends on B");
                testGraph.AddRoot("A", "A is root");
                List<string> results = testGraph.AnalysisResults;

                Assert.Contains("A", results);
                Assert.Contains("B", results);
            });
        }

        [Fact]
        public void TestADependsOnBIfC_NoC()
        {
            TestOnGraphTypes((TestGraph testGraph, DependencyAnalyzerBase<TestGraph> analyzer) =>
            {
                testGraph.AddConditionalRule("A", "C", "B", "A depends on B if C");
                testGraph.AddRoot("A", "A is root");
                List<string> results = testGraph.AnalysisResults;

                Assert.Contains("A", results);
                Assert.DoesNotContain("B", results);
                Assert.DoesNotContain("C", results);
                Assert.True(results.Count == 1);
            });
        }

        [Fact]
        public void TestADependsOnBIfC_HasC()
        {
            TestOnGraphTypes((TestGraph testGraph, DependencyAnalyzerBase<TestGraph> analyzer) =>
            {
                testGraph.AddConditionalRule("A", "C", "B", "A depends on B if C");
                testGraph.AddRoot("A", "A is root");
                testGraph.AddRoot("C", "C is root");
                List<string> results = testGraph.AnalysisResults;

                Assert.Contains("A", results);
                Assert.Contains("B", results);
                Assert.Contains("C", results);
                Assert.True(results.Count == 3);
            });
        }

        [Fact]
        public void TestSimpleDynamicRule()
        {
            TestOnGraphTypes((TestGraph testGraph, DependencyAnalyzerBase<TestGraph> analyzer) =>
            {
                testGraph.SetDynamicDependencyRule((string nodeA, string nodeB) =>
                {
                    if (nodeA.EndsWith("*") && nodeB.StartsWith("*"))
                    {
                        return new Tuple<string, string>(nodeA + nodeB, "DynamicRule");
                    }
                    return null;
                });

                testGraph.AddRoot("A*", "A* is root");
                testGraph.AddRoot("B*", "B* is root");
                testGraph.AddRoot("*C", "*C is root");
                testGraph.AddRoot("*D", "*D is root");
                testGraph.AddRoot("A*B", "A*B is root");
                List<string> results = testGraph.AnalysisResults;

                Assert.Contains("A*", results);
                Assert.Contains("B*", results);
                Assert.Contains("*C", results);
                Assert.Contains("*D", results);
                Assert.Contains("A*B", results);
                Assert.Contains("A**C", results);
                Assert.Contains("A**D", results);
                Assert.Contains("B**C", results);
                Assert.Contains("B**D", results);
                Assert.True(results.Count == 9);
            });
        }

        private void BuildGraphUsingAllTypesOfRules(TestGraph testGraph, DependencyAnalyzerBase<TestGraph> analyzer)
        {
            testGraph.SetDynamicDependencyRule((string nodeA, string nodeB) =>
            {
                if (nodeA.EndsWith("*") && nodeB.StartsWith("*"))
                {
                    return new Tuple<string, string>(nodeA + nodeB, "DynamicRule");
                }
                return null;
            });

            testGraph.AddConditionalRule("A**C", "B**D", "D", "A**C depends on D if B**D");
            testGraph.AddStaticRule("D", "E", "D depends on E");

            // Rules to ensure that there are some nodes that have multiple reasons to exist
            testGraph.AddStaticRule("A*", "E", "A* depends on E");
            testGraph.AddStaticRule("*C", "E", "*C depends on E");

            testGraph.AddRoot("A*", "A* is root");
            testGraph.AddRoot("B*", "B* is root");
            testGraph.AddRoot("*C", "*C is root");
            testGraph.AddRoot("*D", "*D is root");
            testGraph.AddRoot("A*B", "A*B is root");

            List<string> results = testGraph.AnalysisResults;
            Assert.Contains("A*", results);
            Assert.Contains("B*", results);
            Assert.Contains("*C", results);
            Assert.Contains("*D", results);
            Assert.Contains("A*B", results);
            Assert.Contains("A**C", results);
            Assert.Contains("A**D", results);
            Assert.Contains("B**C", results);
            Assert.Contains("B**D", results);
            Assert.Contains("D", results);
            Assert.Contains("E", results);
            Assert.True(results.Count == 11);
        }

        [Fact]
        public void TestDGMLOutput()
        {
            Dictionary<string, string> dgmlOutputs = new Dictionary<string, string>();
            TestOnGraphTypes((TestGraph testGraph, DependencyAnalyzerBase<TestGraph> analyzer) =>
            {
                BuildGraphUsingAllTypesOfRules(testGraph, analyzer);
                MemoryStream dgmlOutput = new MemoryStream();
                DgmlWriter.WriteDependencyGraphToStream(dgmlOutput, analyzer, testGraph);
                dgmlOutput.Seek(0, SeekOrigin.Begin);
                TextReader tr = new StreamReader(dgmlOutput);
                dgmlOutputs[analyzer.GetType().FullName] = tr.ReadToEnd();
            });

            foreach (var pair in dgmlOutputs)
            {
                int nodeCount = pair.Value.Split(new string[] { "<Node " }, StringSplitOptions.None).Length - 1;
                int edgeCount = pair.Value.Split(new string[] { "<Link " }, StringSplitOptions.None).Length - 1;
                if (pair.Key.Contains("FullGraph"))
                {
                    Assert.Equal(21, nodeCount);
                    Assert.Equal(23, edgeCount);
                }
                else if (pair.Key.Contains("FirstMark"))
                {
                    // There are 2 edges in the all types of rules graph that are duplicates. Note that the edge count is 
                    // 2 less than the full graph edge count
                    Assert.Equal(21, nodeCount);
                    Assert.Equal(21, edgeCount);
                }
                else
                {
                    Assert.Contains("NoLog", pair.Key);
                    Assert.Equal(11, nodeCount);
                    Assert.Equal(0, edgeCount);
                }
            }
        }
    }
}
