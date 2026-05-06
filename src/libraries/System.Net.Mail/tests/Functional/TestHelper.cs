// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Mail.Tests
{
    public static class TestHelper
    {
        public const int PassingTestTimeoutMilliseconds = 1 * 60 * 1000;
        public static TimeSpan PassingTestTimeout => TimeSpan.FromMilliseconds(PassingTestTimeoutMilliseconds);
    }
}
