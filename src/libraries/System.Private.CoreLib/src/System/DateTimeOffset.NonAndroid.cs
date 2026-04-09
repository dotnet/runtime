// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    public readonly partial struct DateTimeOffset
    {
        // Returns a DateTimeOffset representing the current date and time. The
        // resolution of the returned value depends on the system timer.
        public static DateTimeOffset Now => ToLocalTime(DateTime.UtcNow, true);
    }
}
