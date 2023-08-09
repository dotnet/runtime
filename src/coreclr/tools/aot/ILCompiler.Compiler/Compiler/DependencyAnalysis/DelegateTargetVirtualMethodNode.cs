// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a reflection-visible virtual method that is a target of a delegate.
    /// </summary>
    public class DelegateTargetVirtualMethodNode : DependencyNodeCore<NodeFactory>
    {
        private readonly MethodDesc _method;

        public DelegateTargetVirtualMethodNode(MethodDesc method)
        {
            Debug.Assert(method.GetCanonMethodTarget(CanonicalFormKind.Specific) == method);
            _method = method;
        }

        protected override string GetName(NodeFactory factory)
        {
            return "Delegate target method: " + _method.ToString();
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
