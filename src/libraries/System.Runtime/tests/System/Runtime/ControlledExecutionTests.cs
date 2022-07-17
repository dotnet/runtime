// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Xunit;

// Disable warnings for ControlledExecution.Run
#pragma warning disable SYSLIB0046

namespace System.Runtime.Tests
{
    public class ControlledExecutionTests
    {
        private bool _startedExecution, _caughtException, _finishedExecution;
        private Exception _exception;
        private volatile int _counter;

        // Tests cancellation on timeout. The ThreadAbortException must be mapped to OperationCanceledException.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetFramework), nameof(PlatformDetection.IsNotMonoRuntime), nameof(PlatformDetection.IsNotNativeAot))]
        public void CancelOnTimeout()
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(200);
            RunTest(LengthyAction, cts.Token);

            Assert.True(_startedExecution);
            Assert.True(_caughtException);
            Assert.False(_finishedExecution);
            Assert.IsType<OperationCanceledException>(_exception);
        }

        // Tests that catch blocks are not aborted. The action catches the ThreadAbortException and throws an exception of a different type.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetFramework), nameof(PlatformDetection.IsNotMonoRuntime), nameof(PlatformDetection.IsNotNativeAot))]
        public void CancelOnTimeout_ThrowFromCatch()
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(200);
            RunTest(LengthyAction_ThrowFromCatch, cts.Token);

            Assert.True(_startedExecution);
            Assert.True(_caughtException);
            Assert.False(_finishedExecution);
            Assert.IsType<TimeoutException>(_exception);
        }

        // Tests that finally blocks are not aborted. The action catches the ThreadAbortException and throws an exception of a different type.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetFramework), nameof(PlatformDetection.IsNotMonoRuntime), nameof(PlatformDetection.IsNotNativeAot))]
        public void CancelOnTimeout_ThrowFromFinally()
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(200);
            RunTest(LengthyAction_ThrowFromFinally, cts.Token);

            Assert.True(_startedExecution);
            Assert.IsType<TimeoutException>(_exception);
        }

        // Tests that finally blocks are not aborted. The action throws an exception of a different type.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetFramework), nameof(PlatformDetection.IsNotMonoRuntime), nameof(PlatformDetection.IsNotNativeAot))]
        public void CancelOnTimeout_Finally()
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(200);
            RunTest(LengthyAction_Finally, cts.Token);

            Assert.True(_startedExecution);
            Assert.True(_finishedExecution);
            Assert.IsType<TimeoutException>(_exception);
        }

        // Tests cancellation before calling the Run method
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetFramework), nameof(PlatformDetection.IsNotMonoRuntime), nameof(PlatformDetection.IsNotNativeAot))]
        public void CancelBeforeRun()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();
            Thread.Sleep(100);
            RunTest(LengthyAction, cts.Token);

            Assert.False(_startedExecution);
            Assert.Null(_exception);
        }

        private void RunTest(Action action, CancellationToken cancellationToken)
        {
            _startedExecution = _caughtException = _finishedExecution = false;
            _exception = null;

            try
            {
                ControlledExecution.Run(action, cancellationToken);
            }
            catch (Exception e)
            {
                _exception = e;
            }
        }

        private void LengthyAction()
        {
            _startedExecution = true;

            try
            {
                for (_counter = 0; _counter < int.MaxValue; _counter++) { }
            }
            catch
            {
                // Swallow all exceptions to verify that the exception is automatically rethrown
                _caughtException = true;
            }

            _finishedExecution = true;
        }

        private void LengthyAction_ThrowFromCatch()
        {
            _startedExecution = true;

            try
            {
                for (_counter = 0; _counter < int.MaxValue; _counter++) { }
            }
            catch
            {
                _caughtException = true;
                // Catch blocks must not be aborted
                Thread.Sleep(100);
                throw new TimeoutException();
            }

            _finishedExecution = true;
        }

        private void LengthyAction_ThrowFromFinally()
        {
            _startedExecution = true;

            try
            {
                // Make sure to run the non-inlined finally
                throw new Exception();
            }
            finally
            {
                // Finally blocks must not be aborted
                Thread.Sleep(400);
                throw new TimeoutException();
            }
        }

        private void LengthyAction_Finally()
        {
            _startedExecution = true;

            try
            {
                // Make sure to run the non-inlined finally
                throw new TimeoutException();
            }
            finally
            {
                // Finally blocks must not be aborted
                Thread.Sleep(400);
                _finishedExecution = true;
            }

        }
    }
}
