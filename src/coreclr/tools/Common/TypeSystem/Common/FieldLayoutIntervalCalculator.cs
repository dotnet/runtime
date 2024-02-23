// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Internal.TypeSystem
{
    public abstract class FieldLayoutIntervalCalculator<TIntervalTag>
    {
        protected struct FieldLayoutInterval : IComparable<FieldLayoutInterval>
        {
            public FieldLayoutInterval(int start, int size, TIntervalTag tag)
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

            public TIntervalTag Tag;

            public int CompareTo(FieldLayoutInterval other)
            {
                return Start.CompareTo(other.Start);
            }
        }

        protected int PointerSize { get; }

        // Represent field layout bits as a series of intervals to prevent pathological bad behavior
        // involving excessively large explicit layout structures.
        private readonly List<FieldLayoutInterval> _fieldLayout = new();

        protected IReadOnlyList<FieldLayoutInterval> Intervals => _fieldLayout;

        public FieldLayoutIntervalCalculator(int pointerSize)
        {
            PointerSize = pointerSize;
        }

        protected abstract bool NeedsRecursiveLayout(int offset, TypeDesc fieldType);

        protected abstract TIntervalTag GetIntervalDataForType(int offset, TypeDesc fieldType);

        protected abstract TIntervalTag EmptyIntervalData { get; }

        private int GetFieldSize(TypeDesc fieldType)
        {
            if (fieldType.IsGCPointer || fieldType.IsPointer || fieldType.IsFunctionPointer || fieldType.IsByRef)
            {
                return PointerSize;
            }
            else if (fieldType.IsValueType)
            {
                return ((MetadataType)fieldType).InstanceByteCountUnaligned.AsInt;
            }
            else
            {
                Debug.Assert(false, fieldType.ToString());
                return fieldType.GetElementSize().AsInt;
            }
        }

        public void AddFields(TypeDesc type, bool addTrailingEmptyInterval = true) => AddToFieldLayout(0, type, addTrailingEmptyInterval: false);

        public void AddToFieldLayout(int offset, TypeDesc fieldType) => AddToFieldLayout(offset, fieldType, addTrailingEmptyInterval: true);

        public void AddToFieldLayout(int offset, TypeDesc fieldType, bool addTrailingEmptyInterval)
        {
            if (NeedsRecursiveLayout(offset, fieldType))
            {
                List<FieldLayoutInterval> nestedIntervals = new List<FieldLayoutInterval>();
                foreach (FieldDesc field in fieldType.GetFields())
                {
                    if (field.IsStatic)
                        continue;

                    int fieldOffset = offset + field.Offset.AsInt;
                    AddToFieldLayout(nestedIntervals, fieldOffset, field.FieldType);
                }

                // Merge in the intervals from structure, filling in the gaps with empty intervals.
                int lastGCRegionReportedEnd = 0;

                foreach (var interval in nestedIntervals)
                {
                    SetFieldLayout(offset + lastGCRegionReportedEnd, interval.Start - lastGCRegionReportedEnd, EmptyIntervalData);
                    SetFieldLayout(offset + interval.Start, interval.Size, interval.Tag);
                    lastGCRegionReportedEnd = interval.EndSentinel;
                }

                if (addTrailingEmptyInterval && nestedIntervals.Count > 0)
                {
                    int trailingRegionStart = nestedIntervals[^1].EndSentinel;
                    int trailingRegionSize = GetFieldSize(fieldType) - trailingRegionStart;
                    SetFieldLayout(offset + trailingRegionStart, trailingRegionSize, EmptyIntervalData);
                }
            }
            else
            {
                SetFieldLayout(offset, GetFieldSize(fieldType), GetIntervalDataForType(offset, fieldType));
            }
        }

        private void AddToFieldLayout(List<FieldLayoutInterval> fieldLayout, int offset, TypeDesc fieldType)
        {
            if (NeedsRecursiveLayout(offset, fieldType))
            {
                List<FieldLayoutInterval> nestedIntervals = new List<FieldLayoutInterval>();
                foreach (FieldDesc field in fieldType.GetFields())
                {
                    int fieldOffset = offset + field.Offset.AsInt;
                    AddToFieldLayout(nestedIntervals, fieldOffset, field.FieldType);
                }
            }
            else
            {
                SetIntervalData(fieldLayout, offset, GetFieldSize(fieldType), GetIntervalDataForType(offset, fieldType));
            }
        }


        protected abstract bool IntervalsHaveCompatibleTags(TIntervalTag existingTag, TIntervalTag nextTag);

        /// <summary>
        /// Combine two bordering or overlapping intervals into a single interval.
        /// </summary>
        /// <param name="firstInterval">The interval that starts earlier of the two intervals.</param>
        /// <param name="nextInterval">The interval that ends later of the two intervals.</param>
        /// <returns>A new interval that represents the combined range.</returns>
        protected abstract FieldLayoutInterval CombineIntervals(FieldLayoutInterval firstInterval, FieldLayoutInterval nextInterval);

        private void SetIntervalData(List<FieldLayoutInterval> fieldLayoutInterval, int offset, int count, TIntervalTag tag)
        {
            if (count == 0)
                return;

            var newInterval = new FieldLayoutInterval(offset, count, tag);

            int binarySearchIndex = fieldLayoutInterval.BinarySearch(newInterval);

            int updatedIntervalIndex;

            if (binarySearchIndex >= 0)
            {
                var existingInterval = fieldLayoutInterval[binarySearchIndex];

                fieldLayoutInterval[binarySearchIndex] = CombineIntervals(existingInterval, newInterval);
                updatedIntervalIndex = binarySearchIndex;
            }
            else
            {
                // No exact start match found.

                int newIntervalLocation = ~binarySearchIndex;

                // Check for previous interval overlaps cases
                if (newIntervalLocation > 0)
                {
                    var previousInterval = fieldLayoutInterval[newIntervalLocation - 1];

                    if (previousInterval.EndSentinel > offset)
                    {
                        fieldLayoutInterval[newIntervalLocation - 1] = CombineIntervals(previousInterval, newInterval);
                        newIntervalLocation--;
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

                updatedIntervalIndex = newIntervalLocation;
            }
            MergeIntervalWithNeighboringIntervals(fieldLayoutInterval, updatedIntervalIndex);
        }

        private void MergeIntervalWithNeighboringIntervals(List<FieldLayoutInterval> fieldLayoutInterval, int intervalIndex)
        {
            // Merge this interval first with the following intervals
            while (true)
            {
                if (intervalIndex + 1 == fieldLayoutInterval.Count)
                {
                    // existing interval is last interval. Expansion always succeeds
                    break;
                }

                var nextInterval = fieldLayoutInterval[intervalIndex + 1];
                var expandedInterval = fieldLayoutInterval[intervalIndex];

                if (nextInterval.Start > expandedInterval.EndSentinel)
                {
                    // Next interval does not contact existing interval. Expansion succeeded
                    break;
                }

                if ((nextInterval.Start == expandedInterval.EndSentinel) && !IntervalsHaveCompatibleTags(expandedInterval.Tag, nextInterval.Tag))
                {
                    // Next interval starts just after existing interval, but does not match tag. Expansion succeeded
                    break;
                }

                Debug.Assert(nextInterval.Start <= expandedInterval.EndSentinel);
                // Next interval overlaps with expanded interval.
                fieldLayoutInterval[intervalIndex] = CombineIntervals(expandedInterval, nextInterval);
                fieldLayoutInterval.RemoveAt(intervalIndex + 1);
            }

            // Now merge with preceeding intervals
            while (true)
            {
                if (intervalIndex == 0)
                {
                    // expanded interval is first interval. Expansion always succeeds
                    break;
                }

                var previousInterval = fieldLayoutInterval[intervalIndex - 1];
                var expandedInterval = fieldLayoutInterval[intervalIndex];

                if (previousInterval.EndSentinel < expandedInterval.Start)
                {
                    // Previous interval does not contact expanded interval. Expansion succeeded
                    break;
                }

                if ((previousInterval.EndSentinel == expandedInterval.Start) && !IntervalsHaveCompatibleTags(expandedInterval.Tag, previousInterval.Tag))
                {
                    // Expanded interval starts just after previous interval, but does not match tag. Expansion succeeded
                    break;
                }

                Debug.Assert(previousInterval.EndSentinel <= expandedInterval.Start);
                // Previous interval overlaps with expanded interval.
                fieldLayoutInterval[intervalIndex] = CombineIntervals(previousInterval, expandedInterval);
                fieldLayoutInterval.RemoveAt(--intervalIndex);
            }
        }

        private void SetFieldLayout(int offset, int count, TIntervalTag tag)
        {
            SetIntervalData(_fieldLayout, offset, count, tag);
        }
    }
}
