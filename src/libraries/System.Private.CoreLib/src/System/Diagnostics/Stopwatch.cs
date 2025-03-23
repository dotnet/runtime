// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public partial class Stopwatch
    {
        private long _elapsed;
        private long _startTimeStamp;
        private bool _isRunning;

        public static readonly long Frequency = QueryPerformanceFrequency();
        public static readonly bool IsHighResolution = true;

        // performance-counter frequency, in counts per ticks.
        // This can speed up conversion to ticks.
        private static readonly double s_tickFrequency = (double)TimeSpan.TicksPerSecond / Frequency;

        public Stopwatch()
        {
        }

        public void Start()
        {
            // Calling start on a running Stopwatch is a no-op.
            if (!_isRunning)
            {
                _startTimeStamp = GetTimestamp();
                _isRunning = true;
            }
        }

        public static Stopwatch StartNew()
        {
            Stopwatch s = new();
            s.Start();
            return s;
        }

        public void Stop()
        {
            // Calling stop on a stopped Stopwatch is a no-op.
            if (_isRunning)
            {
                _elapsed += GetTimestamp() - _startTimeStamp;
                _isRunning = false;
            }
        }

        public void Reset()
        {
            _elapsed = 0;
            _startTimeStamp = 0;
            _isRunning = false;
        }

        // Convenience method for replacing {sw.Reset(); sw.Start();} with a single sw.Restart()
        public void Restart()
        {
            _elapsed = 0;
            _startTimeStamp = GetTimestamp();
            _isRunning = true;
        }

        /// <summary>
        /// Returns the <see cref="Elapsed"/> time as a string.
        /// </summary>
        /// <returns>
        /// Elapsed time string in the same format used by <see cref="TimeSpan.ToString()"/>.
        /// </returns>
        public override string ToString() => Elapsed.ToString();

        public bool IsRunning => _isRunning;

        public TimeSpan Elapsed => new(ElapsedTimeSpanTicks);

        public long ElapsedMilliseconds => ElapsedTimeSpanTicks / TimeSpan.TicksPerMillisecond;

        public long ElapsedTicks
        {
            get
            {
                long timeElapsed = _elapsed;

                // If the Stopwatch is running, add elapsed time since the Stopwatch is started last time.
                if (_isRunning)
                {
                    timeElapsed += GetTimestamp() - _startTimeStamp;
                }

                return timeElapsed;
            }
        }

        public static long GetTimestamp() => QueryPerformanceCounter();

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
            new((long)((endingTimestamp - startingTimestamp) * s_tickFrequency));

        private long ElapsedTimeSpanTicks => (long)(ElapsedTicks * s_tickFrequency);

        private string DebuggerDisplay => $"{Elapsed} (IsRunning = {_isRunning})";
    }
}
