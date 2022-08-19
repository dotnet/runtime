// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    /// <summary>
    /// Represents an algorithm that computes field layout for intrinsic integer types (Int128/UInt128).
    /// </summary>
    public class Int128FieldLayoutAlgorithm : FieldLayoutAlgorithm
    {
        private readonly FieldLayoutAlgorithm _fallbackAlgorithm;

        public Int128FieldLayoutAlgorithm(FieldLayoutAlgorithm fallbackAlgorithm)
        {
            _fallbackAlgorithm = fallbackAlgorithm;
        }

        public override ComputedInstanceFieldLayout ComputeInstanceLayout(DefType defType, InstanceLayoutKind layoutKind)
        {
            Debug.Assert(IsIntegerType(defType));

            string name = defType.Name;
            Debug.Assert((name == "Int128") || (name == "UInt128"));

            ComputedInstanceFieldLayout layoutFromMetadata = _fallbackAlgorithm.ComputeInstanceLayout(defType, layoutKind);

            // 32bit platforms use standard metadata layout engine
            if (defType.Context.Target.Architecture == TargetArchitecture.ARM)
            {
                layoutFromMetadata.LayoutAbiStable = false; // Int128 parameter passing ABI is unstable at this time
                layoutFromMetadata.IsInt128OrHasInt128Fields = true;
                return layoutFromMetadata;
            }

            // 64-bit Unix systems follow the System V ABI and have a 16-byte packing requirement for Int128/UInt128

            return new ComputedInstanceFieldLayout
            {
                ByteCountUnaligned = layoutFromMetadata.ByteCountUnaligned,
                ByteCountAlignment = layoutFromMetadata.ByteCountAlignment,
                FieldAlignment = new LayoutInt(16),
                FieldSize = layoutFromMetadata.FieldSize,
                Offsets = layoutFromMetadata.Offsets,
                LayoutAbiStable = false, // Int128 parameter passing ABI is unstable at this time
                IsInt128OrHasInt128Fields = true
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

        public static bool IsIntegerType(DefType type)
        {
            return type.IsIntrinsic
                && type.Namespace == "System"
                && ((type.Name == "Int128") || (type.Name == "UInt128"));
        }
    }
}
