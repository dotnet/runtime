// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents an event that has metadata generated in the current compilation.
    /// This corresponds to an ECMA-335 Event record. Unlike fields and methods,
    /// events are not first-class entities in the compiler's type system (there is no
    /// interned EventDesc).
    /// </summary>
    /// <remarks>
    /// Only expected to be used during ILScanning when scanning for reflection.
    /// </remarks>
    internal sealed class EventMetadataNode : DependencyNodeCore<NodeFactory>
    {
        private readonly EventPseudoDesc _event;

        public EventMetadataNode(EventPseudoDesc @event)
        {
            _event = @event;
        }

        public EventPseudoDesc Event => _event;

        public override void AddStaticDependencies(DependencySink<NodeFactory> sink, NodeFactory factory) { }

        public override void AddConditionalDependencies(DependencySink<NodeFactory> sink, NodeFactory factory)
        {
            DependencySink<NodeFactory> dependencies = sink;
            CustomAttributeBasedDependencyAlgorithm.AddDependenciesDueToCustomAttributes(dependencies, factory, _event);
        }

        protected override string GetName(NodeFactory factory)
        {
            return "Event metadata: " + _event.ToString();
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => true;
        public override bool StaticDependenciesAreComputed => true;
        public override void SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, DependencySink<NodeFactory> sink, NodeFactory factory) { }
    }
}
