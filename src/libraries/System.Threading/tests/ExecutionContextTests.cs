// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Xunit;

namespace System.Threading.Tests
{
    public static class ExecutionContextTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void CreateCopyTest()
        {
            ThreadTestHelpers.RunTestInBackgroundThread(() =>
            {
                var asyncLocal = new AsyncLocal<int>();
                ExecutionContext executionContext = ExecutionContext.Capture();
                VerifyExecutionContext(executionContext, asyncLocal, 0);
                executionContext = ExecutionContext.Capture();
                ExecutionContext executionContextCopy0 = executionContext.CreateCopy();
                asyncLocal.Value = 1;
                executionContext = ExecutionContext.Capture();
                VerifyExecutionContext(executionContext, asyncLocal, 1);
                VerifyExecutionContext(executionContextCopy0, asyncLocal, 0);
                executionContext = ExecutionContext.Capture();
                ExecutionContext executionContextCopy1 = executionContext.CreateCopy();
                VerifyExecutionContext(executionContextCopy1, asyncLocal, 1);
            });
        }

        [Fact]
        public static void RestoreTest()
        {
            ExecutionContext defaultEC = ExecutionContext.Capture();
            var asyncLocal = new AsyncLocal<int>();
            Assert.Equal(0, asyncLocal.Value);

            asyncLocal.Value = 1;
            ExecutionContext oneEC = ExecutionContext.Capture();
            Assert.Equal(1, asyncLocal.Value);

            ExecutionContext.Restore(defaultEC);
            Assert.Equal(0, asyncLocal.Value);

            ExecutionContext.Restore(oneEC);
            Assert.Equal(1, asyncLocal.Value);

            ExecutionContext.Restore(defaultEC);
            Assert.Equal(0, asyncLocal.Value);

            Assert.Throws<InvalidOperationException>(() => ExecutionContext.Restore(null!));
        }

