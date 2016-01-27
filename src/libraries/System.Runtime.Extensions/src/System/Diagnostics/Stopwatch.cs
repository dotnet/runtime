// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics
{
    // This class uses high-resolution performance counter if installed hardware 
    // does not support it. Otherwise, the class will fall back to DateTime class
    // and uses ticks as a measurement.

    public partial class Stopwatch
    {
        private const long TicksPerMillisecond = 10000;
        private const long TicksPerSecond = TicksPerMillisecond * 1000;

        private long _elapsed;
        private long _startTimeStamp;
        private bool _isRunning;

        // "Frequency" stores the frequency of the high-resolution performance counter, 
        // if one exists. Otherwise it will store TicksPerSecond. 
        // The frequency cannot change while the system is running,
        // so we only need to initialize it once. 
        public static readonly long Frequency;
        public static readonly bool IsHighResolution;

        // performance-counter frequency, in counts per ticks.
        // This can speed up conversion from high frequency performance-counter 
        // to ticks. 
        private static readonly double s_tickFrequency;

        static Stopwatch()
        {
            bool succeeded = QueryPerformanceFrequency(out Frequency);

            if (!succeeded)
            {
                IsHighResolution = false;
                Frequency = TicksPerSecond;
                s_tickFrequency = 1;
            }
            else
            {
                IsHighResolution = true;
                s_tickFrequency = TicksPerSecond;
                s_tickFrequency /= Frequency;
            }
        }

        public Stopwatch()
        {
            Reset();
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
            Stopwatch s = new Stopwatch();
            s.Start();
            return s;
        }

        public void Stop()
        {
            // Calling stop on a stopped Stopwatch is a no-op.
            if (_isRunning)
            {
                long endTimeStamp = GetTimestamp();
                long elapsedThisPeriod = endTimeStamp - _startTimeStamp;
                _elapsed += elapsedThisPeriod;
                _isRunning = false;

                if (_elapsed < 0)
                {
                    // When measuring small time periods the StopWatch.Elapsed* 
                    // properties can return negative values.  This is due to 
                    // bugs in the basic input/output system (BIOS) or the hardware
                    // abstraction layer (HAL) on machines with variable-speed CPUs
                    // (e.g. Intel SpeedStep).

                    _elapsed = 0;
                }
            }
        }

        public void Reset()
        {
            _elapsed = 0;
            _isRunning = false;
            _startTimeStamp = 0;
        }

        // Convenience method for replacing {sw.Reset(); sw.Start();} with a single sw.Restart()
        public void Restart()
        {
            _elapsed = 0;
            _startTimeStamp = GetTimestamp();
            _isRunning = true;
        }

        public bool IsRunning
        {
            get { return _isRunning; }
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
            if (IsHighResolution)
            {
                long timestamp = 0;
                QueryPerformanceCounter(out timestamp);
                return timestamp;
            }
            else
            {
                return DateTime.UtcNow.Ticks;
            }
        }

        // Get the elapsed ticks.        
        private long GetRawElapsedTicks()
        {
            long timeElapsed = _elapsed;

            if (_isRunning)
            {
                // If the StopWatch is running, add elapsed time since
                // the Stopwatch is started last time. 
                long currentTimeStamp = GetTimestamp();
                long elapsedUntilNow = currentTimeStamp - _startTimeStamp;
                timeElapsed += elapsedUntilNow;
            }
            return timeElapsed;
        }

        // Get the elapsed ticks.        
        private long GetElapsedDateTimeTicks()
        {
            long rawTicks = GetRawElapsedTicks();
            if (IsHighResolution)
            {
                // convert high resolution perf counter to DateTime ticks
                double dticks = rawTicks;
                dticks *= s_tickFrequency;
                return unchecked((long)dticks);
            }
            else
            {
                return rawTicks;
            }
        }
    }
}
