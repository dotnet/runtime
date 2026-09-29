// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents the fact that object.GetType was called on a location typed as this
    /// type or one of the subtypes of it.
    /// </summary>
    internal sealed class ObjectGetTypeCalledNode : DependencyNodeCore<NodeFactory>
    {
        private readonly MetadataType _type;

        public ObjectGetTypeCalledNode(MetadataType type)
        {
            _type = type;
        }

        protected override string GetName(NodeFactory factory)
        {
            return $"Object.GetType called on {_type}";
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory) => null;
        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory) => null;

    }
}
