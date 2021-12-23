// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    public readonly partial struct DateTime
    {
        internal const bool s_systemSupportsLeapSeconds = false;

        public static DateTime UtcNow
        {
            get
            {
                return new DateTime(((ulong)(Interop.Sys.GetSystemTimeAsTicks() + UnixEpochTicks)) | KindUtc);
            }
        }

        private static DateTime FromFileTimeLeapSecondsAware(ulong _) => default;
        private static ulong ToFileTimeLeapSecondsAware(long _) => default;

        // IsValidTimeWithLeapSeconds is not expected to be called at all for now on non-Windows platforms
        internal static bool IsValidTimeWithLeapSeconds(int _, int _1, int _2, int _3, int _4, DateTimeKind _5) => false;
    }
}
