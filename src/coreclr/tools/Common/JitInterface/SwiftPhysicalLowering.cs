// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Internal.TypeSystem;

namespace Internal.JitInterface
{
    public static class SwiftPhysicalLowering
    {
        private enum LoweredType
        {
            Empty,
            Opaque,
            Int64,
            Float,
            Double,
        }

        private sealed class LoweringVisitor(int pointerSize) : FieldLayoutIntervalCalculator<LoweredType>(pointerSize)
        {
            protected override LoweredType EmptyIntervalData => LoweredType.Empty;

            protected override bool IntervalsHaveCompatibleTags(LoweredType existingTag, LoweredType nextTag)
            {
                // Adjacent ranges mapped to opaque or empty can be combined.
                return existingTag is LoweredType.Opaque or LoweredType.Empty && nextTag is LoweredType.Opaque or LoweredType.Empty;
            }

            protected override FieldLayoutInterval CombineIntervals(FieldLayoutInterval firstInterval, FieldLayoutInterval nextInterval)
            {
                FieldLayoutInterval resultInterval = firstInterval;
                resultInterval.EndSentinel = nextInterval.EndSentinel;
                if (resultInterval.Tag != nextInterval.Tag)
                {
                    resultInterval.Tag = LoweredType.Opaque;
                }
                return resultInterval;
            }

            protected override LoweredType GetIntervalDataForType(int offset, TypeDesc fieldType)
            {
                // Comments here are from the Swift Calling Convention document:
                // In all of these examples, the maximum voluntary integer size is 4
                // (`i32`) unless otherwise specified.

                // If any range is mapped as a non-empty, non-opaque type, but its start
                // offset is not a multiple of its natural alignment, remap it as opaque.
                // For these purposes, the natural alignment of an integer type is the
                // minimum of its size and the maximum voluntary integer size; the
                // natural alignment of any other type is its C ABI type.
                //
                // TODO: What about 8-byte integers aligned at 4-byte boundaries?
                // Can this even be done in Swift?
                if (fieldType is MetadataType mdType && offset % mdType.InstanceFieldAlignment.AsInt != 0)
                {
                    return LoweredType.Opaque;
                }

                if (fieldType.Category is TypeFlags.Single)
                {
                    return LoweredType.Float;
                }

                if (fieldType.Category is TypeFlags.Double)
                {
                    return LoweredType.Double;
                }

                if (fieldType.Category is TypeFlags.UInt64 or TypeFlags.Int64)
                {
                    return LoweredType.Int64;
                }

                Debug.Assert(PointerSize == 8, "Swift interop is only supported on 64-bit platforms.");

                if (fieldType.Category is TypeFlags.IntPtr or TypeFlags.UIntPtr or TypeFlags.Pointer or TypeFlags.FunctionPointer)
                {
                    return LoweredType.Int64;
                }

                Debug.Assert(fieldType.IsPrimitiveNumeric);

                // If any range is mapped as an integer type that is not larger than the
                // maximum voluntary size, remap it as opaque. Combine adjacent opaque
                // ranges.
                return LoweredType.Opaque;
            }

            protected override bool NeedsRecursiveLayout(int offset, TypeDesc fieldType) => fieldType.IsValueType && !fieldType.IsPrimitiveNumeric;

