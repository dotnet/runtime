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

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            var dependencies = new DependencyList();
            StaticsInfoHashtableNode.AddStaticsInfoDependencies(ref dependencies, factory, Type);
            return dependencies;
        }

        protected override string GetName(NodeFactory factory)
        {
            return "Static base info: " + Type.ToString();
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory) => null;
    }
}
