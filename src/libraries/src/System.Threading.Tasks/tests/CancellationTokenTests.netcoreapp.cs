// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Threading.Tasks.Tests
{
    public static partial class CancellationTokenTests
    {
        [Fact]
        public static void CancellationTokenRegistration_Token_MatchesExpectedValue()
        {
            Assert.Equal(default(CancellationToken), default(CancellationTokenRegistration).Token);

            var cts = new CancellationTokenSource();
            Assert.NotEqual(default(CancellationToken), cts.Token);

            using (var ctr = cts.Token.Register(() => { }))
            {
                Assert.Equal(cts.Token, ctr.Token);
            }
        }

        [Fact]
        public static void CancellationTokenRegistration_UnregisterOnDefaultIsNop()
        {
            Assert.False(default(CancellationTokenRegistration).Unregister());
        }

        [Fact]
        public static void CancellationTokenRegistration_UnregisterRemovesDelegate()
        {
            var cts = new CancellationTokenSource();
            bool invoked = false;
            CancellationTokenRegistration ctr = cts.Token.Register(() => invoked = true);
            Assert.True(ctr.Unregister());
            Assert.False(ctr.Unregister());
            cts.Cancel();
            Assert.False(invoked);
        }

        [Fact]
        public static void CancellationTokenRegistration_UnregisterDoesntWaitForCallbackToComplete()
        {
            using (var barrier = new Barrier(2))
            {
                var cts = new CancellationTokenSource();
                CancellationTokenRegistration ctr = cts.Token.Register(() =>
                {
                    barrier.SignalAndWait();
                    barrier.SignalAndWait();
                });

                Task.Run(() => cts.Cancel());

                // Validate that Unregister doesn't block waiting for the callback to complete.
                // (If it did block, this would deadlock.)
                barrier.SignalAndWait();
                Assert.False(ctr.Unregister());
                barrier.SignalAndWait();
            }
        }
    }
}
