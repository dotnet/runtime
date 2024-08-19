// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

using Internal.TypeSystem;
using Internal.Runtime;

namespace ILCompiler.DependencyAnalysis
{
    internal sealed class CanonicalDefinitionEETypeNode : EETypeNode
    {
        public CanonicalDefinitionEETypeNode(NodeFactory factory, TypeDesc type) : base(factory, type)
        {
            Debug.Assert(type.IsCanonicalDefinitionType(CanonicalFormKind.Any));
        }

        public override bool ShouldSkipEmittingObjectNode(NodeFactory factory) => false;
        public override bool StaticDependenciesAreComputed => true;
        public override bool IsShareable => true;
        protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory factory) => null;
        protected override int GCDescSize => 0;

        // Canonical definition types will have their base size set to the minimum
        protected override int BaseSize => MinimumObjectSize;

        public override int ClassCode => -1851030036;
    }
}
