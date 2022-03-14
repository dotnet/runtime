// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    // This class uses high-resolution performance counter if the installed
    // hardware supports it. Otherwise, the class will fall back to DateTime
    // and uses ticks as a measurement.

    public partial class Stopwatch
    {
        private const long TicksPerMillisecond = 10000;
        private const long TicksPerSecond = TicksPerMillisecond * 1000;

        // _state is 0 when unstarted.
        //
        // _state stores a timestamp if it is positive
        // and an elapsed time if it is negative.
        //
        // The distinction between unstarted and
        // stopped with an elapsed time of 0 is intentionally
        // ignored.
        //
        // Resuming is supported by backdating the newer
        // timestamp by the previous elapsed time.
        private long _state;

        // "Frequency" stores the frequency of the high-resolution performance counter,
        // if one exists. Otherwise it will store TicksPerSecond.
        // The frequency cannot change while the system is running,
        // so we only need to initialize it once.
        public static readonly long Frequency = QueryPerformanceFrequency();
        public static readonly bool IsHighResolution = true;

        // performance-counter frequency, in counts per ticks.
        // This can speed up conversion from high frequency performance-counter
        // to ticks.
        private static readonly double s_tickFrequency = (double)TicksPerSecond / Frequency;

        public Stopwatch()
        {
            Reset();
        }

        public void Start()
        {
            // Calling start on a running Stopwatch is a no-op.
            if (_state > 0)
            {
                return;
            }

            // If we're unstarted, _state == 0 .
            //
            // If we have an existing elapsed time
            // it's -(elapsed time) so adding _state
            // backdates the new timestamp accordingly.
            _state = GetTimestamp() + _state;
        }

        public static Stopwatch StartNew()
        {
            Stopwatch s = new Stopwatch();
            s.Start();
            return s;
        }

        public void Stop()
        {
            // Calling stop on a stopped Stopwatch is a no-op.
            if (_state > 0)
            {
                long endTimeStamp = GetTimestamp();
                long elapsedThisPeriod = endTimeStamp - _state;

                // Buggy BIOS ir HAL can result in negative durations
                // clip to a zero in that case.
                elapsedThisPeriod = Math.Max(elapsedThisPeriod, 0);

                // Negative states encode an elapsed time.
                _state = -elapsedThisPeriod;
            }
        }

        public void Reset()
        {
            _state = 0;
        }

        // Convenience method for replacing {sw.Reset(); sw.Start();} with a single sw.Restart()
        public void Restart()
        {
            _state = GetTimestamp();
        }

        public bool IsRunning
        {
            get { return _state > 0; }
        }

        public TimeSpan Elapsed
        {
            get { return new TimeSpan(GetElapsedDateTimeTicks()); }
        }

        public long ElapsedMilliseconds
        {
            get { return GetElapsedDateTimeTicks() / TicksPerMillisecond; }
        }

        public long ElapsedTicks
        {
            get { return GetRawElapsedTicks(); }
        }

        public static long GetTimestamp()
        {
            Debug.Assert(IsHighResolution);
            return QueryPerformanceCounter();
        }

        /// <summary>Gets the elapsed time since the <paramref name="startingTimestamp"/> value retrieved using <see cref="GetTimestamp"/>.</summary>
        /// <param name="startingTimestamp">The timestamp marking the beginning of the time period.</param>
        /// <returns>A <see cref="TimeSpan"/> for the elapsed time between the starting timestamp and the time of this call.</returns>
        public static TimeSpan GetElapsedTime(long startingTimestamp) =>
            GetElapsedTime(startingTimestamp, GetTimestamp());

        /// <summary>Gets the elapsed time between two timestamps retrieved using <see cref="GetTimestamp"/>.</summary>
        /// <param name="startingTimestamp">The timestamp marking the beginning of the time period.</param>
        /// <param name="endingTimestamp">The timestamp marking the end of the time period.</param>
        /// <returns>A <see cref="TimeSpan"/> for the elapsed time between the starting and ending timestamps.</returns>
        public static TimeSpan GetElapsedTime(long startingTimestamp, long endingTimestamp) =>
            new TimeSpan((long)((endingTimestamp - startingTimestamp) * s_tickFrequency));

        // Get the elapsed ticks.
        private long GetRawElapsedTicks()
        {
            long stateCopy = _state;

            // Unstarted, or stopped with an elapsed time > 0 .
            if (stateCopy <= 0)
            {
                return Math.Abs(stateCopy);
            }

            long currentTimeStamp = GetTimestamp();
            long elapsedSoFar = currentTimeStamp - stateCopy;

            // Buggy BIOS or HAL can result in negative durations
            // clip to a zero in that case.
            elapsedSoFar = Math.Max(elapsedSoFar, 0);

            return elapsedSoFar;
        }

        // Get the elapsed ticks.
        private long GetElapsedDateTimeTicks()
        {
            Debug.Assert(IsHighResolution);
            // convert high resolution perf counter to DateTime ticks
            return unchecked((long)(GetRawElapsedTicks() * s_tickFrequency));
        }
    }
}
