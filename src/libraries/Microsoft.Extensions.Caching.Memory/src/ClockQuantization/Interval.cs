// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Threading;
using System.Runtime.CompilerServices;


namespace Microsoft.Extensions.Internal.ClockQuantization
{
    /// <summary>
    /// Represents an interval within a <see cref="ClockQuantizer"/>'s temporal context.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Within the reference frame of an <see cref="Interval"/>, there is no notion of time; there is only notion of the order
    /// in which <see cref="LazyClockOffsetSerialPosition"/>s are issued.
    /// </para>
    /// <para>
    /// Whereas <see cref="ClockQuantizer.CurrentInterval"/> is always progressing with intervals of at most <see cref="ClockQuantizer.MaxIntervalTimeSpan"/> length,
    /// several <see cref="Interval"/>s may be active concurrently.
    /// </para>
    /// </remarks>
    internal class Interval
    {
        internal struct SnapshotTracker
        {
            internal uint SerialPosition;
            internal readonly long ClockOffset;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static ref readonly SnapshotTracker WithNextSerialPosition(ref SnapshotTracker tracker)
            {
#if NET5_0 || NET5_0_OR_GREATER
                Interlocked.Increment(ref tracker.SerialPosition);
#else
                Interlocked.Add(ref Unsafe.As<uint, int>(ref tracker.SerialPosition), 1);
#endif
                return ref tracker;
            }

            internal SnapshotTracker(in long offset) { SerialPosition = 0u; ClockOffset = offset; }
        }

        private SnapshotTracker _tracker;

        /// <value>
        /// The offset within the temporal context when the <see cref="Interval"/> was started.
        /// </value>
        public long ClockOffset { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _tracker.ClockOffset; }

        internal Interval(in long offset) => _tracker = new SnapshotTracker(in offset);


        /// <summary>
        /// If <paramref name="position"/> does not have an <see cref="LazyClockOffsetSerialPosition.ClockOffset"/> yet, it will be initialized with one,
        /// based off <paramref name="interval"/>'s <see cref="Interval.ClockOffset"/> and its monotonically increasing internal serial position.
        /// </summary>
        /// <param name="interval">The interval to initialize the <see cref="LazyClockOffsetSerialPosition"/> off.</param>
        /// <param name="position">Reference to an (on-stack) <see cref="LazyClockOffsetSerialPosition"/> which may or may not have been initialized.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureInitializedClockOffsetSerialPosition(Interval interval, ref LazyClockOffsetSerialPosition position)
        {
            if (position.HasValue && interval._tracker.SerialPosition > 0u)
            {
                return;
            }

            LazyClockOffsetSerialPosition.ApplySnapshot(ref position, in SnapshotTracker.WithNextSerialPosition(ref interval._tracker));
        }

        /// <summary>
        /// Creates a new <see cref="LazyClockOffsetSerialPosition"/> based off the <see cref="Interval"/>'s <see cref="Interval.ClockOffset"/> and its monotonically increasing internal serial position.
        /// </summary>
        /// <returns>A new <see cref="LazyClockOffsetSerialPosition"/></returns>
        /// <remarks>
        /// A <see cref="LazyClockOffsetSerialPosition"/> created at the time when a new <see cref="Interval"/> is created (e.g. during
        /// <seealso cref="ClockQuantizer.EnsureInitializedExactClockOffsetSerialPosition(ref LazyClockOffsetSerialPosition, bool)"/>) will have <see cref="LazyClockOffsetSerialPosition.IsExact"/> equal
        /// to <see langword="true"/>.
        /// </remarks>
        public LazyClockOffsetSerialPosition NewClockOffsetSerialPosition() => new LazyClockOffsetSerialPosition(in SnapshotTracker.WithNextSerialPosition(ref _tracker));

        internal Interval Seal()
        {
            // Prevent 'Exact' positions post initialization of the Interval; ensure SerialPosition > 0
#if NET5_0 || NET5_0_OR_GREATER
            Interlocked.CompareExchange(ref _tracker.SerialPosition, 1u, 0u);
#else
            Interlocked.CompareExchange(ref Unsafe.As<uint, int>(ref _tracker.SerialPosition), 1, 0);
#endif

            return this;
        }
    }
}

#nullable restore
