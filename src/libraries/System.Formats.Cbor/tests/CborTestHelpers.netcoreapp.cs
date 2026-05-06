// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Formats.Cbor.Tests
{
    internal static class CborTestHelpers
    {
        public static readonly DateTimeOffset UnixEpoch = DateTimeOffset.UnixEpoch;

        public static int SingleToInt32Bits(float value)
            => BitConverter.SingleToInt32Bits(value);
    }
}
