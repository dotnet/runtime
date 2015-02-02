// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Threading;
using Xunit;

namespace System.ComponentModel.EventBasedAsync
{
    public class AsyncOperationTests
    {
        private const int k_spinTimeoutSeconds = 20;

        [Fact]
        public static void Noop()
        {
            // Test that a simple AsyncOperation can be dispatched and completed via AsyncOperationManager
            var operation = new TestAsyncOperation(op => {});
            operation.Wait();
            Assert.True(operation.Completed);
            Assert.False(operation.Cancelled);
            Assert.Null(operation.Exception);
        }

        [Fact]
        public static void ThrowAfterAsyncComplete()
        {
            var operation = new TestAsyncOperation(op => {});
            operation.Wait();

            SendOrPostCallback noopCallback = state => { };
            Assert.Throws<InvalidOperationException>(() => operation.AsyncOperation.Post(noopCallback, null));
            Assert.Throws<InvalidOperationException>(() => operation.AsyncOperation.PostOperationCompleted(noopCallback, null));
            Assert.Throws<InvalidOperationException>(() => operation.AsyncOperation.OperationCompleted());
        }

        [Fact(Skip="Bug 1092169")]
        public static void ThrowAfterSynchronousComplete()
        {
            var operation = AsyncOperationManager.CreateOperation(null);
            operation.OperationCompleted();

            SendOrPostCallback noopCallback = state => {};
            Assert.Throws<InvalidOperationException>(() => operation.Post(noopCallback, null));
            Assert.Throws<InvalidOperationException>(() => operation.PostOperationCompleted(noopCallback, null));
            Assert.Throws<InvalidOperationException>(() => operation.OperationCompleted());
        }

        [Fact]
        public static void Cancel()
        {
            // Test that cancellation gets passed all the way through PostOperationCompleted(callback, AsyncCompletedEventArgs)
            var operation = new TestAsyncOperation(op =>
            {
                var startTime = DateTime.Now;
                var timeoutTime = startTime.AddSeconds(k_spinTimeoutSeconds);
            
                while (!op.Cancelled)
                {
                    if (DateTime.Now > timeoutTime)
                    {
                        throw new TestTimeoutException();
                    }
                }
            });
            
            operation.Cancel();
            operation.Wait();
            Assert.True(operation.Completed);
            Assert.True(operation.Cancelled);
            Assert.Null(operation.Exception);
        }

        [Fact]
        public static void Throw()
        {
            // Test that exceptions get passed all the way through PostOperationCompleted(callback, AsyncCompletedEventArgs)
            var operation = new TestAsyncOperation(op =>
            {
                throw new TestException("Test throw");
            });
            
            Assert.Throws<TestException>(() => operation.Wait());
        }

        [Fact]
        public static void PostNullDelegate()
        {
            // the xUnit SynchronizationContext - AysncTestSyncContext interferes with the current SynchronizationContext
            // used by AsyncOperation when there is exception thrown -> the SC.OperationCompleted() is not called.
            // use  new SC here to avoid this issue
            var orignal = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

            // Pass a non-null state just to emphasize we're only testing passing a null delegate
            var state = new object();
            var operation = AsyncOperationManager.CreateOperation(state);
            Assert.Throws<ArgumentNullException>(() => operation.Post(null, state));
            Assert.Throws<ArgumentNullException>(() => operation.PostOperationCompleted(null, state));

            SynchronizationContext.SetSynchronizationContext(orignal);
        }

        // A simple wrapper for AsyncOperation which executes the specified delegate and a completion handler asynchronously.
        public class TestAsyncOperation
        {
            private object operationId;
            private Action<TestAsyncOperation> executeDelegate;

            public AsyncOperation AsyncOperation { get; private set; }

            public bool Completed { get; private set; }

            public bool Cancelled { get; private set; }

            public Exception Exception { get; private set; }

            public TestAsyncOperation(Action<TestAsyncOperation> executeDelegate)
            {
                // Create an async operation passing an object as the state so we can
                // verify that state is passed properly.
                this.operationId = new object();
                this.AsyncOperation = AsyncOperationManager.CreateOperation(this.operationId);

                Assert.Equal(this.AsyncOperation.SynchronizationContext, AsyncOperationManager.SynchronizationContext);

                // Post work to the wrapped synchronization context
                this.executeDelegate = executeDelegate;
                this.AsyncOperation.Post((SendOrPostCallback)ExecuteWorker, operationId);
            }

            public void Wait()
            {
                var startTime = DateTime.Now;
                var timeoutTime = startTime.AddSeconds(k_spinTimeoutSeconds);
            
                while (!this.Completed)
                {
                    if (DateTime.Now > timeoutTime)
                    {
                        throw new TestTimeoutException();
                    }
                }

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
                Assert.True(this.operationId == operationId, "AsyncOperationManager did not pass UserSuppliedState through to the operation.");

                Exception exception = null;
                
                try
                {
                    this.executeDelegate(this);
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
                if (!this.Completed)
                {
                    this.AsyncOperation.PostOperationCompleted(
                        (SendOrPostCallback)OnOperationCompleted,
                        new AsyncCompletedEventArgs(
                            exception,
                            cancelled,
                            this.operationId));
                }
            }

            private void OnOperationCompleted(object state)
            {
                var e = state as AsyncCompletedEventArgs;

                Assert.True(e != null, "The state passed to this operation must be of type AsyncCompletedEventArgs");
                Assert.Equal(this.operationId, e.UserState);

                this.Completed = true;
                this.Cancelled = e.Cancelled;
                this.Exception = e.Error;
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