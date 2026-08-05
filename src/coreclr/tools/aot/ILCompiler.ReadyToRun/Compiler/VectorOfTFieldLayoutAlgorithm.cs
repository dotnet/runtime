// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.TypeSystem;

namespace ILCompiler
{
    internal sealed class VectorOfTFieldLayoutAlgorithm : FieldLayoutAlgorithm
    {
        private FieldLayoutAlgorithm _fallbackAlgorithm;
        private FieldLayoutAlgorithm _vectorFallbackAlgorithm;
        private byte[] _similarVectorName;
        private DefType _similarVectorOpenType;

        public VectorOfTFieldLayoutAlgorithm(FieldLayoutAlgorithm fallbackAlgorithm, FieldLayoutAlgorithm vectorFallbackAlgorithm, ReadOnlySpan<byte> similarVector)
        {
            _fallbackAlgorithm = fallbackAlgorithm;
            _vectorFallbackAlgorithm = vectorFallbackAlgorithm;
            _similarVectorName = similarVector.ToArray();
        }

        private DefType GetSimilarVector(DefType vectorOfTType)
        {
            if (_similarVectorOpenType == null)
            {
                if (_similarVectorName.SequenceEqual("Unknown"u8))
                    return null;

                _similarVectorOpenType = ((MetadataType)vectorOfTType.GetTypeDefinition()).Module.GetType("System.Runtime.Intrinsics"u8, _similarVectorName);
            }

            return ((MetadataType)_similarVectorOpenType).MakeInstantiatedType(vectorOfTType.Instantiation);
        }

        public override bool ComputeContainsGCPointers(DefType type)
        {
            return false;
        }

        public override bool ComputeContainsByRefs(DefType type)
        {
            return false;
        }

        public override bool ComputeIsUnsafeValueType(DefType type)
        {
            return false;
        }

        public override ComputedInstanceFieldLayout ComputeInstanceLayout(DefType type, InstanceLayoutKind layoutKind)
        {
            DefType similarSpecifiedVector = GetSimilarVector(type);
            if (similarSpecifiedVector == null)
            {
                List<FieldAndOffset> fieldsAndOffsets = new List<FieldAndOffset>();
                foreach (FieldDesc field in type.GetFields())
                {
                    if (!field.IsStatic)
                    {
                        fieldsAndOffsets.Add(new FieldAndOffset(field, LayoutInt.Indeterminate));
                    }
                }
                ComputedInstanceFieldLayout instanceLayout = new ComputedInstanceFieldLayout()
                {
                    FieldSize = LayoutInt.Indeterminate,
                    FieldAlignment = LayoutInt.Indeterminate,
                    ByteCountUnaligned = LayoutInt.Indeterminate,
                    ByteCountAlignment = LayoutInt.Indeterminate,
                    Offsets = fieldsAndOffsets.ToArray(),
                    LayoutAbiStable = true,
                    IsVectorTOrHasVectorTFields = true,
                };
                return instanceLayout;
            }
            else
            {
                ComputedInstanceFieldLayout layoutFromMetadata = _fallbackAlgorithm.ComputeInstanceLayout(type, layoutKind);
                ComputedInstanceFieldLayout layoutFromSimilarIntrinsicVector = _vectorFallbackAlgorithm.ComputeInstanceLayout(similarSpecifiedVector, layoutKind);

                // TODO, enable this code when we switch Vector<T> to follow the same calling convention as its matching similar intrinsic vector
#if MATCHING_HARDWARE_VECTOR
                return new ComputedInstanceFieldLayout
                {
                    ByteCountUnaligned = layoutFromSimilarIntrinsicVector.ByteCountUnaligned,
                    ByteCountAlignment = layoutFromSimilarIntrinsicVector.ByteCountAlignment,
                    FieldAlignment = layoutFromSimilarIntrinsicVector.FieldAlignment,
                    FieldSize = layoutFromSimilarIntrinsicVector.FieldSize,
                    Offsets = layoutFromMetadata.Offsets,
                    LayoutAbiStable = true,
                    IsVectorTOrHasVectorTFields = true,
                };
#else
                return new ComputedInstanceFieldLayout
                {
                    ByteCountUnaligned = layoutFromSimilarIntrinsicVector.ByteCountUnaligned,
                    ByteCountAlignment = layoutFromMetadata.ByteCountAlignment,
                    // On wasm Vector<T> is passed as a v128 and must share its 16-byte alignment.
                    FieldAlignment = type.Context.Target.Architecture == TargetArchitecture.Wasm32
                        ? layoutFromSimilarIntrinsicVector.FieldAlignment
                        : layoutFromMetadata.FieldAlignment,
                    FieldSize = layoutFromSimilarIntrinsicVector.FieldSize,
                    Offsets = layoutFromMetadata.Offsets,
                    LayoutAbiStable = true,
                    IsVectorTOrHasVectorTFields = true,
                };
#endif
            }
        }

        public override ComputedStaticFieldLayout ComputeStaticFieldLayout(DefType type, StaticLayoutKind layoutKind)
        {
            return _fallbackAlgorithm.ComputeStaticFieldLayout(type, layoutKind);
        }

        public override ValueTypeShapeCharacteristics ComputeValueTypeShapeCharacteristics(DefType type)
        {
            if (type.Context.Target.Architecture == TargetArchitecture.ARM64 &&
                VectorFieldLayoutAlgorithm.IsSupportedVectorBaseType(type.Instantiation[0]))
            {
                return type.InstanceFieldSize.AsInt switch
                {
                    16 => ValueTypeShapeCharacteristics.Vector128Aggregate,
                    _ => ValueTypeShapeCharacteristics.None
                };
            }
            return ValueTypeShapeCharacteristics.None;
        }

        public static bool IsVectorOfTType(DefType type)
        {
            return type.IsIntrinsic && type.Namespace == "System.Numerics"u8 && type.Name == "Vector`1"u8;
        }
    }
}
