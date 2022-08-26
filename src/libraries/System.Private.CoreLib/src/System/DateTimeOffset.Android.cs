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

        // TryGetNow is a helper function on Android responsible for:
        // 1) quickly returning a fast path offset when first called
        // 2) starting a background thread to pursue the original implementation
        //
        // On Android, loading AndroidTZData is expensive for startup performance.
        // The fast result relies on `System.TimeZoneInfo.LocalDateTimeOffset` being set
        // in the App Context's properties as the appropriate local date time offset from UTC.
        // monovm_initialize(_preparsed) can be leveraged to do so.
        // However, to handle timezone changes during the app lifetime, AndroidTZData needs to be loaded.
        // So, on first call, we return the fast path and start a background thread to pursue the original
        // implementation which eventually loads AndroidTZData.

        private static TimeSpan TryGetNow()
        {
            if (s_androidTZDataLoaded) // The background thread finished, the cache is loaded.
                return TimeZoneInfo.GetLocalUtcOffset(DateTime.UtcNow, TimeZoneInfoOptions.NoThrowOnInvalidTime);

            if (!s_androidTZDataLoaded && s_loadAndroidTZData == null) // The cache isn't loaded and no background thread has been created
            {
                lock (s_localUtcOffsetLock)
                {
                    // GetLocalUtcOffset may be called multiple times before a cache is loaded and a background thread is running,
                    // once the lock is available, check for a cache and background thread.
                    if (s_androidTZDataLoaded)
                        return TimeZoneInfo.GetLocalUtcOffset(DateTime.UtcNow, TimeZoneInfoOptions.NoThrowOnInvalidTime);

                    if (s_loadAndroidTZData == null)
                    {
                        s_loadAndroidTZData = new Thread(() => {
                            Thread.Sleep(1000);
                            _ = TimeZoneInfo.GetLocalUtcOffset(DateTime.UtcNow, TimeZoneInfoOptions.NoThrowOnInvalidTime);
                            lock (s_localUtcOffsetLock)
                            {
                                s_androidTZDataLoaded = true;
                                s_loadAndroidTZData = null; // Ensure thread is cleared when cache is loaded
                            }
                        });
                        s_loadAndroidTZData.IsBackground = true;
                        s_loadAndroidTZData.Start();
                    }
                }
            }

            object? localDateTimeOffset = AppContext.GetData("System.TimeZoneInfo.LocalDateTimeOffset");
            if (localDateTimeOffset == null)
                return TimeZoneInfo.GetLocalUtcOffset(DateTime.UtcNow, TimeZoneInfoOptions.NoThrowOnInvalidTime); // If no offset property provided through monovm app context, default

            int localDateTimeOffsetSeconds = Convert.ToInt32(localDateTimeOffset);
            TimeSpan offset = TimeSpan.FromSeconds(localDateTimeOffsetSeconds);
            return offset;
        }

        public static DateTimeOffset Now
        {
            get
            {
                DateTime utcDateTime = DateTime.UtcNow;
                TimeSpan offset = TryGetNow();
                long localTicks = utcDateTime.Ticks + offset.Ticks;
                if (localTicks < DateTime.MinTicks || localTicks > DateTime.MaxTicks)
                    throw new ArgumentException(SR.Arg_ArgumentOutOfRangeException);

                return new DateTimeOffset(localTicks, offset);
            }
        }
    }
}
