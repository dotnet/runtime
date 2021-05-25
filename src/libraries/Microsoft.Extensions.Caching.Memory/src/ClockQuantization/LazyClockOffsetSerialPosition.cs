// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Diagnostics;
#if NETSTANDARD2_1 || NETCOREAPP3_0 || NETCOREAPP3_1 || NET5_0 || NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Runtime.CompilerServices;


namespace Microsoft.Extensions.Internal.ClockQuantization
{
    /// <summary>
    /// <para>
    /// Represents a point in time, expressed as a combination of a clock-specific <see cref="ClockOffset"/> and <see cref="SerialPosition"/>. Its value may be unitialized,
    /// as indicated by its <see cref="HasValue"/> property.
    /// </para>
    /// <para>
    /// When initialized (i.e. when <see cref="HasValue"/> equals <see langword="true"/>), the following rules apply:
    /// <list type="bullet">
    /// <item>Issuance of an "exact" <see cref="LazyClockOffsetSerialPosition"/> can occur at <see cref="Interval"/> start. By definition, <see cref="IsExact"/> will equal
    /// <see langword="true"/>, <see cref="SerialPosition"/> will equal <c>1u</c> and <see cref="ClockOffset"/> will equal <see cref="Interval.ClockOffset"/>.</item>
    /// <item>Any <see cref="LazyClockOffsetSerialPosition"/> issued off the same <see cref="Interval"/> with <see cref="SerialPosition"/> N (N &gt; 1u) was issued
    /// at a later point in (continuous) time than the <see cref="LazyClockOffsetSerialPosition"/> with <see cref="SerialPosition"/> equals N-1 and was issued at an earlier
    /// point in (continuous) time than any <see cref="LazyClockOffsetSerialPosition"/> with <see cref="SerialPosition"/> &gt; N.</item>
    /// <item>A helper method <see cref="AssignExactClockOffsetSerialPosition"/> is available to intialize <see cref="LazyClockOffsetSerialPosition"/> without an associated <see cref="Interval"/>.</item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// With several methods available to lazily initialize a <see cref="LazyClockOffsetSerialPosition"/> by reference, it is possible to create <see cref="LazyClockOffsetSerialPosition"/>s
    /// on-stack and initialize them as late as possible and only if deemed necessary for the operation/decision at hand.
    /// <seealso cref="Interval.EnsureInitializedClockOffsetSerialPosition(Interval, ref LazyClockOffsetSerialPosition)"/>
    /// <seealso cref="ClockQuantizer.EnsureInitializedExactClockOffsetSerialPosition(ref LazyClockOffsetSerialPosition, bool)"/>
    /// <seealso cref="ClockQuantizer.EnsureInitializedClockOffsetSerialPosition(ref LazyClockOffsetSerialPosition)"/>
    /// </remarks>
    internal struct LazyClockOffsetSerialPosition : IComparable, IComparable<LazyClockOffsetSerialPosition>
    {
        private static class ThrowHelper
        {
#if NETSTANDARD2_1 || NETCOREAPP3_0 || NETCOREAPP3_1 || NET5_0 || NET5_0_OR_GREATER
            [DoesNotReturn]
#endif
            [MethodImpl(MethodImplOptions.NoInlining)]
            internal static T ThrowInvalidOperationException<T>() => throw new InvalidOperationException();
        }

        private readonly struct Snapshot
        {
            public readonly long ClockOffset;
            public readonly uint SerialPosition;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Snapshot(in Interval.SnapshotTracker tracker)
            {
                SerialPosition = tracker.SerialPosition;
                ClockOffset = tracker.ClockOffset;
            }

            internal Snapshot(long offset)
            {
                SerialPosition = 1u;
                ClockOffset = offset;
            }
        }

        private Snapshot _snapshot;

        /// <value>Returns the offset assigned to the current value.</value>
        /// <exception cref="InvalidOperationException">When <see cref="HasValue"/> is <see langword="false"/>.</exception>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly long ClockOffset { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => HasValue ? _snapshot.ClockOffset : ThrowHelper.ThrowInvalidOperationException<long>(); }

        /// <value>Returns the serial position assigned to the current value.</value>
        /// <exception cref="InvalidOperationException">When <see cref="HasValue"/> is <see langword="false"/>.</exception>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly uint SerialPosition { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => HasValue ? _snapshot.SerialPosition : ThrowHelper.ThrowInvalidOperationException<uint>(); }

        /// <value>Returns <see langword="true"/> if a value is assigned, <see langword="false"/> otherwise.</value>
        public readonly bool HasValue { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _snapshot.SerialPosition > 0u; }

        /// <value>Returns <see langword="true"/> if a value is assigned and said value represents the first <see cref="SerialPosition"/> issued at <see cref="ClockOffset"/>. In other words,
        /// the value was assigned exactly at <see cref="ClockOffset"/>.</value>
        public readonly bool IsExact { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _snapshot.SerialPosition == 1u; }

        internal LazyClockOffsetSerialPosition(in Interval.SnapshotTracker tracker) => _snapshot = new Snapshot(in tracker);

        /// <summary>
        /// Asigns an "exact" value to a <see cref="LazyClockOffsetSerialPosition"/>.
        /// </summary>
        /// <param name="offset">The offset assigned to <paramref name="position"/></param>
        /// <param name="position">The position to be (re-)initialized</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AssignExactClockOffsetSerialPosition(long offset, ref LazyClockOffsetSerialPosition position) => position._snapshot = new Snapshot(offset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ApplySnapshot(ref LazyClockOffsetSerialPosition position, in Interval.SnapshotTracker tracker) => position._snapshot = new Snapshot(in tracker);

        /// <summary>
        /// Compare two <see cref="LazyClockOffsetSerialPosition"/> instances and returns an integer that indicates whether the first instance precedes, follows, or occurs in the same position in the sort order as the second instance.
        /// </summary>
        /// <param name="first">The first instance</param>
        /// <param name="second">The second instance</param>
        /// <returns></returns>
        public static int Compare(in LazyClockOffsetSerialPosition first, in LazyClockOffsetSerialPosition second) => first.CompareTo(second);

        int IComparable.CompareTo(object? obj)
        {
            if (obj == null) return 1;
            if (obj is LazyClockOffsetSerialPosition value)
            {
                return Compare(this, value);
            }

            throw new ArgumentException($"Must be of type {nameof(LazyClockOffsetSerialPosition)}", nameof(obj));
        }

        /// <inheritdoc/>
        public int CompareTo(LazyClockOffsetSerialPosition value)
        {
            int result = ClockOffset.CompareTo(value.ClockOffset);
            if (result == 0)
            {
                return SerialPosition.CompareTo(value.SerialPosition);
            }

            return result;
        }
    }
}

#nullable restore
