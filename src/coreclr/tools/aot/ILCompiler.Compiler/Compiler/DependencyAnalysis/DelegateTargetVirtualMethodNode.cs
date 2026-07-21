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
        private readonly bool _reflected;

        public DelegateTargetVirtualMethodNode(MethodDesc method, bool reflected)
        {
            Debug.Assert(method.GetCanonMethodTarget(CanonicalFormKind.Specific) == method);
            _method = method;
            _reflected = reflected;
        }

        protected override string GetName(NodeFactory factory)
        {
            return (_reflected ? "Reflected delegate target method:" : "Delegate target method: ") + _method.ToString();
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory) => null;
        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory) => null;
    }

    public sealed class ReflectableVirtualMethodImplNode : DependencyNodeCore<NodeFactory>
    {
        private readonly MethodDesc _declaration;
        private readonly MethodDesc _implementation;

        public ReflectableVirtualMethodImplNode(MethodDesc declaration, MethodDesc implementation)
        {
            Debug.Assert(declaration.GetCanonMethodTarget(CanonicalFormKind.Specific) == declaration);
            Debug.Assert(implementation.GetCanonMethodTarget(CanonicalFormKind.Specific) == implementation);

            _declaration = declaration;
            _implementation = implementation;
        }

        protected override string GetName(NodeFactory factory)
        {
            return $"Reflectable virtual method implementation: {_implementation} for {_declaration}";
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory) => null;
        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => true;
        public override bool StaticDependenciesAreComputed => true;

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory)
        {
            yield return new CombinedDependencyListEntry(
                factory.ReflectedMethod(_implementation),
                factory.ReflectedDelegateTargetVirtualMethod(_declaration),
                "Virtual method declaration is reflectable");
        }

        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory) => null;
    }
}