        [Fact]
        public static void DisposeTest()
        {
            ExecutionContext executionContext = ExecutionContext.Capture();
            executionContext.CreateCopy().Dispose();
            executionContext.CreateCopy().Dispose();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49890", TestPlatforms.Android)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51400", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public static void FlowTest()
        {
            ThreadTestHelpers.RunTestInBackgroundThread(() =>
            {
                var asyncLocal = new AsyncLocal<int>();
                asyncLocal.Value = 1;

                var asyncFlowControl = default(AsyncFlowControl);
                Action<Action, Action> verifySuppressRestore =
                    (suppressFlow, restoreFlow) =>
                    {
                        VerifyExecutionContextFlow(asyncLocal, 1);
                        ExecutionContext executionContext2 = ExecutionContext.Capture();

                        suppressFlow();
                        VerifyExecutionContextFlow(asyncLocal, 0);
                        VerifyExecutionContext(executionContext2, asyncLocal, 1);
                        executionContext2 = ExecutionContext.Capture();

                        restoreFlow();
                        VerifyExecutionContextFlow(asyncLocal, 1);
                        VerifyExecutionContext(executionContext2, asyncLocal, 0);
                    };

                verifySuppressRestore(
                    () => asyncFlowControl = ExecutionContext.SuppressFlow(),
                    () => asyncFlowControl.Undo());
                verifySuppressRestore(
                    () => asyncFlowControl = ExecutionContext.SuppressFlow(),
                    () => asyncFlowControl.Dispose());
                verifySuppressRestore(
                    () => ExecutionContext.SuppressFlow(),
                    () => ExecutionContext.RestoreFlow());

                Assert.Throws<InvalidOperationException>(() => ExecutionContext.RestoreFlow());
                asyncFlowControl = ExecutionContext.SuppressFlow();
                Assert.Throws<InvalidOperationException>(() => ExecutionContext.SuppressFlow());

                ThreadTestHelpers.RunTestInBackgroundThread(() =>
                {
                    ExecutionContext.SuppressFlow();
                    Assert.Throws<InvalidOperationException>(() => asyncFlowControl.Undo());
                    Assert.Throws<InvalidOperationException>(() => asyncFlowControl.Dispose());
                    ExecutionContext.RestoreFlow();
                });

                asyncFlowControl.Undo();
                Assert.Throws<InvalidOperationException>(() => asyncFlowControl.Undo());
                Assert.Throws<InvalidOperationException>(() => asyncFlowControl.Dispose());

                // Changing an async local value does not prevent undoing a flow-suppressed execution context. In .NET Core, the
                // execution context is immutable, so changing an async local value changes the execution context instance,
                // contrary to the .NET Framework.
                asyncFlowControl = ExecutionContext.SuppressFlow();
                asyncLocal.Value = 2;
                asyncFlowControl.Undo();
                VerifyExecutionContextFlow(asyncLocal, 2);
                asyncFlowControl = ExecutionContext.SuppressFlow();
                asyncLocal.Value = 3;
                asyncFlowControl.Dispose();
                VerifyExecutionContextFlow(asyncLocal, 3);
                ExecutionContext.SuppressFlow();
                asyncLocal.Value = 4;
                ExecutionContext.RestoreFlow();
                VerifyExecutionContextFlow(asyncLocal, 4);

                // An async flow control cannot be undone when a different execution context is applied. The .NET Framework
                // mutates the execution context when its state changes, and only changes the instance when an execution context
                // is applied (for instance, through ExecutionContext.Run). The framework prevents a suppressed-flow execution
                // context from being applied by returning null from ExecutionContext.Capture, so the only type of execution
                // context that can be applied is one whose flow is not suppressed. After suppressing flow and changing an async
                // local's value, the .NET Framework verifies that a different execution context has not been applied by
                // checking the execution context instance against the one saved from when flow was suppressed. In .NET Core,
                // since the execution context instance will change after changing the async local's value, it verifies that a
                // different execution context has not been applied, by instead ensuring that the current execution context's
                // flow is suppressed.
                {
                    ExecutionContext executionContext = null;
                    Action verifyCannotUndoAsyncFlowControlAfterChangingExecutionContext =
                        () =>
                        {
                            ExecutionContext.Run(
                                executionContext,
                                state =>
                                {
                                    Assert.Throws<InvalidOperationException>(() => asyncFlowControl.Undo());
                                    Assert.Throws<InvalidOperationException>(() => asyncFlowControl.Dispose());
                                },
                                null);
                        };

                    executionContext = ExecutionContext.Capture();
                    asyncFlowControl = ExecutionContext.SuppressFlow();
                    verifyCannotUndoAsyncFlowControlAfterChangingExecutionContext();
                    asyncFlowControl.Undo();

                    executionContext = ExecutionContext.Capture();
                    asyncFlowControl = ExecutionContext.SuppressFlow();
                    asyncLocal.Value = 5;
                    verifyCannotUndoAsyncFlowControlAfterChangingExecutionContext();
                    asyncFlowControl.Undo();
                    VerifyExecutionContextFlow(asyncLocal, 5);
                }
            });
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51400", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public static void CaptureThenSuppressThenRunFlowTest()
        {
            ThreadTestHelpers.RunTestInBackgroundThread(() =>
            {
                var asyncLocal = new AsyncLocal<int>();
                asyncLocal.Value = 1;

                ExecutionContext executionContext = ExecutionContext.Capture();
                ExecutionContext.SuppressFlow();
                ExecutionContext.Run(
                    executionContext,
                    state =>
                    {
                        Assert.Equal(1, asyncLocal.Value);
                        VerifyExecutionContextFlow(asyncLocal, 1);
                    },
                    null);
                Assert.Equal(1, asyncLocal.Value);
                VerifyExecutionContextFlow(asyncLocal, 0);
                ExecutionContext.RestoreFlow();
                VerifyExecutionContextFlow(asyncLocal, 1);

                executionContext = ExecutionContext.Capture();
                asyncLocal.Value = 2;
                ExecutionContext.SuppressFlow();
                Assert.True(ExecutionContext.IsFlowSuppressed());
                ExecutionContext.Run(
                    executionContext,
                    state =>
                    {
                        Assert.Equal(1, asyncLocal.Value);
                        VerifyExecutionContextFlow(asyncLocal, 1);
                    },
                    null);
                Assert.Equal(2, asyncLocal.Value);
                VerifyExecutionContextFlow(asyncLocal, 0);
                ExecutionContext.RestoreFlow();
                VerifyExecutionContextFlow(asyncLocal, 2);
            });
        }

        private static void VerifyExecutionContext(
            ExecutionContext executionContext,
            AsyncLocal<int> asyncLocal,
            int expectedValue)
        {
            int actualValue = 0;
            Action run = () => ExecutionContext.Run(executionContext, state => actualValue = asyncLocal.Value, null);
            if (executionContext == null)
            {
                Assert.Throws<InvalidOperationException>(() => run());
            }
            else
            {
                run();
            }
            Assert.Equal(expectedValue, actualValue);
        }

        private static void VerifyExecutionContextFlow(AsyncLocal<int> asyncLocal, int expectedValue)
        {
            Assert.Equal(expectedValue == 0, ExecutionContext.IsFlowSuppressed());
            if (ExecutionContext.IsFlowSuppressed())
            {
                Assert.Null(ExecutionContext.Capture());
            }
            VerifyExecutionContext(ExecutionContext.Capture(), asyncLocal, expectedValue);

            // Creating a thread flows context if and only if flow is not suppressed
            int asyncLocalValue = -1;
            ThreadTestHelpers.RunTestInBackgroundThread(() => asyncLocalValue = asyncLocal.Value);
            Assert.Equal(expectedValue, asyncLocalValue);

            // Queueing a thread pool work item flows context if and only if flow is not suppressed
            asyncLocalValue = -1;
            var done = new AutoResetEvent(false);
            ThreadPool.QueueUserWorkItem(state =>
            {
                asyncLocalValue = asyncLocal.Value;
                done.Set();
            });
            done.CheckedWait();
            Assert.Equal(expectedValue, asyncLocalValue);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void AsyncFlowControlTest()
        {
            ThreadTestHelpers.RunTestInBackgroundThread(() =>
            {
                Action<AsyncFlowControl, AsyncFlowControl, bool> verifyEquality =
                    (afc0, afc1, areExpectedToBeEqual) =>
                    {
                        Assert.Equal(areExpectedToBeEqual, afc0.Equals(afc1));
                        Assert.Equal(areExpectedToBeEqual, afc0.Equals((object)afc1));
                        Assert.Equal(areExpectedToBeEqual, afc0 == afc1);
                        Assert.NotEqual(areExpectedToBeEqual, afc0 != afc1);
                    };

                AsyncFlowControl asyncFlowControl0 = ExecutionContext.SuppressFlow();
                ExecutionContext.RestoreFlow();
                AsyncFlowControl asyncFlowControl1 = ExecutionContext.SuppressFlow();
                ExecutionContext.RestoreFlow();
                verifyEquality(asyncFlowControl0, asyncFlowControl1, true);
                verifyEquality(asyncFlowControl1, asyncFlowControl0, true);

                var asyncLocal = new AsyncLocal<int>();
                asyncLocal.Value = 1;
                asyncFlowControl1 = ExecutionContext.SuppressFlow();
                ExecutionContext.RestoreFlow();
                verifyEquality(asyncFlowControl0, asyncFlowControl1, true);
                verifyEquality(asyncFlowControl1, asyncFlowControl0, true);

                asyncFlowControl1 = new AsyncFlowControl();
                verifyEquality(asyncFlowControl0, asyncFlowControl1, false);
                verifyEquality(asyncFlowControl1, asyncFlowControl0, false);

                ThreadTestHelpers.RunTestInBackgroundThread(() => asyncFlowControl1 = ExecutionContext.SuppressFlow());
                verifyEquality(asyncFlowControl0, asyncFlowControl1, false);
                verifyEquality(asyncFlowControl1, asyncFlowControl0, false);
            });
        }
    }
}
