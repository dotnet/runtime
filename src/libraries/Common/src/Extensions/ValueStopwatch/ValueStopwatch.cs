// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.Extensions.Internal 
{
    internal struct ValueStopwatch
    {
        private static readonly double TimestampToTicks = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;

        private long _startTimestamp;

        public bool IsActive => _startTimestamp != 0;

        private ValueStopwatch(long startTimestamp)
        {
            _startTimestamp = startTimestamp;
        }

        public static ValueStopwatch StartNew() => new ValueStopwatch(Stopwatch.GetTimestamp());

        public TimeSpan GetElapsedTime()
        {
            // Start timestamp can't be zero in an initialized ValueStopwatch. It would have to be literally the first thing executed when the machine boots to be 0.
            // So it being 0 is a clear indication of default(ValueStopwatch)
            if (!IsActive)
            {
                throw new InvalidOperationException("An uninitialized, or 'default', ValueStopwatch cannot be used to get elapsed time.");
            }

            var end = Stopwatch.GetTimestamp();
            var timestampDelta = end - _startTimestamp;
            var ticks = (long)(TimestampToTicks * timestampDelta);
            return new TimeSpan(ticks);
        }
    }
}
