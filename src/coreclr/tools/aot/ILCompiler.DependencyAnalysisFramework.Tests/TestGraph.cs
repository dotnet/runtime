// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ILCompiler.DependencyAnalysisFramework;

namespace ILCompiler.DependencyAnalysisFramework.Tests
{
    class TestGraph
    {
        public class TestNode : ComputedStaticDependencyNode<TestGraph>
        {
            private readonly string _data;
            private static readonly CombinedDependencyListEntry[] s_emptyDynamicList = new CombinedDependencyListEntry[0];

            public TestNode(string data)
            {
                _data = data;
            } 

            public string Data
            {
                get
                {
                    return _data;
                }
            }

            protected override string GetName(TestGraph context)
            {
                return _data;
            }

            public override bool HasDynamicDependencies
            {
                get
                {
                    return true;
                }
            }

            public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<TestGraph>> markedNodes, int firstNode, TestGraph context)
            {
                if (context._dynamicDependencyComputer == null)
                    return s_emptyDynamicList;

                IEnumerable<CombinedDependencyListEntry> returnValue = s_emptyDynamicList;
                List<CombinedDependencyListEntry> returnValueWithData = null;
                for (int i = firstNode; i < markedNodes.Count; i++)
                {
                    Tuple<string,string> nextResult = context._dynamicDependencyComputer(this.Data, ((TestNode)markedNodes[i]).Data);

                    if (nextResult != null)
                    {
                        if (returnValueWithData == null)
                        {
                            returnValueWithData = new List<DependencyNodeCore<TestGraph>.CombinedDependencyListEntry>();
                            returnValue = returnValueWithData;
                        }

                        returnValueWithData.Add(new CombinedDependencyListEntry(context.GetNode(nextResult.Item1), markedNodes[i], nextResult.Item2));
                    }
                }

                return returnValue;
            }
        }

        Dictionary<string, HashSet<Tuple<string, string>>> _staticNonConditionalRules = new Dictionary<string, HashSet<Tuple<string, string>>>();
        Dictionary<string, HashSet<Tuple<string, Tuple<string, string>>>> _staticConditionalRules = new Dictionary<string, HashSet<Tuple<string, Tuple<string, string>>>>();

        Func<string, string, Tuple<string,string>> _dynamicDependencyComputer;
        Dictionary<string, TestNode> _nodes = new Dictionary<string, TestNode>();
        DependencyAnalyzerBase<TestGraph> _analyzer;

        public void AddStaticRule(string depender, string dependedOn, string reason)
        {
            HashSet<Tuple<string, string>> knownEdges = null;
            if (!_staticNonConditionalRules.TryGetValue(depender, out knownEdges))
            {
                knownEdges = new HashSet<Tuple<string, string>>();
                _staticNonConditionalRules[depender] = knownEdges;
            }
            knownEdges.Add(new Tuple<string,string>(dependedOn, reason));
        }

        public void AddConditionalRule(string depender, string otherdepender, string dependedOn, string reason)
        {
            HashSet<Tuple<string, Tuple<string, string>>> knownEdges = null;
            if (!_staticConditionalRules.TryGetValue(depender, out knownEdges))
            {
                knownEdges = new HashSet<Tuple<string, Tuple<string, string>>>();
                _staticConditionalRules[depender] = knownEdges;
            }

            knownEdges.Add(new Tuple<string, Tuple<string, string>>(dependedOn, new Tuple<string,string>(otherdepender, reason)));
        }

        public void SetDynamicDependencyRule(Func<string, string, Tuple<string, string>> dynamicDependencyComputer)
        {
            _dynamicDependencyComputer = dynamicDependencyComputer;
        }

        public void AddRoot(string nodeNode, string reason)
        {
            _analyzer.AddRoot(this.GetNode(nodeNode), reason);
        }

        public TestNode GetNode(string nodeName)
        {
            TestNode node;
            if (!_nodes.TryGetValue(nodeName, out node))
            {
                node = new TestNode(nodeName);
                _nodes.Add(nodeName, node);
            }

            return node;
        }

        public void AttachToDependencyAnalyzer(DependencyAnalyzerBase<TestGraph> analyzer)
        {
            analyzer.ComputeDependencyRoutine += Analyzer_ComputeDependencyRoutine;
            _analyzer = analyzer;
        }

        private void Analyzer_ComputeDependencyRoutine(List<DependencyNodeCore<TestGraph>> obj)
        {
            foreach (TestNode node in obj)
            {
                List<TestNode.DependencyListEntry> staticList = new List<DependencyNodeCore<TestGraph>.DependencyListEntry>();
                List<TestNode.CombinedDependencyListEntry> conditionalStaticList = new List<DependencyNodeCore<TestGraph>.CombinedDependencyListEntry>();

                HashSet<Tuple<string, string>> nonConditionalRules;
                if (_staticNonConditionalRules.TryGetValue(node.Data, out nonConditionalRules))
                {
                    foreach (Tuple<string, string> dependedOn in nonConditionalRules)
                    {
                        staticList.Add(new TestNode.DependencyListEntry(GetNode(dependedOn.Item1), dependedOn.Item2));
                    }
                }

                HashSet<Tuple<string, Tuple<string, string>>> conditionalRules;
                if (_staticConditionalRules.TryGetValue(node.Data, out conditionalRules))
                {
                    foreach (Tuple<string, Tuple<string, string>> dependedOn in conditionalRules)
                    {
                        conditionalStaticList.Add(new TestNode.CombinedDependencyListEntry(GetNode(dependedOn.Item1), GetNode(dependedOn.Item2.Item1), dependedOn.Item2.Item2));
                    }
                }

                node.SetStaticDependencies(staticList, conditionalStaticList);
            }
        }

        public List<string> AnalysisResults
        {
            get
            {
                List<string> liveNodes = new List<string>();

                _analyzer.ComputeMarkedNodes();

                foreach (var node in _analyzer.MarkedNodeList)
                {
                    liveNodes.Add(((TestNode)node).Data);
                }

                return liveNodes;
            }
        }
    }
}
