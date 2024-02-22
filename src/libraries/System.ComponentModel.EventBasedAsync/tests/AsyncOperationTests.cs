// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.ComponentModel.EventBasedAsync.Tests
{
    public class AsyncOperationTests
    {
        private const int SpinTimeoutSeconds = 30;

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedOrBrowserBackgroundExec))]
        public static void Noop()
        {
            // Test that a simple AsyncOperation can be dispatched and completed via AsyncOperationManager
            Task.Run(() =>
                {
                    var operation = new TestAsyncOperation(op => { });
                    operation.Wait();

                    Assert.True(operation.Completed);
                    Assert.False(operation.Cancelled);
                    Assert.Null(operation.Exception);

                }).GetAwaiter().GetResult();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedOrBrowserBackgroundExec))]
        public static void ThrowAfterAsyncComplete()
        {
            Task.Run(() =>
                {
                    var operation = new TestAsyncOperation(op => { });
                    operation.Wait();

                    SendOrPostCallback noopCallback = state => { };
                    Assert.Throws<InvalidOperationException>(() => operation.AsyncOperation.Post(noopCallback, null));
                    Assert.Throws<InvalidOperationException>(() => operation.AsyncOperation.PostOperationCompleted(noopCallback, null));
                    Assert.Throws<InvalidOperationException>(() => operation.AsyncOperation.OperationCompleted());
                }).GetAwaiter().GetResult();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedOrBrowserBackgroundExec))]
        public static void ThrowAfterSynchronousComplete()
        {
            Task.Run(() =>
               {
                   var operation = AsyncOperationManager.CreateOperation(null);
                   operation.OperationCompleted();

                   SendOrPostCallback noopCallback = state => { };
                   Assert.Throws<InvalidOperationException>(() => operation.Post(noopCallback, null));
                   Assert.Throws<InvalidOperationException>(() => operation.PostOperationCompleted(noopCallback, null));
                   Assert.Throws<InvalidOperationException>(() => operation.OperationCompleted());
               }).GetAwaiter().GetResult();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedOrBrowserBackgroundExec))]
        public static void Cancel()
        {
            // Test that cancellation gets passed all the way through PostOperationCompleted(callback, AsyncCompletedEventArgs)
            Task.Run(() =>
             {
                 var cancelEvent = new ManualResetEventSlim();
                 var operation = new TestAsyncOperation(op =>
                 {
                     Assert.True(cancelEvent.Wait(TimeSpan.FromSeconds(SpinTimeoutSeconds)));
                 }, cancelEvent: cancelEvent);

                 operation.Cancel();
                 operation.Wait();
                 Assert.True(operation.Completed);
                 Assert.True(operation.Cancelled);
                 Assert.Null(operation.Exception);
             }).GetAwaiter().GetResult();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedOrBrowserBackgroundExec))]
        public static void Throw()
        {
            // Test that exceptions get passed all the way through PostOperationCompleted(callback, AsyncCompletedEventArgs)
            Task.Run(() =>
            {
                var operation = new TestAsyncOperation(op =>
                {
                    throw new TestException("Test throw");
                });

                Assert.Throws<TestException>(() => operation.Wait());
            }).GetAwaiter().GetResult();
        }

        [Fact]
        public static void PostNullDelegate()
        {
            // the xUnit SynchronizationContext - AsyncTestSyncContext interferes with the current SynchronizationContext
            // used by AsyncOperation when there is exception thrown -> the SC.OperationCompleted() is not called.
            // use  new SC here to avoid this issue
            var original = SynchronizationContext.Current;
            try
            {
                SynchronizationContext.SetSynchronizationContext(null);

                // Pass a non-null state just to emphasize we're only testing passing a null delegate
                var state = new object();
                var operation = AsyncOperationManager.CreateOperation(state);
                Assert.Throws<ArgumentNullException>(() => operation.Post(null, state));
                Assert.Throws<ArgumentNullException>(() => operation.PostOperationCompleted(null, state));
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(original);
            }
        }

        // A simple wrapper for AsyncOperation which executes the specified delegate and a completion handler asynchronously.
        public class TestAsyncOperation
        {
            private readonly object _operationId;
            private readonly Action<TestAsyncOperation> _executeDelegate;
            private readonly ManualResetEventSlim _cancelEvent;
            private readonly ManualResetEventSlim _completeEvent;

            public AsyncOperation AsyncOperation { get; private set; }

            public bool Completed { get { return _completeEvent.IsSet; } }

            public bool Cancelled { get { return _cancelEvent.IsSet; } }

            public Exception Exception { get; private set; }

            public TestAsyncOperation(Action<TestAsyncOperation> executeDelegate, ManualResetEventSlim cancelEvent = null)
            {
                // Create an async operation passing an object as the state so we can
                // verify that state is passed properly.
                _operationId = new object();
                AsyncOperation = AsyncOperationManager.CreateOperation(_operationId);
                Assert.Same(_operationId, AsyncOperation.UserSuppliedState);
                Assert.Same(AsyncOperationManager.SynchronizationContext, AsyncOperation.SynchronizationContext);

                _completeEvent = new ManualResetEventSlim(false);
                _cancelEvent = cancelEvent ?? new ManualResetEventSlim(false);

                // Post work to the wrapped synchronization context
                _executeDelegate = executeDelegate;
                AsyncOperation.Post((SendOrPostCallback)ExecuteWorker, _operationId);
            }

            public void Wait()
            {
                Assert.True(_completeEvent.Wait(TimeSpan.FromSeconds(SpinTimeoutSeconds)));

                if (Exception != null)
                {
                    throw Exception;
                }
            }

            public void Cancel()
            {
                CompleteOperationAsync(cancelled: true);
            }

            private void ExecuteWorker(object operationId)
            {
                Assert.Same(_operationId, operationId);

                Exception exception = null;

                try
                {
                    _executeDelegate(this);
                }
                catch (Exception e)
                {
                    exception = e;
                }
                finally
                {
                    CompleteOperationAsync(exception: exception);
                }
            }

            private void CompleteOperationAsync(Exception exception = null, bool cancelled = false)
            {
                if (!(Completed || Cancelled))
                {
                    AsyncOperation.PostOperationCompleted(
                        (SendOrPostCallback)OnOperationCompleted,
                        new AsyncCompletedEventArgs(
                            exception,
                            cancelled,
                            _operationId));
                }
            }

            private void OnOperationCompleted(object state)
            {
                AsyncCompletedEventArgs e = Assert.IsType<AsyncCompletedEventArgs>(state);
                Assert.Equal(_operationId, e.UserState);

                Exception = e.Error;

                // Make sure to set _cancelEvent before _completeEvent so that anyone waiting on
                // _completeEvent will not be at risk of reading Cancelled before it is set.
                if (e.Cancelled)
                    _cancelEvent.Set();

                _completeEvent.Set();
            }
        }
    }
}
