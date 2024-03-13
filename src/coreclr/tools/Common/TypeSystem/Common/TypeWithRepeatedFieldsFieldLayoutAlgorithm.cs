// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace Internal.TypeSystem
{
    /// <summary>
    /// Represents an algorithm that computes field layout for intrinsic vector types (Vector64/Vector128/Vector256).
    /// </summary>
    public class TypeWithRepeatedFieldsFieldLayoutAlgorithm : FieldLayoutAlgorithm
    {
        private readonly FieldLayoutAlgorithm _fallbackAlgorithm;

        public TypeWithRepeatedFieldsFieldLayoutAlgorithm(FieldLayoutAlgorithm fallbackAlgorithm)
        {
            _fallbackAlgorithm = fallbackAlgorithm;
        }

        public override ComputedInstanceFieldLayout ComputeInstanceLayout(DefType defType, InstanceLayoutKind layoutKind)
        {
            var type = (TypeWithRepeatedFields)defType;

            ComputedInstanceFieldLayout layoutFromMetadata = _fallbackAlgorithm.ComputeInstanceLayout(type.MetadataType, layoutKind);

            FieldAndOffset[] offsets = layoutFromMetadata.Offsets;

            if (offsets is not null)
            {
                var fieldOffsets = new FieldAndOffset[type.NumFields];

                LayoutInt cumulativeOffset = new LayoutInt(0);
                int fieldIndex = 0;
                foreach (FieldDesc field in type.GetFields())
                {
                    if (field.IsStatic)
                    {
                        continue;
                    }
                    fieldOffsets[fieldIndex++] = new FieldAndOffset(field, cumulativeOffset);
                    cumulativeOffset += field.FieldType.GetElementSize();
                }

                offsets = fieldOffsets;
            }

            return new ComputedInstanceFieldLayout
            {
                ByteCountUnaligned = layoutFromMetadata.ByteCountUnaligned,
                ByteCountAlignment = layoutFromMetadata.ByteCountAlignment,
                FieldAlignment = layoutFromMetadata.FieldAlignment,
                FieldSize = layoutFromMetadata.FieldSize,
                Offsets = offsets,
                LayoutAbiStable = layoutFromMetadata.LayoutAbiStable
            };
        }

        public override ComputedStaticFieldLayout ComputeStaticFieldLayout(DefType defType, StaticLayoutKind layoutKind)
        {
            return _fallbackAlgorithm.ComputeStaticFieldLayout(((TypeWithRepeatedFields)defType).MetadataType, layoutKind);
        }

        public override bool ComputeContainsGCPointers(DefType type)
        {
            return _fallbackAlgorithm.ComputeContainsGCPointers(((TypeWithRepeatedFields)type).MetadataType);
        }

        public override bool ComputeIsUnsafeValueType(DefType type)
        {
            return _fallbackAlgorithm.ComputeIsUnsafeValueType(((TypeWithRepeatedFields)type).MetadataType);
        }

        public override ValueTypeShapeCharacteristics ComputeValueTypeShapeCharacteristics(DefType type)
        {
            return _fallbackAlgorithm.ComputeValueTypeShapeCharacteristics(((TypeWithRepeatedFields)type).MetadataType);
        }
    }
}