            public List<CorInfoType> GetLoweredTypeSequence()
            {
                // We need to track the sequence size to ensure we break up the opaque ranges
                // into correctly-sized integers that do not require padding.
                int loweredSequenceSize = 0;
                List<CorInfoType> loweredTypes = new();
                foreach (var interval in Intervals)
                {
                    // Empty intervals at this point don't need to be represented in the lowered type sequence.
                    // We want to skip over them.
                    if (interval.Tag == LoweredType.Empty)
                        continue;

                    if (interval.Tag == LoweredType.Float)
                    {
                        loweredTypes.Add(CorInfoType.CORINFO_TYPE_FLOAT);
                        loweredSequenceSize = loweredSequenceSize.AlignUp(4);
                        loweredSequenceSize += 4;
                    }

                    if (interval.Tag == LoweredType.Double)
                    {
                        loweredTypes.Add(CorInfoType.CORINFO_TYPE_DOUBLE);
                        loweredSequenceSize = loweredSequenceSize.AlignUp(8);
                        loweredSequenceSize += 8;
                    }

                    if (interval.Tag == LoweredType.Int64)
                    {
                        loweredTypes.Add(CorInfoType.CORINFO_TYPE_LONG);
                        loweredSequenceSize = loweredSequenceSize.AlignUp(8);
                        loweredSequenceSize += 8;
                    }

                    if (interval.Tag == LoweredType.Opaque)
                    {
                        // We need to split the opaque ranges into integer parameters.
                        // As part of this splitting, we must ensure that we don't introduce alignment padding.
                        // This lowering algorithm should produce a lowered type sequence that would have the same padding for
                        // a naturally-aligned struct with the lowered fields as the original type has.
                        // This algorithm intends to split the opaque range into the least number of lowered elements that covers the entire range.
                        // The lowered range is allowed to extend past the end of the opaque range (including past the end of the struct),
                        // but not into the next non-empty interval.
                        // However, due to the properties of the lowering (the only non-8 byte elements of the lowering are 4-byte floats),
                        // we'll never encounter a scneario where we need would need to account for a correctly-aligned
                        // opaque range of > 4 bytes that we must not pad to 8 bytes.


                        // As long as we need to fill more than 4 bytes and the sequence is currently 8-byte aligned, we'll split into 8-byte integers.
                        // If we have more than 2 bytes but less than 4 and the sequence is 4-byte aligned, we'll use a 4-byte integer to represent the rest of the parameters.
                        // If we have 2 bytes and the sequence is 2-byte aligned, we'll use a 2-byte integer to represent the rest of the parameters.
                        // If we have 1 byte, we'll use a 1-byte integer to represent the rest of the parameters.
                        int remainingIntervalSize = interval.Size;
                        while (remainingIntervalSize > 4 && loweredSequenceSize == loweredSequenceSize.AlignUp(8))
                        {
                            loweredTypes.Add(CorInfoType.CORINFO_TYPE_LONG);
                            loweredSequenceSize += 8;
                            remainingIntervalSize -= 8;
                        }

                        if (remainingIntervalSize > 2 && loweredSequenceSize == loweredSequenceSize.AlignUp(4))
                        {
                            loweredTypes.Add(CorInfoType.CORINFO_TYPE_INT);
                            loweredSequenceSize += 4;
                            remainingIntervalSize -= 4;
                        }

                        if (remainingIntervalSize > 1 && loweredSequenceSize == loweredSequenceSize.AlignUp(2))
                        {
                            loweredTypes.Add(CorInfoType.CORINFO_TYPE_SHORT);
                            loweredSequenceSize += 2;
                            remainingIntervalSize -= 2;
                        }

                        if (remainingIntervalSize == 1)
                        {
                            loweredSequenceSize += 1;
                            loweredTypes.Add(CorInfoType.CORINFO_TYPE_BYTE);
                        }
                    }
                }
                return loweredTypes;
            }
        }

        public static CORINFO_SWIFT_LOWERING LowerTypeForSwiftSignature(TypeDesc type)
        {
            if (!type.IsValueType || type is DefType { ContainsGCPointers: true })
            {
                Debug.Fail("Non-unmanaged types should not be passed directly to a Swift function.");
                return new() { byReference = true };
            }

            LoweringVisitor visitor = new(type.Context.Target.PointerSize);
            visitor.AddFields(type, addTrailingEmptyInterval: false);

            List<CorInfoType> loweredTypes = visitor.GetLoweredTypeSequence();

            // If a type has a primitive sequence with more than 4 elements, Swift passes it by reference.
            if (loweredTypes.Count > 4)
            {
                return new() { byReference = true };
            }

            CORINFO_SWIFT_LOWERING lowering = new()
            {
                byReference = false,
                numLoweredElements = loweredTypes.Count
            };

            CollectionsMarshal.AsSpan(loweredTypes).CopyTo(lowering.LoweredElements);

            return lowering;
        }
    }
}
