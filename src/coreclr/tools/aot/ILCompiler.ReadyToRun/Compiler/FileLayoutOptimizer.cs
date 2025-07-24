// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Internal.TypeSystem;

using ILCompiler.DependencyAnalysis;
#if READYTORUN
using ILCompiler.DependencyAnalysis.ReadyToRun;
#endif
using ILCompiler.DependencyAnalysisFramework;
using System.Linq;
using System.Collections.Immutable;
using System.Text;
using System.Reflection.Metadata.Ecma335;
using ILCompiler.PettisHansenSort;

#if !READYTORUN
using MethodWithGCInfo = ILCompiler.DependencyAnalysis.MethodCodeNode;
#endif

namespace ILCompiler
{
    public enum MethodLayoutAlgorithm
    {
        DefaultSort,
        ExclusiveWeight,
        HotCold,
        InstrumentedHotCold,
        HotWarmCold,
#if READYTORUN
        CallFrequency,
#endif
        PettisHansen,
        Random,
        Explicit,
    }

    public enum FileLayoutAlgorithm
    {
        DefaultSort,
        MethodOrder,
    }

    class FileLayoutOptimizer
    {
        public FileLayoutOptimizer (Logger logger,
                                              MethodLayoutAlgorithm methodAlgorithm,
                                              FileLayoutAlgorithm fileAlgorithm,
                                              ProfileDataManager profileData,
                                              NodeFactory nodeFactory,
                                              string orderFile = null)
        {
            _logger = logger;
            _methodLayoutAlgorithm = methodAlgorithm;
            _fileLayoutAlgorithm = fileAlgorithm;
            _profileData = profileData;
            _nodeFactory = nodeFactory;
            _orderFile = orderFile;
        }

        private Logger _logger;
        private MethodLayoutAlgorithm _methodLayoutAlgorithm = MethodLayoutAlgorithm.DefaultSort;
        private FileLayoutAlgorithm _fileLayoutAlgorithm = FileLayoutAlgorithm.DefaultSort;
        private ProfileDataManager _profileData;
        private NodeFactory _nodeFactory;
        private string _orderFile;

        public ImmutableArray<DependencyNodeCore<NodeFactory>> ApplyProfilerGuidedMethodSort(ImmutableArray<DependencyNodeCore<NodeFactory>> nodes)
        {
            if (_methodLayoutAlgorithm == MethodLayoutAlgorithm.DefaultSort)
                return nodes;

            List<MethodWithGCInfo> methods = new List<MethodWithGCInfo>();
            foreach (var node in nodes)
            {
                if (node is MethodWithGCInfo method)
                {
                    methods.Add(method);
                }
            }

            methods = ApplyMethodSort(methods);

            int sortOrder = 0;

            List<MethodWithGCInfo> sortedMethodsList = methods;

            foreach (var methodNode in sortedMethodsList)
            {
                methodNode.CustomSort = sortOrder;
#if READYTORUN
                MethodColdCodeNode methodColdCodeNode = methodNode.ColdCodeNode;
                if (methodColdCodeNode != null)
                {
                    methodColdCodeNode.CustomSort = sortOrder + sortedMethodsList.Count;
                }
#endif
                sortOrder++;
            }

            if (_fileLayoutAlgorithm == FileLayoutAlgorithm.MethodOrder)
            {
                // Sort the dependencies of methods by the method order
                foreach (var method in sortedMethodsList)
                {
                    ApplySortToDependencies(method, 0);
                }
            }

            var newNodesArray = nodes.ToArray();
            newNodesArray.MergeSortAllowDuplicates(new SortableDependencyNode.ObjectNodeComparer(CompilerComparer.Instance));
            return newNodesArray.ToImmutableArray();

            void ApplySortToDependencies(DependencyNodeCore<NodeFactory> node, int depth)
            {
                if (depth > 5)
                    return;

                if (node is SortableDependencyNode sortableNode)
                {
                    if (sortableNode.CustomSort != Int32.MaxValue)
                        return; // Node already sorted
                    sortableNode.CustomSort += sortOrder++;
                }
                foreach (var dependency in node.GetStaticDependencies(_nodeFactory))
                {
                    ApplySortToDependencies(dependency.Node, depth + 1);
                }
            }
        }

