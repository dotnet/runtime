// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Runtime;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    internal sealed class GenericDefinitionEETypeNode : EETypeNode
    {
        public GenericDefinitionEETypeNode(NodeFactory factory, TypeDesc type) : base(factory, type)
        {
            Debug.Assert(type.IsGenericDefinition);
        }

        public override bool ShouldSkipEmittingObjectNode(NodeFactory factory)
        {
            return false;
        }

        protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory factory)
        {
            DependencyList dependencyList = null;

            // Ask the metadata manager if we have any dependencies due to reflectability.
            factory.MetadataManager.GetDependenciesDueToReflectability(ref dependencyList, factory, _type);

            return dependencyList;
        }

        protected internal override void ComputeOptionalEETypeFields(NodeFactory factory, bool relocsOnly)
        {
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataBuilder dataBuilder = new ObjectDataBuilder(factory, relocsOnly);

            dataBuilder.RequireInitialPointerAlignment();
            dataBuilder.AddSymbol(this);
            EETypeRareFlags rareFlags = 0;

            uint flags = EETypeBuilderHelpers.ComputeFlags(_type);
            if (factory.PreinitializationManager.HasLazyStaticConstructor(_type))
                rareFlags |= EETypeRareFlags.HasCctorFlag;
            if (_type.IsByRefLike)
                rareFlags |= EETypeRareFlags.IsByRefLikeFlag;

            if (rareFlags != 0)
                _optionalFieldsBuilder.SetFieldValue(EETypeOptionalFieldTag.RareFlags, (uint)rareFlags);

            if (HasOptionalFields)
                flags |= (uint)EETypeFlags.OptionalFieldsFlag;

            flags |= (uint)EETypeFlags.HasComponentSizeFlag;
            flags |= (ushort)_type.Instantiation.Length;

            dataBuilder.EmitUInt(flags);
            dataBuilder.EmitInt(0);         // Base size is always 0
            dataBuilder.EmitZeroPointer();  // No related type
            dataBuilder.EmitShort(0);       // No VTable
            dataBuilder.EmitShort(0);       // No interface map
            dataBuilder.EmitInt(_type.GetHashCode());
            OutputTypeManagerIndirection(factory, ref dataBuilder);
            OutputWritableData(factory, ref dataBuilder);
            OutputOptionalFields(factory, ref dataBuilder);

            return dataBuilder.ToObjectData();
        }

        public override int ClassCode => -160325006;
    }
}
