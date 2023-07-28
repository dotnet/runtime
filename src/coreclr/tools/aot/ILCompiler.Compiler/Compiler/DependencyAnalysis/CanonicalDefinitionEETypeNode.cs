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

        protected internal override void ComputeOptionalEETypeFields(NodeFactory factory, bool relocsOnly)
        {
            if (_type.IsCanonicalDefinitionType(CanonicalFormKind.Universal))
            {
                // Value types should have at least 1 byte of size to avoid zero-length structures
                // Add pointer-size to the number of instance field bytes to consistently represents the boxed size.
                uint numInstanceFieldBytes = 1 + (uint)factory.Target.PointerSize;

                uint valueTypeFieldPadding = (uint)(MinimumObjectSize - factory.Target.PointerSize) - numInstanceFieldBytes;
                uint valueTypeFieldPaddingEncoded = EETypeBuilderHelpers.ComputeValueTypeFieldPaddingFieldValue(valueTypeFieldPadding, 1, _type.Context.Target.PointerSize);
                Debug.Assert(valueTypeFieldPaddingEncoded != 0);

                _optionalFieldsBuilder.SetFieldValue(EETypeOptionalFieldTag.ValueTypeFieldPadding, valueTypeFieldPaddingEncoded);
            }
        }

        // Canonical definition types will have their base size set to the minimum
        protected override int BaseSize => MinimumObjectSize;

        public override int ClassCode => -1851030036;
    }
}
