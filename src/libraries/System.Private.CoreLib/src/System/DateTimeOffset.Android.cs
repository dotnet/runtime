// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Threading;

namespace System
{
    public readonly partial struct DateTimeOffset
    {
        // 0 == in process of being loaded, 1 == loaded
        private static volatile int s_androidTZDataLoaded = -1;

        // Now on Android does the following
        // 1) quickly returning a fast path result when first called if the right AppContext data element is set
        // 2) starting a background thread to load TimeZoneInfo local cache
        //
        // On Android, loading AndroidTZData is expensive for startup performance.
        // The fast result relies on `System.TimeZoneInfo.LocalDateTimeOffset` being set
        // in the App Context's properties as the appropriate local date time offset from UTC.
        // monovm_initialize(_preparsed) can be leveraged to do so.
        // However, to handle timezone changes during the app lifetime, AndroidTZData needs to be loaded.
        // So, on first call, we return the fast path and start a background thread to load
        // the TimeZoneInfo Local cache implementation which loads AndroidTZData.
        public static DateTimeOffset Now
        {
            get
            {
                DateTime utcDateTime = DateTime.UtcNow;

                if (s_androidTZDataLoaded == 1) // The background thread finished, the cache is loaded.
                {
                    return ToLocalTime(utcDateTime, true);
                }

                object? localDateTimeOffset = AppContext.GetData("System.TimeZoneInfo.LocalDateTimeOffset");
                if (localDateTimeOffset == null) // If no offset property provided through monovm app context, default
                {
                    // no need to create the thread, load tzdata now
                    s_androidTZDataLoaded = 1;
                    return ToLocalTime(utcDateTime, true);
                }

                // The cache isn't loaded yet.
                if (Interlocked.CompareExchange(ref s_androidTZDataLoaded, 0, -1) == -1)
                {
                    new Thread(() =>
                    {
                        try
                        {
                            // Delay the background thread to avoid impacting startup, if it still coincides after 1s, startup is already perceived as slow
                            Thread.Sleep(1000);

                            _ = TimeZoneInfo.Local; // Load AndroidTZData
                        }
                        finally
                        {
                            s_androidTZDataLoaded = 1;
                        }
                    }) { IsBackground = true }.Start();
                }

                // Fast path obtained offset incorporated into ToLocalTime(DateTime.UtcNow, true) logic
                int localDateTimeOffsetSeconds = Convert.ToInt32(localDateTimeOffset, CultureInfo.InvariantCulture);
                TimeSpan offset = TimeSpan.FromSeconds(localDateTimeOffsetSeconds);
                long localTicks = utcDateTime.Ticks + offset.Ticks;
                if (localTicks < DateTime.MinTicks || localTicks > DateTime.MaxTicks)
                    throw new ArgumentException(SR.Arg_ArgumentOutOfRangeException);

                return new DateTimeOffset(localTicks, offset);
            }
        }
    }
}
