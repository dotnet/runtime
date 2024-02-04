// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Buffers.ArrayPool.Tests
{
    public class CollectionTests : ArrayPoolTest
    {
        [OuterLoop("This is a long running test (over 2 minutes)")]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void BuffersAreCollectedWhenStale()
        {
            RemoteInvokeWithTrimming(() =>
            {
                const int BufferCount = 8;
                const int BufferSize = 1025;

                List<int[]> rentedBuffers = new List<int[]>();

                // Rent and return a set of buffers
                for (int i = 0; i < BufferCount; i++)
                {
                    rentedBuffers.Add(ArrayPool<int>.Shared.Rent(BufferSize));
                }
                for (int i = 0; i < BufferCount; i++)
                {
                    ArrayPool<int>.Shared.Return(rentedBuffers[i]);
                }

                // Rent what we returned and ensure they are the same
                for (int i = 0; i < BufferCount; i++)
                {
                    var buffer = ArrayPool<int>.Shared.Rent(BufferSize);
                    Assert.Contains(rentedBuffers, item => ReferenceEquals(item, buffer));
                }
                for (int i = 0; i < BufferCount; i++)
                {
                    ArrayPool<int>.Shared.Return(rentedBuffers[i]);
                }

                // Trigger a few Gen2 GCs to make sure the pool has appropriately time stamped buffers.
                for (int i = 0; i < 2; i++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }

                // Now wait a little over a minute and force a GC to get some buffers returned
                Console.WriteLine("Waiting a minute for buffers to go stale...");
                Thread.Sleep(61 * 1000);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                bool foundNewBuffer = false;
                for (int i = 0; i < BufferCount; i++)
                {
                    var buffer = ArrayPool<int>.Shared.Rent(BufferSize);
                    if (!rentedBuffers.Any(item => ReferenceEquals(item, buffer)))
                    {
                        foundNewBuffer = true;
                    }
                }

                Assert.True(foundNewBuffer);
            }, 3 * 60 * 1000); // This test has to wait for the buffers to go stale (give it three minutes)
        }

        private static bool IsStressModeEnabledAndRemoteExecutorSupported => TestEnvironment.IsStressModeEnabled && RemoteExecutor.IsSupported;

        // This test can cause problems for other tests run in parallel (from other assemblies) as
        // it pushes the physical memory usage above 80% temporarily.
        [ConditionalFact(nameof(IsStressModeEnabledAndRemoteExecutorSupported))]
        public unsafe void ThreadLocalIsCollectedUnderHighPressure()
        {
            RemoteInvokeWithTrimming(() =>
            {
                // Create our buffer, return it, re-rent it and ensure we have the same one
                const int BufferSize = 4097;
                byte[] buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
                ArrayPool<byte>.Shared.Return(buffer);
                Assert.Same(buffer, ArrayPool<byte>.Shared.Rent(BufferSize));

                // Return it and put memory pressure on to get it cleared
                ArrayPool<byte>.Shared.Return(buffer);

                const int AllocSize = 1024 * 1024 * 64;
                int PageSize = Environment.SystemPageSize;
                var pressureMethod = ArrayPool<byte>.Shared.GetType().GetMethod("GetMemoryPressure", BindingFlags.Static | BindingFlags.NonPublic);
                do
                {
                    Span<byte> native = new Span<byte>(Marshal.AllocHGlobal(AllocSize).ToPointer(), AllocSize);

                    // Touch the pages to bring them into physical memory
                    for (int i = 0; i < native.Length; i += PageSize)
                    {
                        native[i] = 0xEF;
                    }

                    GC.Collect(2);
                } while ((int)pressureMethod.Invoke(null, null) != 2);

                GC.WaitForPendingFinalizers();

                // Should have a new buffer now
                Assert.NotSame(buffer, ArrayPool<byte>.Shared.Rent(BufferSize));
            });
        }

        // This test can cause problems for other tests run in parallel (from other assemblies) as
        // it pushes the physical memory usage above 80% temporarily.
        [OuterLoop("This is a long running test (over 2 minutes)")]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public unsafe void ThreadLocalIsCollectedUnderNormalPressure()
        {
            RemoteInvokeWithTrimming(() =>
            {
                // Create our buffer, return it, re-rent it and ensure we have the same one
                const int BufferSize = 4097;
                byte[] buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
                ArrayPool<byte>.Shared.Return(buffer);
                Assert.Same(buffer, ArrayPool<byte>.Shared.Rent(BufferSize));

                // Return it and put memory pressure on to get it cleared
                ArrayPool<byte>.Shared.Return(buffer);

                // Make sure buffer gets time stamped
                for (int i = 0; i < 2; i++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }

                // Now wait for enough time to pass and force a GC to get buffers dropped
                Console.WriteLine("Waiting a minute for buffers to go stale...");
                Thread.Sleep(61 * 1000);
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // Should have a new buffer now
                Assert.NotSame(buffer, ArrayPool<byte>.Shared.Rent(BufferSize));
            }, 3 * 60 * 1000); // This test has to wait for the buffers to go stale (give it three minutes)
        }

        private static bool IsPreciseGcSupportedAndRemoteExecutorSupported => PlatformDetection.IsPreciseGcSupported && RemoteExecutor.IsSupported;

        [ActiveIssue("https://github.com/dotnet/runtime/issues/44037")]
        [ConditionalFact(nameof(IsPreciseGcSupportedAndRemoteExecutorSupported))]
        public void PollingEventFires()
        {
            RemoteInvokeWithTrimming(() =>
            {
                bool pollEventFired = false;
                float[] buffer = ArrayPool<float>.Shared.Rent(10);

                // Polling doesn't start until the thread locals are created for a pool.
                // Try before the return then after.

                RunWithListener(() =>
                {
                    GC.Collect(2);
                    GC.WaitForPendingFinalizers();
                },
                EventLevel.Informational,
                e =>
                {
                    if (e.EventId == EventIds.BufferTrimPoll)
                        pollEventFired = true;
                });

                Assert.False(pollEventFired, "collection isn't hooked up until the first item is returned");
                ArrayPool<float>.Shared.Return(buffer);

                RunWithListener(() =>
                {
                    GC.Collect(2);
                    GC.WaitForPendingFinalizers();
                },
                EventLevel.Informational,
                e =>
                {
                    if (e.EventId == EventIds.BufferTrimPoll)
                        pollEventFired = true;
                });

                // Polling events should only fire when trimming is enabled
                Assert.True(pollEventFired);
            });
        }
    }
}
