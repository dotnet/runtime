// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace System
{
    public readonly partial struct DateTimeOffset
    {
        private static bool s_androidTZDataLoaded;
        private static readonly object s_localUtcOffsetLock = new();
        private static Thread? s_loadAndroidTZData;
        private static bool s_startNewBackgroundThread = true;

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

                if (s_androidTZDataLoaded) // The background thread finished, the cache is loaded.
                    return ToLocalTime(utcDateTime, true);

                if (s_startNewBackgroundThread) // The cache isn't loaded and no background thread has been created
                {
                    lock (s_localUtcOffsetLock)
                    {
                        // Now may be called multiple times before a cache is loaded and a background thread is running,
                        // once the lock is available, check for a cache and background thread.
                        if (s_androidTZDataLoaded)
                            return ToLocalTime(utcDateTime, true);

                        if (s_loadAndroidTZData == null)
                        {
                            s_loadAndroidTZData = new Thread(() => {
                                // Delay the background thread to avoid impacting startup, if it still coincides after 1s, startup is already perceived as slow
                                Thread.Sleep(1000);

                                _ = TimeZoneInfo.Local; // Load AndroidTZData
                                s_androidTZDataLoaded = true;

                                lock (s_localUtcOffsetLock)
                                {
                                    s_loadAndroidTZData = null; // Ensure thread is cleared when cache is loaded
                                }
                            });
                            s_loadAndroidTZData.IsBackground = true;
                        }
                    }

                    if (s_startNewBackgroundThread)
                    {
                        // Because Start does not block the calling thread,
                        // setting the boolean flag to false immediately after should
                        // prevent two calls to DateTimeOffset.Now in quick succession
                        // from both reaching here.
                        s_loadAndroidTZData.Start();
                        s_startNewBackgroundThread = false;
                    }
                }


                object? localDateTimeOffset = AppContext.GetData("System.TimeZoneInfo.LocalDateTimeOffset");
                if (localDateTimeOffset == null) // If no offset property provided through monovm app context, default
                    return ToLocalTime(utcDateTime, true);

                // Fast path obtained offset incorporated into ToLocalTime(DateTime.UtcNow, true) logic
                int localDateTimeOffsetSeconds = Convert.ToInt32(localDateTimeOffset);
                TimeSpan offset = TimeSpan.FromSeconds(localDateTimeOffsetSeconds);
                long localTicks = utcDateTime.Ticks + offset.Ticks;
                if (localTicks < DateTime.MinTicks || localTicks > DateTime.MaxTicks)
                    throw new ArgumentException(SR.Arg_ArgumentOutOfRangeException);

                return new DateTimeOffset(localTicks, offset);
            }
        }
    }
}
