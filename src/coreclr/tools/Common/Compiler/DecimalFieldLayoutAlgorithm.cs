// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    /// <summary>
    /// Represents an algorithm that computes field layout for the IEEE 754 decimal floating-point
    /// types (Decimal32/Decimal64/Decimal128). Decimal128 shares the 16-byte packing requirement of
    /// Int128/UInt128; Decimal32/Decimal64 keep the natural alignment of their underlying scalar.
    /// </summary>
    public class DecimalFieldLayoutAlgorithm : FieldLayoutAlgorithm
    {
        private readonly FieldLayoutAlgorithm _fallbackAlgorithm;

        public DecimalFieldLayoutAlgorithm(FieldLayoutAlgorithm fallbackAlgorithm)
        {
            _fallbackAlgorithm = fallbackAlgorithm;
        }

        public override ComputedInstanceFieldLayout ComputeInstanceLayout(DefType defType, InstanceLayoutKind layoutKind)
        {
            Debug.Assert(IsDecimalFloatingPointType(defType));

            ComputedInstanceFieldLayout layoutFromMetadata = _fallbackAlgorithm.ComputeInstanceLayout(defType, layoutKind);

            // Only Decimal128 corresponds to a 16-byte ABI primitive (_Decimal128) requiring the packing
            // applied to Int128/UInt128; Decimal32/Decimal64 keep the natural alignment of uint/ulong.
            // ARM32 has no such primitive in its PCS, so it uses the metadata layout as Int128 does;
            // every other target is 16-byte aligned, matching the Int128/UInt128 treatment.
            if (defType.Name != "Decimal128"u8
                || defType.Context.Target.Architecture == TargetArchitecture.ARM)
            {
                layoutFromMetadata.LayoutAbiStable = true;
                layoutFromMetadata.IsDecimalFloatingPointOrHasDecimalFloatingPointFields = true;
                return layoutFromMetadata;
            }

            return new ComputedInstanceFieldLayout
            {
                ByteCountUnaligned = layoutFromMetadata.ByteCountUnaligned,
                ByteCountAlignment = layoutFromMetadata.ByteCountAlignment,
                FieldAlignment = new LayoutInt(16),
                FieldSize = layoutFromMetadata.FieldSize,
                Offsets = layoutFromMetadata.Offsets,
                LayoutAbiStable = true,
                IsDecimalFloatingPointOrHasDecimalFloatingPointFields = true
            };
        }

        public override ComputedStaticFieldLayout ComputeStaticFieldLayout(DefType defType, StaticLayoutKind layoutKind)
        {
            return _fallbackAlgorithm.ComputeStaticFieldLayout(defType, layoutKind);
        }

        public override bool ComputeContainsGCPointers(DefType type)
        {
            Debug.Assert(!_fallbackAlgorithm.ComputeContainsGCPointers(type));
            return false;
        }

        public override bool ComputeContainsByRefs(DefType type)
        {
            Debug.Assert(!_fallbackAlgorithm.ComputeContainsByRefs(type));
            return false;
        }

        public override bool ComputeIsUnsafeValueType(DefType type)
        {
            Debug.Assert(!_fallbackAlgorithm.ComputeIsUnsafeValueType(type));
            return false;
        }

        public override ValueTypeShapeCharacteristics ComputeValueTypeShapeCharacteristics(DefType type)
        {
            Debug.Assert(_fallbackAlgorithm.ComputeValueTypeShapeCharacteristics(type) == ValueTypeShapeCharacteristics.None);
            return ValueTypeShapeCharacteristics.None;
        }

        public static bool IsDecimalFloatingPointType(DefType type)
        {
            return type.IsIntrinsic
                && type.Namespace == "System.Numerics"u8
                && (type.Name == "Decimal32"u8 || type.Name == "Decimal64"u8 || type.Name == "Decimal128"u8);
        }
    }
}