        private List<MethodWithGCInfo> ApplyMethodSort(List<MethodWithGCInfo> methods)
        {
            switch (_methodLayoutAlgorithm)
            {
                case MethodLayoutAlgorithm.DefaultSort:
                    break;

                case MethodLayoutAlgorithm.ExclusiveWeight:
                    methods.MergeSortAllowDuplicates(sortMethodWithGCInfoByWeight);

                    int sortMethodWithGCInfoByWeight(MethodWithGCInfo left, MethodWithGCInfo right)
                    {
                        return -MethodWithGCInfoToWeight(left).CompareTo(MethodWithGCInfoToWeight(right));
                    }
                    break;

                case MethodLayoutAlgorithm.HotCold:
                    methods.MergeSortAllowDuplicates((MethodWithGCInfo left, MethodWithGCInfo right) => ComputeHotColdRegion(left).CompareTo(ComputeHotColdRegion(right)));

                    int ComputeHotColdRegion(MethodWithGCInfo method)
                    {
                        return MethodWithGCInfoToWeight(method) > 0 ? 0 : 1;
                    }
                    break;

                case MethodLayoutAlgorithm.InstrumentedHotCold:
                    methods.MergeSortAllowDuplicates((MethodWithGCInfo left, MethodWithGCInfo right) => (_profileData[left.Method] != null).CompareTo(_profileData[right.Method] != null));
                    break;

                case MethodLayoutAlgorithm.HotWarmCold:
                    methods.MergeSortAllowDuplicates((MethodWithGCInfo left, MethodWithGCInfo right) => ComputeHotWarmColdRegion(left).CompareTo(ComputeHotWarmColdRegion(right)));

                    int ComputeHotWarmColdRegion(MethodWithGCInfo method)
                    {
                        double weight = MethodWithGCInfoToWeight(method);

                        // If weight is greater than 128 its probably signicantly used at runtime
                        if (weight > 128)
                            return 0;

                        // If weight is less than 128 but greater than 0, then its probably used at startup
                        // or some at runtime, but is less critical than the hot code
                        if (weight > 0)
                            return 1;

                        // Methods without weight are probably relatively rarely used
                        return 2;
                    };
                    break;

#if READYTORUN
                case MethodLayoutAlgorithm.CallFrequency:
                    methods = MethodCallFrequencySort(methods);
                    break;
#endif

                case MethodLayoutAlgorithm.PettisHansen:
                    methods = PettisHansenSort(methods);
                    break;

                case MethodLayoutAlgorithm.Random:
                    Random rand = new Random(0);
                    for (int i = 0; i < methods.Count - 1; i++)
                    {
                        int j = rand.Next(i, methods.Count);
                        MethodWithGCInfo temp = methods[i];
                        methods[i] = methods[j];
                        methods[j] = temp;
                    }
                    break;

                case MethodLayoutAlgorithm.Explicit:
                    var nameMap = new Dictionary<string, MethodWithGCInfo>(methods.Count);
                    var order = new Dictionary<MethodWithGCInfo, int>(methods.Count);

                    for (int i = 0; i < methods.Count; i++)
                    {
                        nameMap[methods[i].GetMangledName(_nodeFactory.NameMangler)] = methods[i];
                        order[methods[i]] = int.MaxValue;
                    }

                    using (StreamReader sr = new StreamReader(_orderFile))
                    {
                        int line = 0;
                        while (!sr.EndOfStream)
                        {
                            string symbolName = sr.ReadLine();
                            if (string.IsNullOrEmpty(symbolName)
                                || !nameMap.TryGetValue(symbolName, out MethodWithGCInfo m))
                                continue;

                            order[m] = line++;
                        }
                    }

                    methods.MergeSortAllowDuplicates((MethodWithGCInfo left, MethodWithGCInfo right) => order[left].CompareTo(order[right]));
                    break;

                default:
                    throw new NotImplementedException(_methodLayoutAlgorithm.ToString());
            }

            return methods;
        }

        private double MethodWithGCInfoToWeight(MethodWithGCInfo method)
        {
            var profileData = _profileData[method.Method];
            double weight = 0;

            if (profileData != null)
            {
                weight = profileData.ExclusiveWeight;
            }
            return weight;
        }

        private class CallerCalleeCount
        {
            public readonly MethodDesc Caller;
            public readonly MethodDesc Callee;
            public readonly int Count;

