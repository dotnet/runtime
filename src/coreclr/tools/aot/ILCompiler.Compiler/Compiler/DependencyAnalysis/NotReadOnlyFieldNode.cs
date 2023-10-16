// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a field that cannot be optimized to `readonly`.
    /// </summary>
    public class NotReadOnlyFieldNode : DependencyNodeCore<NodeFactory>
    {
        private readonly FieldDesc _field;

        public NotReadOnlyFieldNode(FieldDesc field)
        {
            Debug.Assert(!field.OwningType.IsCanonicalSubtype(CanonicalFormKind.Any)
                || field.OwningType.ConvertToCanonForm(CanonicalFormKind.Specific) == field.OwningType);
            _field = field;
        }

        public FieldDesc Field => _field;

        protected override string GetName(NodeFactory factory)
        {
            return "Field written outside initializer: " + _field.ToString();
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory) => null;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory) => null;
    }
}
