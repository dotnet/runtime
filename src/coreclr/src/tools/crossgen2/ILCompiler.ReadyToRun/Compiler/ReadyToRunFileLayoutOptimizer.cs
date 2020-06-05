// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Internal.TypeSystem;

using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysis.ReadyToRun;
using ILCompiler.DependencyAnalysisFramework;
using System.Linq;
using System.Collections.Immutable;
using System.Text;
using System.Reflection.Metadata.Ecma335;

namespace ILCompiler
{
    public enum ReadyToRunMethodLayoutAlgorithm
    {
        DefaultSort,
        ExclusiveWeight,
        HotCold,
        HotWarmCold
    }

    public enum ReadyToRunFileLayoutAlgorithm
    {
        DefaultSort,
        MethodOrder
    }

    class ReadyToRunFileLayoutOptimizer
    {
        public ReadyToRunFileLayoutOptimizer (ReadyToRunMethodLayoutAlgorithm methodAlgorithm,
                                              ReadyToRunFileLayoutAlgorithm fileAlgorithm,
                                              ProfileDataManager profileData,
                                              NodeFactory nodeFactory)
        {
            _methodLayoutAlgorithm = methodAlgorithm;
            _fileLayoutAlgorithm = fileAlgorithm;
            _profileData = profileData;
            _nodeFactory = nodeFactory;
        }

        private ReadyToRunMethodLayoutAlgorithm _methodLayoutAlgorithm = ReadyToRunMethodLayoutAlgorithm.DefaultSort;
        private ReadyToRunFileLayoutAlgorithm _fileLayoutAlgorithm = ReadyToRunFileLayoutAlgorithm.DefaultSort;
        private ProfileDataManager _profileData;
        private NodeFactory _nodeFactory;

        private List<MethodWithGCInfo> SortByRegion(ImmutableArray<DependencyNodeCore<NodeFactory>> nodes, Func<MethodWithGCInfo, int> getRegion)
        {
            List<MethodWithGCInfo> methods = new List<MethodWithGCInfo>();
            foreach (var node in nodes)
            {
                if (node is MethodWithGCInfo method)
                {
                    methods.Add(method);
                }
            }
            methods.MergeSortAllowDuplicates((MethodWithGCInfo left, MethodWithGCInfo right) => getRegion(left).CompareTo(getRegion(right)));
            return methods;
        }

        public ImmutableArray<DependencyNodeCore<NodeFactory>> ApplyProfilerGuidedMethodSort(ImmutableArray<DependencyNodeCore<NodeFactory>> nodes)
        {
            if (_methodLayoutAlgorithm == ReadyToRunMethodLayoutAlgorithm.DefaultSort)
                return nodes;

            Func<List<MethodWithGCInfo>> sortedMethods = null;

            if (_methodLayoutAlgorithm == ReadyToRunMethodLayoutAlgorithm.ExclusiveWeight)
            {
                sortedMethods = () =>
                {
                    List<MethodWithGCInfo> methods = new List<MethodWithGCInfo>();
                    foreach (var node in nodes)
                    {
                        if (node is MethodWithGCInfo method)
                        {
                            methods.Add(method);
                        }
                    }

                    methods.MergeSortAllowDuplicates(sortMethodWithGCInfoByWeight);
                    return methods;
                };

                int sortMethodWithGCInfoByWeight(MethodWithGCInfo left, MethodWithGCInfo right)
                {
                    return -MethodWithGCInfoToWeight(left).CompareTo(MethodWithGCInfoToWeight(right));
                }
                double MethodWithGCInfoToWeight(MethodWithGCInfo method)
                {
                    var profileData = _profileData[method.Method];
                    double weight = 0;

                    if (profileData != null)
                    {
                        weight = profileData.ExclusiveWeight;
                    }
                    return weight;
                }
            }
            else if (_methodLayoutAlgorithm == ReadyToRunMethodLayoutAlgorithm.HotCold)
            {
                sortedMethods = () => SortByRegion(nodes, (method) =>
                {
                    var profileData = _profileData[method.Method];
                    double weight = 0;

                    if (profileData != null)
                    {
                        weight = profileData.ExclusiveWeight;
                    }
                    return weight > 0 ? 0 : 1;
                });
            }
            else if (_methodLayoutAlgorithm == ReadyToRunMethodLayoutAlgorithm.HotWarmCold)
            {
                sortedMethods = () => SortByRegion(nodes, (method) =>
                {
                    var profileData = _profileData[method.Method];
                    double weight = 0;

                    if (profileData != null)
                    {
                        weight = profileData.ExclusiveWeight;
                    }
                    // If weight is greater than 128 its probably signicantly used at runtime
                    if (weight > 128)
                        return 0;

                    // If weight is less than 128 but greater than 0, then its probably used at startup
                    // or some at runtime, but is less critical than the hot code
                    if (weight > 0)
                        return 1;

                    // Methods without weight are probably relatively rarely used
                    return 2;
                });
            }

            int sortOrder = 0;

            List<MethodWithGCInfo> sortedMethodsList = sortedMethods();

            foreach (var methodNode in sortedMethodsList)
            {
                methodNode.CustomSort = sortOrder;
                sortOrder++;
            }

            if (_fileLayoutAlgorithm == ReadyToRunFileLayoutAlgorithm.MethodOrder)
            {
                // Sort the dependencies of methods by the method order
                foreach (var method in sortedMethodsList)
                {
                    ApplySortToDependencies(method, 0);
                }
            }

            var newNodesArray = nodes.ToArray();
            newNodesArray.MergeSortAllowDuplicates(new SortableDependencyNode.ObjectNodeComparer(new CompilerComparer()));
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
    }
}
