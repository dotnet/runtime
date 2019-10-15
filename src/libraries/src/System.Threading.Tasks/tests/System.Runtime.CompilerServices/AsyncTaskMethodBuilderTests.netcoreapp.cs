// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Threading.Tasks.Tests
{
    public partial class AsyncTaskMethodBuilderTests
    {
        [OuterLoop]
        [Fact]
        public static void DroppedIncompleteStateMachine_RaisesIncompleteAsyncMethodEvent()
        {
            RemoteExecutor.Invoke(() =>
            {
                using (var listener = new TestEventListener("System.Threading.Tasks.TplEventSource", EventLevel.Verbose))
                {
                    var events = new ConcurrentQueue<EventWrittenEventArgs>();
                    listener.RunWithCallback(events.Enqueue, () =>
                    {
                        NeverCompletes();
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.WaitForPendingFinalizers();
                    });

                    Assert.DoesNotContain(events, ev => ev.EventId == 0); // errors from the EventSource itself
                    EventWrittenEventArgs iam = events.SingleOrDefault(e => e.EventName == "IncompleteAsyncMethod");
                    Assert.NotNull(iam);
                    Assert.NotNull(iam.Payload);

                    string description = iam.Payload[0] as string;
                    Assert.NotNull(description);
                    Assert.Contains(nameof(NeverCompletesAsync), description);
                    Assert.Contains("__state", description);
                    Assert.Contains("local1", description);
                    Assert.Contains("local2", description);
                    Assert.Contains("42", description);
                    Assert.Contains("stored data", description);
                }

                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void NeverCompletes() { var ignored = NeverCompletesAsync(); }

        private static async Task NeverCompletesAsync()
        {
            int local1 = 42;
            string local2 = "stored data";
            await new TaskCompletionSource<bool>().Task; // await will never complete
            GC.KeepAlive(local1);
            GC.KeepAlive(local2);
        }
    }
}
