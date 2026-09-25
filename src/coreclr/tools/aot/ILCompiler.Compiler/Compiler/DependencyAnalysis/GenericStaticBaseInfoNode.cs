// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents an entry in a hashtable that contains information about static bases of generic types.
    /// </summary>
    internal sealed class GenericStaticBaseInfoNode : DependencyNodeCore<NodeFactory>
    {
        public MetadataType Type { get; }

        public GenericStaticBaseInfoNode(MetadataType type)
        {
            Debug.Assert(!type.IsCanonicalSubtype(CanonicalFormKind.Any));
            Debug.Assert(type.HasInstantiation);
            Type = type;
        }

        public override void AddStaticDependencies(DependencySink<NodeFactory> sink, NodeFactory factory)
        {
            DependencySink<NodeFactory> dependencies = sink;
            StaticsInfoHashtableNode.AddStaticsInfoDependencies(dependencies, factory, Type);
        }

        protected override string GetName(NodeFactory factory)
        {
            return "Static base info: " + Type.ToString();
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override void AddConditionalDependencies(DependencySink<NodeFactory> sink, NodeFactory factory) { }
        public override void SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, DependencySink<NodeFactory> sink, NodeFactory factory) { }
    }
}
