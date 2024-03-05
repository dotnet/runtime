// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    /// <summary>
    /// Represents an algorithm that computes field layout for intrinsic vector types (Vector64/Vector128/Vector256).
    /// </summary>
    public class VectorFieldLayoutAlgorithm : FieldLayoutAlgorithm
    {
        private readonly FieldLayoutAlgorithm _fallbackAlgorithm;
        private readonly bool _vectorAbiIsStable;

        public VectorFieldLayoutAlgorithm(FieldLayoutAlgorithm fallbackAlgorithm, bool vectorAbiIsStable = true)
        {
            _vectorAbiIsStable = vectorAbiIsStable;
            _fallbackAlgorithm = fallbackAlgorithm;
        }

        public override ComputedInstanceFieldLayout ComputeInstanceLayout(DefType defType, InstanceLayoutKind layoutKind)
        {
            Debug.Assert(IsVectorType(defType));

            LayoutInt alignment;

            string name = defType.Name;
            if (name == "Vector64`1")
            {
                alignment = new LayoutInt(8);
            }
            else if (name == "Vector128`1")
            {
                if (defType.Context.Target.Architecture == TargetArchitecture.ARM)
                {
                    // The Procedure Call Standard for ARM defaults to 8-byte alignment for __m128
                    alignment = new LayoutInt(8);
                }
                else
                {
                    alignment = new LayoutInt(16);
                }
            }
            else if (name == "Vector256`1")
            {
                if (defType.Context.Target.Architecture == TargetArchitecture.ARM)
                {
                    // No such type exists for the Procedure Call Standard for ARM. We will default
                    // to the same alignment as __m128, which is supported by the ABI.
                    alignment = new LayoutInt(8);
                }
                else if (defType.Context.Target.Architecture == TargetArchitecture.ARM64 || defType.Context.Target.Architecture == TargetArchitecture.RiscV64)
                {
                    // The Procedure Call Standard for ARM 64-bit (with SVE support) defaults to
                    // 16-byte alignment for __m256.
                    alignment = new LayoutInt(16);
                }
                else
                {
                    alignment = new LayoutInt(32);
                }
            }
            else
            {
                Debug.Assert(name == "Vector512`1");

                if (defType.Context.Target.Architecture == TargetArchitecture.ARM)
                {
                    // No such type exists for the Procedure Call Standard for ARM. We will default
                    // to the same alignment as __m128, which is supported by the ABI.
                    alignment = new LayoutInt(8);
                }
                else if (defType.Context.Target.Architecture == TargetArchitecture.ARM64 || defType.Context.Target.Architecture == TargetArchitecture.RiscV64)
                {
                    // The Procedure Call Standard for ARM 64-bit (with SVE support) defaults to
                    // 16-byte alignment for __m256.
                    alignment = new LayoutInt(16);
                }
                else
                {
                    alignment = new LayoutInt(64);
                }
            }

            ComputedInstanceFieldLayout layoutFromMetadata = _fallbackAlgorithm.ComputeInstanceLayout(defType, layoutKind);

            return new ComputedInstanceFieldLayout
            {
                ByteCountUnaligned = layoutFromMetadata.ByteCountUnaligned,
                ByteCountAlignment = layoutFromMetadata.ByteCountAlignment,
                FieldAlignment = alignment,
                FieldSize = layoutFromMetadata.FieldSize,
                Offsets = layoutFromMetadata.Offsets,
                LayoutAbiStable = _vectorAbiIsStable
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
            if (type.Context.Target.Architecture == TargetArchitecture.ARM64 &&
                type.Instantiation[0].IsPrimitiveNumeric)
            {
                return type.InstanceFieldSize.AsInt switch
                {
                    8 => ValueTypeShapeCharacteristics.Vector64Aggregate,
                    16 => ValueTypeShapeCharacteristics.Vector128Aggregate,
                    32 => ValueTypeShapeCharacteristics.Vector128Aggregate,
                    64 => ValueTypeShapeCharacteristics.Vector128Aggregate,
                    _ => ValueTypeShapeCharacteristics.None
                };
            }
            return ValueTypeShapeCharacteristics.None;
        }

        public static bool IsVectorType(DefType type)
        {
            return type.IsIntrinsic &&
                type.Namespace == "System.Runtime.Intrinsics" &&
                ((type.Name == "Vector64`1") ||
                 (type.Name == "Vector128`1") ||
                 (type.Name == "Vector256`1") ||
                 (type.Name == "Vector512`1"));
        }
    }
}
