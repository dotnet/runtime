// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
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
                if (existingTag is LoweredType.Empty
                    && nextTag is LoweredType.Empty)
                {
                    return true;
                }

                if (existingTag is LoweredType.Opaque
                    && nextTag is LoweredType.Opaque)
                {
                    return true;
                }

                return false;
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

            private List<FieldLayoutInterval> CreateConsolidatedIntervals()
            {
                // First, let's make a list of exclusively non-empty intervals.
                List<FieldLayoutInterval> consolidatedIntervals = new(Intervals.Count);
                foreach (var interval in Intervals)
                {
                    if (interval.Tag != LoweredType.Empty)
                    {
                        consolidatedIntervals.Add(interval);
                    }
                }

                // Now, we'll look for adjacent opaque intervals and combine them.
                for (int i = 0; i < consolidatedIntervals.Count - 1; i++)
                {
                    // Only merge sequential opaque intervals that are within the same PointerSize-sized chunk.
                    if (consolidatedIntervals[i].Tag == LoweredType.Opaque
                        && consolidatedIntervals[i + 1].Tag == LoweredType.Opaque
                        && (consolidatedIntervals[i].EndSentinel - 1) / PointerSize == consolidatedIntervals[i + 1].Start / PointerSize)
                    {
                        consolidatedIntervals[i] = CombineIntervals(consolidatedIntervals[i], consolidatedIntervals[i + 1]);
                        consolidatedIntervals.RemoveAt(i + 1);
                        i--;
                    }
                }

                return consolidatedIntervals;
            }

            public List<(CorInfoType, int)> GetLoweredTypeSequence()
            {
                List<(CorInfoType, int)> loweredTypes = new();
                foreach (var interval in CreateConsolidatedIntervals())
                {
                    // Empty intervals at this point don't need to be represented in the lowered type sequence.
                    // We want to skip over them.
                    if (interval.Tag == LoweredType.Empty)
                        continue;

                    if (interval.Tag == LoweredType.Float)
                    {
                        loweredTypes.Add((CorInfoType.CORINFO_TYPE_FLOAT, interval.Start));
                    }

                    if (interval.Tag == LoweredType.Double)
                    {
                        loweredTypes.Add((CorInfoType.CORINFO_TYPE_DOUBLE, interval.Start));
                    }

                    if (interval.Tag == LoweredType.Int64)
                    {
                        loweredTypes.Add((CorInfoType.CORINFO_TYPE_LONG, interval.Start));
                    }

                    if (interval.Tag is LoweredType.Opaque)
                    {
                        // We need to split the opaque ranges into integer parameters.
                        // As part of this splitting, we must ensure that we don't introduce alignment padding.
                        // This lowering algorithm should produce a lowered type sequence that would have the same padding for
                        // a naturally-aligned struct with the lowered fields as the original type has.
                        // This algorithm intends to split the opaque range into the least number of lowered elements that covers the entire range.
                        // The lowered range is allowed to extend past the end of the opaque range (including past the end of the struct),
                        // but not into the next non-empty interval.
                        // However, due to the properties of the lowering (the only non-8 byte elements of the lowering are 4-byte floats),
                        // we'll never encounter a scenario where we need would need to account for a correctly-aligned
                        // opaque range of > 4 bytes that we must not pad to 8 bytes.


                        // While we need to fill more than 4 bytes and the sequence is currently 8-byte aligned, we'll split into 8-byte integers.
                        // While we need to fill more than 2 bytes but less than 4 and the sequence is 4-byte aligned, we'll use a 4-byte integer to represent the rest of the parameters.
                        // While we need to fill more than 1 bytes and the sequence is 2-byte aligned, we'll use a 2-byte integer to represent the rest of the parameters.
                        // While we need to fill at least 1 byte, we'll use a 1-byte integer to represent the rest of the parameters.
                        int opaqueIntervalStart = interval.Start;
                        int remainingIntervalSize = interval.Size;
                        while (remainingIntervalSize > 0)
                        {
                            if (remainingIntervalSize > 4 && opaqueIntervalStart == opaqueIntervalStart.AlignUp(8))
                            {
                                loweredTypes.Add((CorInfoType.CORINFO_TYPE_LONG, opaqueIntervalStart));
                                opaqueIntervalStart += 8;
                                remainingIntervalSize -= 8;
                            }
                            else if (remainingIntervalSize > 2 && opaqueIntervalStart == opaqueIntervalStart.AlignUp(4))
                            {
                                loweredTypes.Add((CorInfoType.CORINFO_TYPE_INT, opaqueIntervalStart));
                                opaqueIntervalStart += 4;
                                remainingIntervalSize -= 4;
                            }
                            else if (remainingIntervalSize > 1 && opaqueIntervalStart == opaqueIntervalStart.AlignUp(2))
                            {
                                loweredTypes.Add((CorInfoType.CORINFO_TYPE_SHORT, opaqueIntervalStart));
                                opaqueIntervalStart += 2;
                                remainingIntervalSize -= 2;
                            }
                            else
                            {
                                loweredTypes.Add((CorInfoType.CORINFO_TYPE_BYTE, opaqueIntervalStart));
                                opaqueIntervalStart++;
                                remainingIntervalSize--;
                            }
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

            List<(CorInfoType type, int offset)> loweredTypes = visitor.GetLoweredTypeSequence();

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

            for (int i = 0; i < loweredTypes.Count; i++)
            {
                lowering.LoweredElements[i] = loweredTypes[i].type;
                lowering.Offsets[i] = (uint)loweredTypes[i].offset;
            }

            return lowering;
        }
    }
}
