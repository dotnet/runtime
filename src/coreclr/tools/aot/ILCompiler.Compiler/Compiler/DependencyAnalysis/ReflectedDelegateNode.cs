// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a delegate type that was reflected on using <see cref="System.Delegate.Method"/>.
    /// </summary>
    public class ReflectedDelegateNode : DependencyNodeCore<NodeFactory>
    {
        private readonly TypeDesc _delegateType;

        public ReflectedDelegateNode(TypeDesc delegateType)
        {
            Debug.Assert(delegateType is null || delegateType.IsDelegate);

            // We accept 3 kinds of types:
            // * Null: Delegate.get_Method was used on an unknown type. All delegate targets are reflection-visible.
            // * A type definition: An unknown instantiation of the delegate was reflected on. Typically caused by dataflow analysis analyzing uninstantiated code.
            // * Canonical form: Some canonical form was reflected on.
            Debug.Assert(delegateType is null
                || delegateType.IsGenericDefinition
                || delegateType.ConvertToCanonForm(CanonicalFormKind.Specific) == delegateType);

            _delegateType = delegateType;
        }

        protected override string GetName(NodeFactory factory)
        {
            return "Reflectable delegate type: " + _delegateType?.ToString() ?? "All delegates";
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
