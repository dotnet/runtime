// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

// Disable warnings for ControlledExecution.Run
#pragma warning disable SYSLIB0046

namespace System.Runtime.Tests
{
    public sealed class ControlledExecutionTests
    {
        private volatile bool _readyForCancellation;
        private bool _caughtException, _finishedExecution;
        private Exception _exception;
        private volatile int _counter;

        // Tests that the Run method finishes normally if no cancellation is requested
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMonoRuntime), nameof(PlatformDetection.IsNotNativeAot))]
        public void RunWithoutCancelling()
        {
            var cts = new CancellationTokenSource();
            RunTest(Test, cts.Token);

            Assert.True(_finishedExecution);
            Assert.Null(_exception);

            void Test()
            {
                _finishedExecution = true;
            }
        }

        // Tests that a nested invocation of the Run method throws an InvalidOperationException
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMonoRuntime), nameof(PlatformDetection.IsNotNativeAot))]
        public void TestNestedRunInvocation()
        {
            bool nestedExecution = false;

            var cts = new CancellationTokenSource();
            RunTest(Test, cts.Token);

            Assert.False(nestedExecution);
            Assert.IsType<InvalidOperationException>(_exception);

            void Test()
            {
                ControlledExecution.Run(() => nestedExecution = true, cts.Token);
            }
        }

        // Tests that an infinite loop may be aborted and that the ThreadAbortException is translated
        // to an OperationCanceledException.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMonoRuntime), nameof(PlatformDetection.IsNotNativeAot))]
        public void CancelOutsideOfTryCatchFinally()
        {
            var cts = new CancellationTokenSource();
            Task.Run(() => CancelWhenTestIsReady(cts));
            RunTest(Test, cts.Token);

            Assert.False(_finishedExecution);
            Assert.IsType<OperationCanceledException>(_exception);

            void Test()
            {
                _readyForCancellation = true;
                RunInfiniteLoop();
                _finishedExecution = true;
            }
        }

        // Tests that an infinite loop may be aborted, that the ThreadAbortException is automatically rethrown,
        // and that it is eventually translated to an OperationCanceledException.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMonoRuntime), nameof(PlatformDetection.IsNotNativeAot))]
        public void CancelInTryAndExitCatchNormally()
        {
            var cts = new CancellationTokenSource();
            Task.Run(() => CancelWhenTestIsReady(cts));
            RunTest(Test, cts.Token);

            Assert.True(_caughtException);
            Assert.False(_finishedExecution);
            Assert.IsType<OperationCanceledException>(_exception);

            void Test()
            {
                try
                {
                    _readyForCancellation = true;
                    RunInfiniteLoop();
                }
                catch
                {
                    // Swallow all exceptions to verify that the ThreadAbortException is automatically rethrown
                    _caughtException = true;
                }

                _finishedExecution = true;
            }
        }

        // Tests that catch blocks are not aborted. The catch block swallows the ThreadAbortException and throws a different exception.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMonoRuntime), nameof(PlatformDetection.IsNotNativeAot))]
        public void CancelInTryAndThrowFromCatch()
        {
            var cts = new CancellationTokenSource();
            Task.Run(() => CancelWhenTestIsReady(cts));
            RunTest(Test, cts.Token);

            Assert.True(_caughtException);
            Assert.IsType<TestException>(_exception);

            void Test()
            {
                try
                {
                    _readyForCancellation = true;
                    RunInfiniteLoop();
                }
                catch
                {
                    _caughtException = true;
                    // The catch block must not be aborted
                    Thread.Sleep(200);
                    throw new TestException();
                }
            }
        }

        // Tests that finally blocks are not aborted. The finally block exits normally.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMonoRuntime), nameof(PlatformDetection.IsNotNativeAot))]
        public void CancelInFinallyThatSleeps()
        {
            var cts = new CancellationTokenSource();
            Task.Run(() => CancelWhenTestIsReady(cts));
            RunTest(Test, cts.Token);

            Assert.True(_finishedExecution);
            Assert.IsType<TestException>(_exception);

            void Test()
            {
                try
                {
                    // Make sure to run the non-inlined finally
                    throw new TestException();
                }
                finally
                {
                    _readyForCancellation = true;
                    WaitUntilAbortIsRequested();
                    // The finally block must not be aborted
                    Thread.Sleep(200);
                    _finishedExecution = true;
                }
            }
        }

        // Tests that finally blocks are not aborted. The finally block throws an exception.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMonoRuntime), nameof(PlatformDetection.IsNotNativeAot))]
        public void CancelInFinallyThatSleepsAndThrows()
        {
            var cts = new CancellationTokenSource();
            Task.Run(() => CancelWhenTestIsReady(cts));
            RunTest(Test, cts.Token);

            Assert.IsType<TestException>(_exception);

            void Test()
            {
                try
                {
                    // Make sure to run the non-inlined finally
                    throw new Exception();
                }
                finally
                {
                    _readyForCancellation = true;
                    WaitUntilAbortIsRequested();
                    // The finally block must not be aborted
                    Thread.Sleep(200);
                    throw new TestException();
                }
            }
        }

        // Tests cancellation before calling the Run method. The action must never start.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMonoRuntime), nameof(PlatformDetection.IsNotNativeAot))]
        public void CancelBeforeRun()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();
            RunTest(Test, cts.Token);

            Assert.False(_finishedExecution);
            Assert.IsType<OperationCanceledException>(_exception);

            void Test()
            {
                _finishedExecution = true;
            }
        }

        // Tests cancellation by the action itself
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMonoRuntime), nameof(PlatformDetection.IsNotNativeAot))]
        public void CancelItselfOutsideOfTryCatchFinally()
        {
            var cts = new CancellationTokenSource();
            RunTest(Test, cts.Token);

            Assert.False(_finishedExecution);
            // CancellationTokenSource.Cancel catches the ThreadAbortException; however, it is rethrown at the end
            // of the catch block.
            Assert.IsType<OperationCanceledException>(_exception);

            void Test()
            {
                cts.Cancel();
                _finishedExecution = true;
            }
        }

        // Tests cancellation by the action itself. Finally blocks must be executed except the one that triggered cancellation.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMonoRuntime), nameof(PlatformDetection.IsNotNativeAot))]
        public void CancelItselfFromFinally()
        {
            bool finishedContainingFinally = false;

            var cts = new CancellationTokenSource();
            RunTest(Test, cts.Token);

            Assert.False(finishedContainingFinally);
            Assert.True(_finishedExecution);
            // CancellationTokenSource.Cancel catches the ThreadAbortException and wraps it into an AggregateException
            // at the end of the method's execution. The ThreadAbortException is not rethrown at the end of the catch
            // block, because the Cancel method is called from a finally block.
            Assert.IsType<AggregateException>(_exception);
            Assert.IsType<ThreadAbortException>(_exception.InnerException);

            void Test()
            {
                try
                {
                    try
                    {
                        // Make sure to run the non-inlined finally
                        throw new Exception();
                    }
                    finally
                    {
                        // When cancelling itself, the containing finally block must be aborted
                        cts.Cancel();
                        finishedContainingFinally = true;
                    }
                }
                finally
                {
                    _finishedExecution = true;
                }
            }
        }

        private void RunTest(Action action, CancellationToken cancellationToken)
        {
            _readyForCancellation = _caughtException = _finishedExecution = false;
            _exception = null;
            _counter = 0;

            try
            {
                ControlledExecution.Run(action, cancellationToken);
            }
            catch (Exception e)
            {
                _exception = e;
            }
        }

        private void CancelWhenTestIsReady(CancellationTokenSource cancellationTokenSource)
        {
            // Wait until the execution is ready to be canceled
            while (!_readyForCancellation)
            {
                Thread.Sleep(10);
            }
            cancellationTokenSource.Cancel();
        }

        private static void WaitUntilAbortIsRequested()
        {
            while ((Thread.CurrentThread.ThreadState & ThreadState.AbortRequested) == 0)
            {
                Thread.Sleep(10);
            }
        }

        private void RunInfiniteLoop()
        {
            while (true)
            {
                if ((++_counter & 0xfffff) == 0)
                {
                    Thread.Sleep(0);
                }
            }
        }

        private sealed class TestException : Exception
        {
        }
    }
}