            public CallerCalleeCount(MethodDesc caller, MethodDesc callee, int count)
            {
                Caller = caller;
                Callee = callee;
                Count = count;
            }
        }

#if READYTORUN
        /// <summary>
        /// Use callchain profile information to generate method ordering. We place
        /// callers and callees by traversing the caller-callee pairs in the callchain
        /// profile in the order of descending hit count. All methods not present
        /// (or not matched) in the callchain profile go last.
        /// </summary>
        /// <param name="methodsToPlace">List of methods to place</param>
        private List<MethodWithGCInfo> MethodCallFrequencySort(List<MethodWithGCInfo> methodsToPlace)
        {
            if (_profileData.CallChainProfile == null)
            {
                return methodsToPlace;
            }

            Dictionary<MethodDesc, MethodWithGCInfo> methodMap = new Dictionary<MethodDesc, MethodWithGCInfo>();
            foreach (MethodWithGCInfo methodWithGCInfo in methodsToPlace)
            {
                methodMap.Add(methodWithGCInfo.Method, methodWithGCInfo);
            }

            List<CallerCalleeCount> callList = new List<CallerCalleeCount>();
            foreach (KeyValuePair<MethodDesc, Dictionary<MethodDesc, int>> methodProfile in _profileData.CallChainProfile.ResolvedProfileData.Where(kvp => methodMap.ContainsKey(kvp.Key)))
            {
                foreach (KeyValuePair<MethodDesc, int> callee in methodProfile.Value.Where(kvp => methodMap.ContainsKey(kvp.Key)))
                {
                    callList.Add(new CallerCalleeCount(methodProfile.Key, callee.Key, callee.Value));
                }
            }
            callList.Sort((a, b) => b.Count.CompareTo(a.Count));

            List<MethodWithGCInfo> outputMethods = new List<MethodWithGCInfo>();
            outputMethods.Capacity = methodsToPlace.Count;

            foreach (CallerCalleeCount call in callList)
            {
                if (methodMap.TryGetValue(call.Caller, out MethodWithGCInfo callerWithGCInfo) && callerWithGCInfo != null)
                {
                    outputMethods.Add(callerWithGCInfo);
                    methodMap[call.Caller] = null;
                }
                if (methodMap.TryGetValue(call.Callee, out MethodWithGCInfo calleeWithGCInfo) && calleeWithGCInfo != null)
                {
                    outputMethods.Add(calleeWithGCInfo);
                    methodMap[call.Callee] = null;
                }
            }

            // Methods unknown to the callchain profile go last
            outputMethods.AddRange(methodMap.Values.Where(m => m != null));
            Debug.Assert(outputMethods.Count == methodsToPlace.Count);
            return outputMethods;
        }
#endif

        /// <summary>
        /// Sort methods with Pettis-Hansen using call graph data from profile.
        /// </summary>
        private List<MethodWithGCInfo> PettisHansenSort(List<MethodWithGCInfo> methodsToPlace)
        {
            var graphNodes = new List<CallGraphNode>(methodsToPlace.Count);
            var mdToIndex = new Dictionary<MethodDesc, int>();
            int index = 0;
            foreach (MethodWithGCInfo method in methodsToPlace)
            {
                mdToIndex.Add(method.Method, index);
                graphNodes.Add(new CallGraphNode(index));
                index++;
            }

            bool any = false;
            foreach (MethodWithGCInfo method in methodsToPlace)
            {
                MethodProfileData data = _profileData[method.Method];
                if (data == null || data.CallWeights == null)
                    continue;

                foreach ((MethodDesc other, int count) in data.CallWeights)
                {
                    if (!mdToIndex.TryGetValue(other, out int otherIndex))
                        continue;

                    graphNodes[mdToIndex[method.Method]].IncreaseEdge(graphNodes[otherIndex], count);
                    any = true;
                }
            }

            if (!any)
            {
#if READYTORUN
                _logger.Writer.WriteLine("Warning: no call graph data was found or a .mibc file was not specified. Skipping Pettis Hansen method ordering.");
#endif
                return methodsToPlace;
            }

            List<List<int>> components = PettisHansen.Sort(graphNodes);
            // We expect to see a permutation.
            Debug.Assert(components.SelectMany(l => l).OrderBy(i => i).SequenceEqual(Enumerable.Range(0, methodsToPlace.Count)));

            List<MethodWithGCInfo> result = new List<MethodWithGCInfo>(methodsToPlace.Count);
            foreach (List<int> component in components)
            {
                foreach (int node in component)
                    result.Add(methodsToPlace[node]);
            }

            return result;
        }
    }
}
