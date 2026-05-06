// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System
{
    // Provide date/time test helpers
    public static class DateTimeTestHelpers
    {
        // Fixed DateTimeOffset, to avoid the use of DateTimeOffset.(Utc)Now in situations that simply require a value be present.
        public static DateTimeOffset FixedDateTimeOffsetValue => new DateTimeOffset(2001, 2, 3, 4, 5, 6, 789, TimeSpan.Zero);

        // Fixed DateTime, to avoid the use of DateTime.(Utc)Now in situations that simply require a value be present.
        public static DateTime FixedDateTimeValue => FixedDateTimeOffsetValue.UtcDateTime;
    }

}
