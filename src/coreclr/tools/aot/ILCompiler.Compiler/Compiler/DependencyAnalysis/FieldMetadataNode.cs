// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.Dataflow;
using ILCompiler.DependencyAnalysisFramework;
using ILCompiler.Logging;

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;
using EcmaField = Internal.TypeSystem.Ecma.EcmaField;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a field that has metadata generated in the current compilation.
    /// This corresponds to a ECMA-335 FieldDef record. It is however not a 1:1
    /// mapping because a field could be used in the AOT compiled program without generating
    /// the reflection metadata for it (which would not be possible in IL terms).
    /// </summary>
    /// <remarks>
    /// Only expected to be used during ILScanning when scanning for reflection.
    /// </remarks>
    internal sealed class FieldMetadataNode : DependencyNodeCore<NodeFactory>
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

            if (_field is EcmaField ecmaField)
            {
                DynamicDependencyAttributesOnEntityNode.AddDependenciesDueToDynamicDependencyAttribute(ref dependencies, factory, ecmaField);

                // On a reflectable field, perform generic data flow for the field's type
                // This is a compensation for the DI issue described in https://github.com/dotnet/runtime/issues/81358
                GenericArgumentDataFlow.ProcessGenericArgumentDataFlow(ref dependencies, factory, new MessageOrigin(_field), ecmaField.FieldType, ecmaField.OwningType);
            }

            if (_field.HasEmbeddedSignatureData)
            {
                foreach (var sigData in _field.GetEmbeddedSignatureData())
                    if (sigData.type != null)
                        TypeMetadataNode.GetMetadataDependencies(ref dependencies, factory, sigData.type, "Modifier in a field signature");
            }

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
