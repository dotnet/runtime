// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using ILCompiler.DependencyAnalysisFramework;

namespace ILCompiler.DependencyAnalysis
{
    class DeferredTillPhaseNode : DependencyNodeCore<NodeFactory>
    {
        private readonly int _phase;
        private readonly List<DependencyNodeCore<NodeFactory>> _dependencies = new List<DependencyNodeCore<NodeFactory>>();
        private bool _dependenciesNoLongerMutable;

        public DeferredTillPhaseNode(int phase)
        {
            Debug.Assert(phase > 0);
            _phase = phase;
        }

        public void NotifyCurrentPhase(int newPhase)
        {
            if (newPhase >= _phase)
                _dependenciesNoLongerMutable = true;
        }

        public void AddDependency(DependencyNodeCore<NodeFactory> newDependency)
        {
            if (_dependenciesNoLongerMutable)
                throw new Exception();

            _dependencies.Add(newDependency);
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;

        public override bool HasDynamicDependencies => false;

        public override bool HasConditionalStaticDependencies => false;

        public override bool StaticDependenciesAreComputed => _dependenciesNoLongerMutable;

        public override void AddConditionalDependencies(DependencySink<NodeFactory> sink, NodeFactory context) { }
        public override void AddStaticDependencies(DependencySink<NodeFactory> sink, NodeFactory context)
        {
            foreach (var dependencyNode in _dependencies)
            {
                sink.Add(new DependencyNodeCore<NodeFactory>.DependencyListEntry(dependencyNode, "DeferredDependency"));
            }
        }
        public override void SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, DependencySink<NodeFactory> sink, NodeFactory context) => throw new NotImplementedException();
        protected override string GetName(NodeFactory context) => $"DeferredTillPhaseNode {_phase}";

        public override int DependencyPhaseForDeferredStaticComputation => _phase;
    }
}
