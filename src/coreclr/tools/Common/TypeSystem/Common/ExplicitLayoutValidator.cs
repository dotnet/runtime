// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Internal.TypeSystem
{
    public struct ExplicitLayoutValidator
    {
        private enum FieldLayoutTag : byte
        {
            Empty,
            NonORef,
            ORef,
        }

        private struct FieldLayoutInterval : IComparable<FieldLayoutInterval>
        {
            public FieldLayoutInterval(int start, int size, FieldLayoutTag tag)
            {
                Start = start;
                Size = size;
                Tag = tag;
            }

            public int Start;
            public int Size;

            public int EndSentinel
            {
                get
                {
                    return Start + Size;
                }
                set
                {
                    Size = value - Start;
                    Debug.Assert(Size >= 0);
                }
            }

            public FieldLayoutTag Tag;

            public int CompareTo(FieldLayoutInterval other)
            {
                return Start.CompareTo(other.Start);
            }
        }

        private readonly int _pointerSize;

        // Represent field layout bits as as a series of intervals to prevent pathological bad behavior
        // involving excessively large explicit layout structures.
        private readonly List<FieldLayoutInterval> _fieldLayout = new List<FieldLayoutInterval>();

        private readonly MetadataType _typeBeingValidated;

        private ExplicitLayoutValidator(MetadataType type, int typeSizeInBytes)
        {
            _typeBeingValidated = type;
            _pointerSize = type.Context.Target.PointerSize;
        }

        public static void Validate(MetadataType type, ComputedInstanceFieldLayout layout)
        {
            int typeSizeInBytes = layout.ByteCountUnaligned.AsInt;
            ExplicitLayoutValidator validator = new ExplicitLayoutValidator(type, typeSizeInBytes);

            foreach (FieldAndOffset fieldAndOffset in layout.Offsets)
            {
                validator.AddToFieldLayout(fieldAndOffset.Offset.AsInt, fieldAndOffset.Field.FieldType);
            }
        }

        private void AddToFieldLayout(int offset, TypeDesc fieldType)
        {
            if (fieldType.IsGCPointer)
            {
                if (offset % _pointerSize != 0)
                {
                    // Misaligned ORef
                    ThrowFieldLayoutError(offset);
                }
                SetFieldLayout(offset, _pointerSize, FieldLayoutTag.ORef);
            }
            else if (fieldType.IsPointer || fieldType.IsFunctionPointer)
            {
                SetFieldLayout(offset, _pointerSize, FieldLayoutTag.NonORef);
            }
            else if (fieldType.IsValueType)
            {
                if (fieldType.IsByRefLike && offset % _pointerSize != 0)
                {
                    // Misaligned ByRefLike
                    ThrowFieldLayoutError(offset);
                }

                MetadataType mdType = (MetadataType)fieldType;
                int fieldSize = mdType.InstanceByteCountUnaligned.AsInt;
                if (!mdType.ContainsGCPointers)
                {
                    // Plain value type, mark the entire range as NonORef
                    SetFieldLayout(offset, fieldSize, FieldLayoutTag.NonORef);
                }
                else
                {
                    if (offset % _pointerSize != 0)
                    {
                        // Misaligned struct with GC pointers
                        ThrowFieldLayoutError(offset);
                    }

                    List<FieldLayoutInterval> fieldORefMap = new List<FieldLayoutInterval>();
                    MarkORefLocations(mdType, fieldORefMap, offset: 0);

                    // Merge in fieldORefMap from structure specifying not attributed intervals as NonORef
                    int lastGCRegionReportedEnd = 0;

                    foreach (var gcRegion in fieldORefMap)
                    {
                        SetFieldLayout(offset + lastGCRegionReportedEnd, gcRegion.Start - lastGCRegionReportedEnd, FieldLayoutTag.NonORef);
                        Debug.Assert(gcRegion.Tag == FieldLayoutTag.ORef);
                        SetFieldLayout(offset + gcRegion.Start, gcRegion.Size, gcRegion.Tag);
                        lastGCRegionReportedEnd = gcRegion.EndSentinel;
                    }

                    if (fieldORefMap.Count > 0)
                    {
                        int trailingRegionStart = fieldORefMap[fieldORefMap.Count - 1].EndSentinel;
                        int trailingRegionSize = fieldSize - trailingRegionStart;
                        SetFieldLayout(offset + trailingRegionStart, trailingRegionSize, FieldLayoutTag.NonORef);
                    }
                }
            }
            else if (fieldType.IsByRef)
            {
                if (offset % _pointerSize != 0)
                {
                    // Misaligned pointer field
                    ThrowFieldLayoutError(offset);
                }
                SetFieldLayout(offset, _pointerSize, FieldLayoutTag.NonORef);
            }
            else
            {
                Debug.Assert(false, fieldType.ToString());
            }
        }

        private void MarkORefLocations(MetadataType type, List<FieldLayoutInterval> orefMap, int offset)
        {
            // Recurse into struct fields
            foreach (FieldDesc field in type.GetFields())
            {
                if (!field.IsStatic)
                {
                    int fieldOffset = offset + field.Offset.AsInt;
                    if (field.FieldType.IsGCPointer)
                    {
                        SetFieldLayout(orefMap, offset, _pointerSize, FieldLayoutTag.ORef);
                    }
                    else if (field.FieldType.IsValueType)
                    {
                        MetadataType mdFieldType = (MetadataType)field.FieldType;
                        if (mdFieldType.ContainsGCPointers)
                        {
                            MarkORefLocations(mdFieldType, orefMap, fieldOffset);
                        }
                    }
                }
            }
        }

        private void SetFieldLayout(List<FieldLayoutInterval> fieldLayoutInterval, int offset, int count, FieldLayoutTag tag)
        {
            if (count == 0)
                return;

            var newInterval = new FieldLayoutInterval(offset, count, tag);

            int binarySearchIndex = fieldLayoutInterval.BinarySearch(newInterval);

            if (binarySearchIndex >= 0)
            {
                var existingInterval = fieldLayoutInterval[binarySearchIndex];

                // Exact match found for start of interval.
                if (tag != existingInterval.Tag)
                {
                    ThrowFieldLayoutError(offset);
                }

                if (existingInterval.Size >= count)
                {
                    // Existing interval is big enough.
                }
                else
                {
                    // Expand existing interval, and then check to see if that's valid.
                    existingInterval.Size = count;
                    fieldLayoutInterval[binarySearchIndex] = existingInterval;

                    ValidateAndMergeIntervalWithFollowingIntervals(fieldLayoutInterval, binarySearchIndex);
                }
            }
            else
            {
                // No exact start match found.

                int newIntervalLocation = ~binarySearchIndex;

                // Check for previous interval overlaps cases
                if (newIntervalLocation > 0)
                {
                    var previousInterval = fieldLayoutInterval[newIntervalLocation - 1];
                    bool tagMatches = previousInterval.Tag == tag;

                    if (previousInterval.EndSentinel > offset)
                    {
                        // Previous interval overlaps.
                        if (!tagMatches)
                        {
                            ThrowFieldLayoutError(offset);
                        }
                    }

                    if (previousInterval.EndSentinel > offset || (tagMatches && previousInterval.EndSentinel == offset))
                    {
                        // Previous interval overlaps, or exactly matches up with new interval and tag matches. Instead
                        // of expanding interval set, simply expand the previous interval.
                        previousInterval.EndSentinel = newInterval.EndSentinel;

                        fieldLayoutInterval[newIntervalLocation - 1] = previousInterval;
                        newIntervalLocation = newIntervalLocation - 1;
                    }
                    else
                    {
                        fieldLayoutInterval.Insert(newIntervalLocation, newInterval);
                    }
                }
                else
                {
                    // New interval added at start
                    fieldLayoutInterval.Insert(newIntervalLocation, newInterval);
                }

                ValidateAndMergeIntervalWithFollowingIntervals(fieldLayoutInterval, newIntervalLocation);
            }
        }

        private void ValidateAndMergeIntervalWithFollowingIntervals(List<FieldLayoutInterval> fieldLayoutInterval, int intervalIndex)
        {
            while(true)
            {
                if (intervalIndex + 1 == fieldLayoutInterval.Count)
                {
                    // existing interval is last interval. Expansion always succeeds
                    break;
                }
                else
                {
                    var nextInterval = fieldLayoutInterval[intervalIndex + 1];
                    var expandedInterval = fieldLayoutInterval[intervalIndex];
                    var tag = expandedInterval.Tag;

                    if (nextInterval.Start > expandedInterval.EndSentinel)
                    {
                        // Next interval does not contact existing interval. Expansion succeeded
                        break;
                    }

                    if ((nextInterval.Start == expandedInterval.EndSentinel) && nextInterval.Tag != tag)
                    {
                        // Next interval starts just after existing interval, but does not match tag. Expansion succeeded
                        break;
                    }

                    Debug.Assert(nextInterval.Start <= expandedInterval.EndSentinel);
                    // Next interval overlaps with expanded interval.

                    if (nextInterval.Tag != tag)
                    {
                        ThrowFieldLayoutError(nextInterval.Start);
                    }

                    // Expand existing interval to cover region of next interval
                    expandedInterval.EndSentinel = nextInterval.EndSentinel;
                    fieldLayoutInterval[intervalIndex] = expandedInterval;

                    // Remove next interval
                    fieldLayoutInterval.RemoveAt(intervalIndex + 1);
                }
            }
        }

        private void SetFieldLayout(int offset, int count, FieldLayoutTag tag)
        {
            SetFieldLayout(_fieldLayout, offset, count, tag);
        }

        private void ThrowFieldLayoutError(int offset)
        {
            ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadExplicitLayout, _typeBeingValidated, offset.ToStringInvariant());
        }
    }
}
