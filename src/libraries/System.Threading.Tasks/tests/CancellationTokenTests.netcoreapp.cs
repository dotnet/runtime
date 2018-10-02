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
        public static void CancellationTokenRegistration_UnregisterWhileCallbackRunning_UnregisterDoesntWaitForCallbackToComplete()
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

        [Fact]
        public static void CancellationTokenRegistration_UnregisterDuringCancellation_SuccessfullyRemovedIfNotYetInvoked()
        {
            var ctr0running = new ManualResetEventSlim();
            var ctr2blocked = new ManualResetEventSlim();
            var ctr2running = new ManualResetEventSlim();
            var cts = new CancellationTokenSource();

            CancellationTokenRegistration ctr0 = cts.Token.Register(() => ctr0running.Set());

            bool ctr1Invoked = false;
            CancellationTokenRegistration ctr1 = cts.Token.Register(() => ctr1Invoked = true);

            CancellationTokenRegistration ctr2 = cts.Token.Register(() =>
            {
                ctr2running.Set();
                ctr2blocked.Wait();
            });

            // Cancel.  This will trigger ctr2 to run, then ctr1, then ctr0.
            Task.Run(() => cts.Cancel());
            ctr2running.Wait(); // wait for ctr2 to start running
            Assert.False(ctr2.Unregister());

            // Now that ctr2 is running, unregister ctr1. This should succeed
            // and ctr1 should not run.
            Assert.True(ctr1.Unregister());

            // Allow ctr2 to continue.  ctr1 should not run.  ctr0 should, so wait for it.
            ctr2blocked.Set();
            ctr0running.Wait();
            Assert.False(ctr0.Unregister());
            Assert.False(ctr1Invoked);
        }

        [Fact]
        public static async Task CancellationTokenRegistration_ConcurrentUnregisterWithCancel_ReturnsFalseOrCallbackInvoked()
        {
            using (Barrier barrier = new Barrier(2))
            {
                const int Iters = 10_000;
                CancellationTokenSource cts = new CancellationTokenSource();
                bool unregisterResult = false, callbackInvoked = false;

                var tasks = new Task[]
                {
                    // Register and unregister
                    Task.Factory.StartNew(() =>
                    {
                        for (int i = 0; i < Iters; i++)
                        {
                            barrier.SignalAndWait();
                            CancellationTokenRegistration ctr = cts.Token.Register(() => callbackInvoked = true);
                            barrier.SignalAndWait();
                            unregisterResult = ctr.Unregister();
                            barrier.SignalAndWait();
                        }
                    }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default),

                    // Cancel, and validate the results
                    Task.Factory.StartNew(() =>
                    {
                        for (int i = 0; i < Iters; i++)
                        {
                            barrier.SignalAndWait();
                            barrier.SignalAndWait();
                            cts.Cancel();
                            barrier.SignalAndWait();

                            Assert.True(unregisterResult ^ callbackInvoked);

                            unregisterResult = callbackInvoked = false;
                            cts = new CancellationTokenSource();
                        }
                    }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)
                };

                // wait for one to fail or both to complete
                await await Task.WhenAny(tasks);
                await Task.WhenAll(tasks);
            }
        }
    }
}
