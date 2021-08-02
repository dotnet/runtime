// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem
{
    /// <summary>
    /// RuntimeDeterminedFieldLayoutAlgorithm algorithm which can be used to compute field layout
    /// for any RuntimeDeterminedType
    /// Only useable for accessing the instance field size
    /// </summary>
    public class RuntimeDeterminedFieldLayoutAlgorithm : FieldLayoutAlgorithm
    {
        public override ComputedInstanceFieldLayout ComputeInstanceLayout(DefType defType, InstanceLayoutKind layoutKind)
        {
            // Individual field offset layout for a RuntimeDeterminedType is not a supported operation
            if (layoutKind != InstanceLayoutKind.TypeOnly)
                throw new NotSupportedException();

            RuntimeDeterminedType type = (RuntimeDeterminedType)defType;
            DefType canonicalType = type.CanonicalType;

            ComputedInstanceFieldLayout result = new ComputedInstanceFieldLayout
            {
                ByteCountUnaligned = canonicalType.InstanceByteCountUnaligned,
                ByteCountAlignment = canonicalType.InstanceByteAlignment,
                FieldAlignment = canonicalType.InstanceFieldAlignment,
                FieldSize = canonicalType.InstanceFieldSize,
                Offsets = Array.Empty<FieldAndOffset>(),
                LayoutAbiStable = canonicalType.LayoutAbiStable
            };

            return result;
        }

        public unsafe override ComputedStaticFieldLayout ComputeStaticFieldLayout(DefType defType, StaticLayoutKind layoutKind)
        {
            // Static field layout for a RuntimeDeterminedType is not a supported operation
            throw new NotSupportedException();
        }

        public override bool ComputeContainsGCPointers(DefType type)
        {
            RuntimeDeterminedType runtimeDeterminedType = (RuntimeDeterminedType)type;
            DefType canonicalType = runtimeDeterminedType.CanonicalType;

            return canonicalType.ContainsGCPointers;
        }

        public override ValueTypeShapeCharacteristics ComputeValueTypeShapeCharacteristics(DefType type)
        {
            RuntimeDeterminedType runtimeDeterminedType = (RuntimeDeterminedType)type;
            DefType canonicalType = runtimeDeterminedType.CanonicalType;

            return canonicalType.ValueTypeShapeCharacteristics;
        }

        public override bool ComputeIsUnsafeValueType(DefType type)
        {
            RuntimeDeterminedType runtimeDeterminedType = (RuntimeDeterminedType)type;
            DefType canonicalType = runtimeDeterminedType.CanonicalType;

            return canonicalType.IsUnsafeValueType;
        }
    }
}
