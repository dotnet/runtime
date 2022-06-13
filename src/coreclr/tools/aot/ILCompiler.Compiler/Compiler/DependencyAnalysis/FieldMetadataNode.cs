// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;
using EcmaField = Internal.TypeSystem.Ecma.EcmaField;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a field that has metadata generated in the current compilation.
    /// </summary>
    /// <remarks>
    /// Only expected to be used during ILScanning when scanning for reflection.
    /// </remarks>
    internal class FieldMetadataNode : DependencyNodeCore<NodeFactory>
    {
        private readonly FieldDesc _field;

        public FieldMetadataNode(FieldDesc field)
        {
            Debug.Assert(field.IsTypicalFieldDefinition);
            _field = field;
        }

        public FieldDesc Field => _field;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            DependencyList dependencies = new DependencyList();
            dependencies.Add(factory.TypeMetadata((MetadataType)_field.OwningType), "Owning type metadata");

            CustomAttributeBasedDependencyAlgorithm.AddDependenciesDueToCustomAttributes(ref dependencies, factory, ((EcmaField)_field));

            return dependencies;
        }
        protected override string GetName(NodeFactory factory)
        {
            return "Field metadata: " + _field.ToString();
        }

        protected override void OnMarked(NodeFactory factory)
        {
            Debug.Assert(!factory.MetadataManager.IsReflectionBlocked(_field));
            Debug.Assert(factory.MetadataManager.CanGenerateMetadata(_field));
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory) => null;
    }
}
