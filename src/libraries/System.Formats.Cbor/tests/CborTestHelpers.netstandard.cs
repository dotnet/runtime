// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Formats.Cbor.Tests
{
    internal static class CborTestHelpers
    {
        private const long UnixEpochTicks = 719162L /*Number of days from 1/1/0001 to 12/31/1969*/ * 10000 * 1000 * 60 * 60 * 24; /* Ticks per day.*/
        public static readonly DateTimeOffset UnixEpoch = new DateTimeOffset(UnixEpochTicks, TimeSpan.Zero);

        public static unsafe int SingleToInt32Bits(float value)
            => *((int*)&value);
    }
}
