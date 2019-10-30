// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace System.Net.Sockets.Tests
{
    public static class TestSettings
    {
        // Timeout values in milliseconds.
        public const int PassingTestTimeout = 10000;
        public const int FailingTestTimeout = 100;

        // Number of redundant UDP packets to send to increase test reliability
        // Update: was 10, changing to 1 to measure impact of random test failures occurring on *nix.
        // Certain random failures appear to be caused by a UDP client sending in a loop (based on UDPRedundancy)
        // to a server which was closed but another server created (on a different thread \ test) that happens to
        // have the same port #.
        // This occurs on *nix but not Windows because *nix uses random values (1024-65535) while Windows increments.
        public const int UDPRedundancy = 1;

        public static Task WhenAllOrAnyFailedWithTimeout(params Task[] tasks) => tasks.WhenAllOrAnyFailed(PassingTestTimeout);
    }
}
