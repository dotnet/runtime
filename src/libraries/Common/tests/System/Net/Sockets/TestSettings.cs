// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Sockets.Tests
{
    public static class TestSettings
    {
        // Timeout values in milliseconds.
        public const int PassingTestTimeout = 5000;
        public const int FailingTestTimeout = 100;

        // Number of redundant UDP packets to send to increase test reliability
        public const int UDPRedundancy = 10;
    }
}
