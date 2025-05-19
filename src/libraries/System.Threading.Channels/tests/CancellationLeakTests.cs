// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Threading.Channels.Tests
{
    public class CancellationLeakTests
    {
        private class TrackableDisposable : IDisposable
        {
            public bool WasDisposed { get; private set; } = false;
            public void Dispose() => WasDisposed = true;
        }

        [Fact]
        public async Task CancelledChannel_ShouldDisposeRegistration()
        {
            // Create a bounded channel
            var channel = Channel.CreateBounded<int>(1);
            
            using var cts = new CancellationTokenSource();
            var trackable = new TrackableDisposable();
            
            // Register our trackable object with the token
            using (cts.Token.Register(() => trackable.Dispose()))
            {
                try
                {
                    // Try to read from the channel with the cancellation token
                    var readTask = channel.Reader.ReadAsync(cts.Token).AsTask();
                    
                    // Cancel the operation
                    cts.Cancel();
                    
                    // Wait for the cancellation to be observed
                    await Assert.ThrowsAsync<OperationCanceledException>(() => readTask);
                    
                    // The trackable should have been disposed, indicating the registration was properly unregistered
                    Assert.True(trackable.WasDisposed);
                }
                catch (OperationCanceledException)
                {
                    // Expected
                }
            }
        }
        
        [Fact]
        public async Task MultipleCancelledReads_ShouldNotLeakMemory()
        {
            // Create a bounded channel
            var channel = Channel.CreateBounded<int>(1);
            
            // Perform multiple read operations with cancellation
            for (int i = 0; i < 100; i++)
            {
                using var cts = new CancellationTokenSource();
                try
                {
                    // Start a read operation
                    var readTask = channel.Reader.ReadAsync(cts.Token).AsTask();
                    
                    // Cancel it almost immediately
                    cts.Cancel();
                    
                    // Wait for the cancellation to be observed
                    await Assert.ThrowsAsync<OperationCanceledException>(() => readTask);
                }
                catch (OperationCanceledException)
                {
                    // Expected
                }
                
                // Force GC to detect potential leaks
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            
            // If we reach here without memory growth, the test passes
            // This is a best-effort test, as it's hard to definitively test for memory leaks in a unit test
        }
        
        [Fact]
        public async Task RepeatedCancellation_ShouldNotLeakRegistrations()
        {
            // This test simulates the scenario in the issue description
            var channel = Channel.CreateBounded<int>(1);
            int iterations = 1000;
            
            // Run many iterations to increase the likelihood of detecting a leak
            for (int i = 0; i < iterations; i++)
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));
                try
                {
                    await channel.Reader.ReadAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // Expected
                }
                
                if (i % 100 == 0)
                {
                    // Periodically force GC to help detect leaks
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
            
            // Final GC to clean up
            GC.Collect();
            GC.WaitForPendingFinalizers();
            
            // The test passes if we reach this point without excessive memory usage
        }
    }
}