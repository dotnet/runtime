// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.ComponentModel.EventBasedAsync
{
    public class AsyncOperationTests
    {
        private const int SpinTimeoutSeconds = 30;

        [Fact]
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

                }).Wait();
        }

        [Fact]
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
                }).Wait();
        }

        [Fact(Skip = "Bug 1092169")]
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
               }).Wait();
        }

        [Fact]
        public static void Cancel()
        {
            // Test that cancellation gets passed all the way through PostOperationCompleted(callback, AsyncCompletedEventArgs)
            Task.Run(() =>
             {
                 var cancelEvent = new ManualResetEventSlim();
                 var operation = new TestAsyncOperation(op =>
                 {
                     var ret = cancelEvent.Wait(SpinTimeoutSeconds*1000);
                     Assert.True(ret);
                 }, cancelEvent: cancelEvent);

                 operation.Cancel();
                 operation.Wait();
                 Assert.True(operation.Completed);
                 Assert.True(operation.Cancelled);
                 Assert.Null(operation.Exception);
             }).Wait();
        }

        [Fact]
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
            }).Wait();
        }

        [Fact]
        public static void PostNullDelegate()
        {
            // the xUnit SynchronizationContext - AysncTestSyncContext interferes with the current SynchronizationContext
            // used by AsyncOperation when there is exception thrown -> the SC.OperationCompleted() is not called.
            // use  new SC here to avoid this issue
            var orignal = SynchronizationContext.Current;
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
                SynchronizationContext.SetSynchronizationContext(orignal);
            }
        }

        // A simple wrapper for AsyncOperation which executes the specified delegate and a completion handler asynchronously.
        public class TestAsyncOperation
        {
            private object _operationId;
            private Action<TestAsyncOperation> _executeDelegate;
            private ManualResetEventSlim _cancelEvent, _completeEvent;

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
                Assert.Equal(AsyncOperation.SynchronizationContext, AsyncOperationManager.SynchronizationContext);

                _completeEvent = new ManualResetEventSlim(false);
                _cancelEvent = cancelEvent ?? new ManualResetEventSlim(false);

                // Post work to the wrapped synchronization context
                _executeDelegate = executeDelegate;
                AsyncOperation.Post((SendOrPostCallback)ExecuteWorker, _operationId);
            }

            public void Wait()
            {
                var ret = _completeEvent.Wait(SpinTimeoutSeconds*1000);
                Assert.True(ret);

                if (this.Exception != null)
                {
                    throw this.Exception;
                }
            }

            public void Cancel()
            {
                CompleteOperationAsync(cancelled: true);
            }

            private void ExecuteWorker(object operationId)
            {
                Assert.True(_operationId == operationId, "AsyncOperationManager did not pass UserSuppliedState through to the operation.");

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
                var e = state as AsyncCompletedEventArgs;

                Assert.True(e != null, "The state passed to this operation must be of type AsyncCompletedEventArgs");
                Assert.Equal(_operationId, e.UserState);

                Exception = e.Error;

                _completeEvent.Set();
                if (e.Cancelled)
                    _cancelEvent.Set();
            }
        }

        public class TestException : Exception
        {
            public TestException(string message) :
                base(message)
            {
            }
        }

        public class TestTimeoutException : Exception
        {
        }
    }
}