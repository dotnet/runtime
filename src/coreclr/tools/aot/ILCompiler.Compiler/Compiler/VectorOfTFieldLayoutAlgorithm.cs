// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    /// <summary>
    /// Represents an algorithm that computes field layout for the SIMD Vector&lt;T&gt; type
    /// depending on the target details.
    /// </summary>
    public class VectorOfTFieldLayoutAlgorithm : FieldLayoutAlgorithm
    {
        private readonly FieldLayoutAlgorithm _fallbackAlgorithm;

        public VectorOfTFieldLayoutAlgorithm(FieldLayoutAlgorithm fallbackAlgorithm)
        {
            _fallbackAlgorithm = fallbackAlgorithm;
        }

        public override ComputedInstanceFieldLayout ComputeInstanceLayout(DefType defType, InstanceLayoutKind layoutKind)
        {
            TargetDetails targetDetails = defType.Context.Target;

            ComputedInstanceFieldLayout layoutFromMetadata = _fallbackAlgorithm.ComputeInstanceLayout(defType, layoutKind);

            LayoutInt instanceFieldSize;

            if (targetDetails.MaximumSimdVectorLength == SimdVectorLength.Vector128Bit)
            {
                instanceFieldSize = new LayoutInt(16);
            }
            else if (targetDetails.MaximumSimdVectorLength == SimdVectorLength.Vector256Bit)
            {
                instanceFieldSize = new LayoutInt(32);
            }
            else
            {
                Debug.Assert(targetDetails.MaximumSimdVectorLength == SimdVectorLength.None);
                return layoutFromMetadata;
            }

            return new ComputedInstanceFieldLayout
            {
                ByteCountUnaligned = instanceFieldSize,
                ByteCountAlignment = layoutFromMetadata.ByteCountAlignment,
                FieldAlignment = layoutFromMetadata.FieldAlignment,
                FieldSize = instanceFieldSize,
                Offsets = layoutFromMetadata.Offsets,
            };
        }

        public unsafe override ComputedStaticFieldLayout ComputeStaticFieldLayout(DefType defType, StaticLayoutKind layoutKind)
        {
            return _fallbackAlgorithm.ComputeStaticFieldLayout(defType, layoutKind);
        }

        public override bool ComputeContainsGCPointers(DefType type)
        {
            return false;
        }

        public override ValueTypeShapeCharacteristics ComputeValueTypeShapeCharacteristics(DefType type)
        {
            if (type.Context.Target.Architecture == TargetArchitecture.ARM64 &&
                            type.Instantiation[0].IsPrimitiveNumeric)
            {
                return type.InstanceFieldSize.AsInt switch
                {
                    8 => ValueTypeShapeCharacteristics.Vector64Aggregate,
                    16 => ValueTypeShapeCharacteristics.Vector128Aggregate,
                    _ => ValueTypeShapeCharacteristics.None
                };
            }

            return _fallbackAlgorithm.ComputeValueTypeShapeCharacteristics(type);
        }

        public override bool ComputeIsUnsafeValueType(DefType type)
        {
            return false;
        }

        public static bool IsVectorOfTType(DefType type)
        {
            return type.IsIntrinsic && type.Namespace == "System.Numerics" && type.Name == "Vector`1";
        }
    }
}
