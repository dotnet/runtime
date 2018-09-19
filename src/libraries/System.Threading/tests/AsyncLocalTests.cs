// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

#pragma warning disable 1998 // Async method with no "await" operators.

namespace System.Threading.Tests
{
    public static class AsyncLocalTests
    {
        [Fact]
        public static async Task ValueProperty()
        {
            AsyncLocal<int> local = new AsyncLocal<int>();
            Assert.Equal(local.Value, 0);

            local.Value = 1;
            Assert.Equal(local.Value, 1);

            local.Value = 0;
            Assert.Equal(local.Value, 0);
        }

        [Fact]
        public static async Task CaptureAndRestore()
        {
            AsyncLocal<int> local = new AsyncLocal<int>();

            local.Value = 42;

            ExecutionContext ec = ExecutionContext.Capture();

            local.Value = 12;

            ExecutionContext.Run(
                ec,
                _ =>
                {
                    Assert.Equal(local.Value, 42);
                    local.Value = 56;
                },
                null);

            Assert.Equal(local.Value, 12);
        }

        [Fact]
        public static async Task CaptureAndRestoreEmptyContext()
        {
            AsyncLocal<int> local = new AsyncLocal<int>();

            ExecutionContext ec = ExecutionContext.Capture();

            local.Value = 12;

            ExecutionContext.Run(
                ec,
                _ =>
                {
                    Assert.Equal(local.Value, 0);
                    local.Value = 56;
                },
                null);

            Assert.Equal(local.Value, 12);
        }

        [Theory]
        [MemberData(nameof(GetCounts))]
        public static async Task CaptureAndRestoreNullAsyncLocals(int count)
        {
            AsyncLocal<object>[] locals = new AsyncLocal<object>[count];
            for (var i = 0; i < locals.Length; i++)
            {
                locals[i] = new AsyncLocal<object>();
            }

            ExecutionContext ec = ExecutionContext.Capture();

            ExecutionContext.Run(
                ec,
                _ =>
                {
                    for (var i = 0; i < locals.Length; i++)
                    {
                        AsyncLocal<object> local = locals[i];

                        Assert.Null(local.Value);
                        local.Value = 56;
                        Assert.IsType<int>(local.Value);
                        Assert.Equal(56, (int)local.Value);
                    }
                },
                null);

            for (var i = 0; i < locals.Length; i++)
            {
                Assert.Null(locals[i].Value);
            }
        }

        [Fact]
        public static async Task CaptureAndRunOnFlowSupressedContext()
        {
            ExecutionContext.SuppressFlow();
            try
            {
                ExecutionContext ec = ExecutionContext.Capture();
                Assert.Throws<InvalidOperationException>(() => ExecutionContext.Run(ec, _ => { }, null));
            }
            finally
            {
                ExecutionContext.RestoreFlow();
            }
        }

        [Fact]
        public static async Task NotifyOnValuePropertyChange()
        {
            bool expectThreadContextChange = false;
            int expectedPreviousValue = 0;
            int expectedCurrentValue = 1;
            bool gotNotification = false;
            bool expectNotification = false;

            AsyncLocal<int> local = new AsyncLocal<int>(
                args =>
                {
                    gotNotification = true;

                    Assert.True(expectNotification);
                    expectNotification = false;

                    Assert.Equal(args.ThreadContextChanged, expectThreadContextChange);
                    Assert.Equal(args.PreviousValue, expectedPreviousValue);
                    Assert.Equal(args.CurrentValue, expectedCurrentValue);
                });

            expectNotification = true;
            local.Value = 1;

            Assert.True(gotNotification);

            expectNotification = true;
            expectThreadContextChange = true;
            expectedPreviousValue = local.Value;
            expectedCurrentValue = 0;
            return;
        }

        [Fact]
        public static async Task NotifyCountOnObjectValueChange()
        {
            var obj0 = new object();
            var obj1 = new object();
            var obj2 = new object();

            int asyncLocal0ChangeCount = 0;
            int asyncLocal1ChangeCount = 0;
            void VerifyChangeCounts(int expectedAsyncLocal0ChangeCount, int expectedAsyncLocal1ChangeCount)
            {
                Assert.Equal(expectedAsyncLocal0ChangeCount, asyncLocal0ChangeCount);
                Assert.Equal(expectedAsyncLocal1ChangeCount, asyncLocal1ChangeCount);
            }

            Action<AsyncLocalValueChangedArgs<object>> onAsyncLocal0Changed = e =>
            {
                Assert.True(e.PreviousValue == null || e.CurrentValue == null);
                object nonNullValue = e.PreviousValue ?? e.CurrentValue;
                Assert.Same(obj0, nonNullValue);
                ++asyncLocal0ChangeCount;
            };
            VerifyChangeCounts(0, 0);
            var asyncLocal0 = new AsyncLocal<object>(onAsyncLocal0Changed);
            VerifyChangeCounts(0, 0);
            asyncLocal0.Value = obj0;
            VerifyChangeCounts(1, 0);
            var executionContext = ExecutionContext.Capture();

            Action<AsyncLocalValueChangedArgs<object>> onAsyncLocal1Changed = e =>
            {
                Assert.True(e.PreviousValue == null || e.CurrentValue == null);
                object nonNullValue = e.PreviousValue ?? e.CurrentValue;
                Assert.True(nonNullValue == obj1 || nonNullValue == obj2);
                ++asyncLocal1ChangeCount;
            };
            VerifyChangeCounts(1, 0);
            var asyncLocal1 = new AsyncLocal<object>(onAsyncLocal1Changed);
            VerifyChangeCounts(1, 0);
            asyncLocal1.Value = obj1;
            VerifyChangeCounts(1, 1);
            asyncLocal1.Value = null;
            VerifyChangeCounts(1, 2);
            asyncLocal1.Value = obj2;
            VerifyChangeCounts(1, 3);

            ExecutionContext.Run(executionContext, data => VerifyChangeCounts(1, 4), null);
            VerifyChangeCounts(1, 5);
        }

        [Fact]
        public static async Task NotifyOnThreadContextChange()
        {
            bool expectThreadContextChange = false;
            int expectedPreviousValue = 0;
            int expectedCurrentValue = 1;
            bool gotNotification = false;
            bool expectNotification = false;

            AsyncLocal<int> local = new AsyncLocal<int>(
                args =>
                {
                    gotNotification = true;

                    Assert.True(expectNotification);
                    expectNotification = false;

                    Assert.Equal(args.ThreadContextChanged, expectThreadContextChange);
                    Assert.Equal(args.PreviousValue, expectedPreviousValue);
                    Assert.Equal(args.CurrentValue, expectedCurrentValue);
                });

            expectNotification = true;
            local.Value = 1;
            Assert.True(gotNotification);
            gotNotification = false;

            ExecutionContext ec = ExecutionContext.Capture();

            expectNotification = true;
            expectedPreviousValue = 1;
            expectedCurrentValue = 2;
            local.Value = 2;
            Assert.True(gotNotification);
            gotNotification = false;

            expectNotification = true;
            expectedPreviousValue = 2;
            expectedCurrentValue = 1;
            expectThreadContextChange = true;

            ExecutionContext.Run(
                ec,
                _ =>
                {
                    Assert.True(gotNotification);
                    gotNotification = false;

                    Assert.Equal(local.Value, 1);

                    expectNotification = true;
                    expectedPreviousValue = 1;
                    expectedCurrentValue = 3;
                    expectThreadContextChange = false;
                    local.Value = 3;
                    Assert.True(gotNotification);
                    gotNotification = false;

                    expectNotification = true;
                    expectedPreviousValue = 3;
                    expectedCurrentValue = 2;
                    expectThreadContextChange = true;
                    return;
                },
                null);

            Assert.True(gotNotification);
            gotNotification = false;

            Assert.Equal(local.Value, 2);

            expectNotification = true;
            expectThreadContextChange = true;
            expectedPreviousValue = local.Value;
            expectedCurrentValue = 0;
            return;
        }

        [Fact]
        public static async Task NotifyOnThreadContextChangeWithOneEmptyContext()
        {
            bool expectThreadContextChange = false;
            int expectedPreviousValue = 0;
            int expectedCurrentValue = 1;
            bool gotNotification = false;
            bool expectNotification = false;

            AsyncLocal<int> local = new AsyncLocal<int>(
                args =>
                {
                    gotNotification = true;

                    Assert.True(expectNotification);
                    expectNotification = false;

                    Assert.Equal(args.ThreadContextChanged, expectThreadContextChange);
                    Assert.Equal(args.PreviousValue, expectedPreviousValue);
                    Assert.Equal(args.CurrentValue, expectedCurrentValue);
                });

            ExecutionContext ec = ExecutionContext.Capture();

            expectNotification = true;
            expectedPreviousValue = 0;
            expectedCurrentValue = 1;
            local.Value = 1;
            Assert.True(gotNotification);
            gotNotification = false;

            expectNotification = true;
            expectedPreviousValue = 1;
            expectedCurrentValue = 0;
            expectThreadContextChange = true;

            ExecutionContext.Run(
                ec,
                _ =>
                {
                    Assert.True(gotNotification);
                    gotNotification = false;

                    Assert.Equal(local.Value, 0);

                    expectNotification = true;
                    expectedPreviousValue = 0;
                    expectedCurrentValue = 1;
                    expectThreadContextChange = true;
                    return;
                },
                null);

            Assert.True(gotNotification);
            gotNotification = false;

            Assert.Equal(local.Value, 1);

            expectNotification = true;
            expectThreadContextChange = true;
            expectedPreviousValue = local.Value;
            expectedCurrentValue = 0;
            return;
        }

        // helper to make it easy to start an anonymous async method on the current thread.
        private static Task Run(Func<Task> func)
        {
            return func();
        }

        [Fact]
        public static async Task AsyncMethodNotifications()
        {
            //
            // Define thread-local and async-local values.  The async-local value uses its notification
            // to keep the thread-local value in sync with the async-local value.
            //
            ThreadLocal<int> tls = new ThreadLocal<int>();
            AsyncLocal<int> als = new AsyncLocal<int>(args =>
            {
                tls.Value = args.CurrentValue;
            });

            Assert.Equal(tls.Value, als.Value);

            als.Value = 1;
            Assert.Equal(tls.Value, als.Value);

            als.Value = 2;
            Assert.Equal(tls.Value, als.Value);

            await Run(async () =>
            {
                Assert.Equal(tls.Value, als.Value);
                Assert.Equal(als.Value, 2);

                als.Value = 3;
                Assert.Equal(tls.Value, als.Value);

                Task t = Run(async () =>
                {
                    Assert.Equal(tls.Value, als.Value);
                    Assert.Equal(als.Value, 3);

                    als.Value = 4;

                    Assert.Equal(tls.Value, als.Value);
                    Assert.Equal(als.Value, 4);

                    await Task.Run(() =>
                    {
                        Assert.Equal(tls.Value, als.Value);
                        Assert.Equal(als.Value, 4);

                        als.Value = 5;

                        Assert.Equal(tls.Value, als.Value);
                        Assert.Equal(als.Value, 5);
                    });

                    Assert.Equal(tls.Value, als.Value);
                    Assert.Equal(als.Value, 4);

                    als.Value = 6;

                    Assert.Equal(tls.Value, als.Value);
                    Assert.Equal(als.Value, 6);
                });

                Assert.Equal(tls.Value, als.Value);
                Assert.Equal(als.Value, 3);

                await Task.Yield();

                Assert.Equal(tls.Value, als.Value);
                Assert.Equal(als.Value, 3);

                await t;

                Assert.Equal(tls.Value, als.Value);
                Assert.Equal(als.Value, 3);
            });

            Assert.Equal(tls.Value, als.Value);
            Assert.Equal(als.Value, 2);
        }

        [Fact]
        public static async Task SetValueFromNotification()
        {
            int valueToSet = 0;
            AsyncLocal<int> local = null;
            local = new AsyncLocal<int>(args => { if (args.ThreadContextChanged) local.Value = valueToSet; });

            valueToSet = 2;
            local.Value = 1;
            Assert.Equal(local.Value, 1);

            await Run(async () =>
            {
                local.Value = 3;
                valueToSet = 4;
            });

            Assert.Equal(local.Value, 4);
        }

        [Fact]
        public static async Task ExecutionContextCopyOnWrite()
        {
            AsyncLocal<int> local = new AsyncLocal<int>();

            local.Value = 42;

            await Run(async () =>
                {
                    SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
                    Assert.Equal(42, local.Value);
                    local.Value = 12;
                });

            Assert.Equal(local.Value, 42);
        }

        [Theory]
        [MemberData(nameof(GetCounts))]
        public static async Task AddAndUpdateManyLocals_ValueType(int count)
        {
            var locals = new AsyncLocal<int>[count];
            for (int i = 0; i < locals.Length; i++)
            {
                locals[i] = new AsyncLocal<int>();
                locals[i].Value = i;

                for (int j = 0; j <= i; j++)
                {
                    Assert.Equal(j, locals[j].Value);

                    locals[j].Value = j + 1;
                    Assert.Equal(j + 1, locals[j].Value);

                    locals[j].Value = j;
                    Assert.Equal(j, locals[j].Value);
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetCounts))]
        public static async Task AddUpdateAndRemoveManyLocals_ReferenceType(int count)
        {
            var locals = new AsyncLocal<string>[count];

            for (int i = 0; i < locals.Length; i++)
            {
                locals[i] = new AsyncLocal<string>();
                locals[i].Value = i.ToString();

                for (int j = 0; j <= i; j++)
                {
                    Assert.Equal(j.ToString(), locals[j].Value);

                    locals[j].Value = (j + 1).ToString();
                    Assert.Equal((j + 1).ToString(), locals[j].Value);

                    locals[j].Value = j.ToString();
                    Assert.Equal(j.ToString(), locals[j].Value);
                }
            }

            for (int i = 0; i < locals.Length; i++)
            {
                locals[i].Value = null;
                Assert.Null(locals[i].Value);
                for (int j = i + 1; j < locals.Length; j++)
                {
                    Assert.Equal(j.ToString(), locals[j].Value);
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetCounts))]
        public static async Task AddUpdateAndRemoveManyLocals_ReferenceType_NotifyOnChange(int count)
        {
            string valueChangedLog = string.Empty;
            string expectedValueChangedLog = string.Empty;
            string GetValueChangedLogLine(string previousValue, string currentValue) =>
                $"{previousValue ?? "(null)"} => {currentValue ?? "(null)"}{Environment.NewLine}";
            Action<AsyncLocalValueChangedArgs<string>> valueChangedHandler =
                args => valueChangedLog += GetValueChangedLogLine(args.PreviousValue, args.CurrentValue);
            void VerifyValueChangedLog()
            {
                Assert.Equal(expectedValueChangedLog, valueChangedLog);
                valueChangedLog = string.Empty;
                expectedValueChangedLog = string.Empty;
            }

            var locals = new AsyncLocal<string>[count];

            for (int i = 0; i < locals.Length; i++)
            {
                locals[i] = new AsyncLocal<string>(valueChangedHandler);
                expectedValueChangedLog += GetValueChangedLogLine(locals[i].Value, i.ToString());
                locals[i].Value = i.ToString();
                VerifyValueChangedLog();

                for (int j = 0; j <= i; j++)
                {
                    Assert.Equal(j.ToString(), locals[j].Value);

                    expectedValueChangedLog += GetValueChangedLogLine(locals[j].Value, (j + 1).ToString());
                    locals[j].Value = (j + 1).ToString();
                    Assert.Equal((j + 1).ToString(), locals[j].Value);
                    VerifyValueChangedLog();

                    expectedValueChangedLog += GetValueChangedLogLine(locals[j].Value, j.ToString());
                    locals[j].Value = j.ToString();
                    Assert.Equal(j.ToString(), locals[j].Value);
                    VerifyValueChangedLog();
                }
            }

            for (int i = 0; i < locals.Length; i++)
            {
                expectedValueChangedLog += GetValueChangedLogLine(locals[i].Value, null);
                locals[i].Value = null;
                Assert.Null(locals[i].Value);
                VerifyValueChangedLog();
                for (int j = i + 1; j < locals.Length; j++)
                {
                    Assert.Equal(j.ToString(), locals[j].Value);
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetCounts))]
        public static async Task AsyncLocalsUnwind(int count)
        {
            AsyncLocal<object>[] asyncLocals = new AsyncLocal<object>[count];

            ExecutionContext Default = ExecutionContext.Capture();
            int[] manuallySetCounts = new int[count];
            int[] automaticallyUnsetCounts = new int[count];
            int[] automaticallySetCounts = new int[count];
            ExecutionContext[] capturedContexts = new ExecutionContext[count];

            // Setup the AsyncLocals; capturing ExecutionContext for each level
            await SetLocalsRecursivelyAsync(count - 1);

            ValidateCounts(thresholdIndex: 0, maunalSets: 1, automaticUnsets: 1, automaticSets: 0);
            ValidateAsyncLocalsValuesNull();

            // Check Running with the contexts captured when setting the locals
            TestCapturedExecutionContexts();

            ExecutionContext.SuppressFlow();
            try
            {
                // Re-check restoring, but starting with a suppressed flow
                TestCapturedExecutionContexts();
            }
            finally
            {
                ExecutionContext.RestoreFlow();
            }

            // -- Local functions --
            void ValidateAsyncLocalsValuesNull()
            {
                // Check AsyncLocals haven't leaked
                for (int i = 0; i < asyncLocals.Length; i++)
                {
                    Assert.Null(asyncLocals[i].Value);
                }
            }

            void ValidateAsyncLocalsValues(int thresholdIndex)
            {
                for (int localsIndex = 0; localsIndex < asyncLocals.Length; localsIndex++)
                {
                    if (localsIndex >= thresholdIndex)
                    {
                        Assert.Equal(localsIndex, (int)asyncLocals[localsIndex].Value);
                    }
                    else
                    {
                        Assert.Null(asyncLocals[localsIndex].Value);
                    }
                }
            }

            void TestCapturedExecutionContexts()
            {
                for (int contextIndex = 0; contextIndex < asyncLocals.Length; contextIndex++)
                {
                    ClearCounts();

                    ExecutionContext.Run(
                        capturedContexts[contextIndex].CreateCopy(), 
                        (o) => TestCapturedExecutionContext((int)o), 
                        contextIndex);

                    // Validate locals have been restored to the Default context's values
                    ValidateAsyncLocalsValuesNull();
                }
            }

            void TestCapturedExecutionContext(int contextIndex)
            {
                ValidateCounts(thresholdIndex: contextIndex, maunalSets: 0, automaticUnsets: 0, automaticSets: 1);
                // Validate locals have been restored to the outer context's values
                ValidateAsyncLocalsValues(thresholdIndex: contextIndex);

                // Validate locals are correctly reset Running with a Default context from a non-Default context
                ExecutionContext.Run(
                    Default.CreateCopy(), 
                    _ => ValidateAsyncLocalsValuesNull(), 
                    null);

                ValidateCounts(thresholdIndex: contextIndex, maunalSets: 0, automaticUnsets: 1, automaticSets: 2);
                // Validate locals have been restored to the outer context's values
                ValidateAsyncLocalsValues(thresholdIndex: contextIndex);

                for (int innerContextIndex = 0; innerContextIndex < asyncLocals.Length; innerContextIndex++)
                {
                    // Validate locals are correctly restored Running with another non-Default context from a non-Default context
                    ExecutionContext.Run(
                        capturedContexts[innerContextIndex].CreateCopy(), 
                        o => ValidateAsyncLocalsValues(thresholdIndex: (int)o),
                        innerContextIndex);

                    // Validate locals have been restored to the outer context's values
                    ValidateAsyncLocalsValues(thresholdIndex: contextIndex);
                }
            }

            void ValidateCounts(int thresholdIndex, int maunalSets, int automaticUnsets, int automaticSets)
            {
                for (int localsIndex = 0; localsIndex < asyncLocals.Length; localsIndex++)
                {
                    Assert.Equal(localsIndex < thresholdIndex ? 0 : maunalSets, manuallySetCounts[localsIndex]);
                    Assert.Equal(localsIndex < thresholdIndex ? 0 : automaticUnsets, automaticallyUnsetCounts[localsIndex]);
                    Assert.Equal(localsIndex < thresholdIndex ? 0 : automaticSets, automaticallySetCounts[localsIndex]);
                }
            }

            // Synchronous function is async to create different ExectutionContexts for each set, and check async unwinding
            async Task SetLocalsRecursivelyAsync(int index)
            {
                // Set AsyncLocal
                asyncLocals[index] = new AsyncLocal<object>(CountValueChanges)
                {
                    Value = index
                };

                // Capture context with AsyncLocal set
                capturedContexts[index] = ExecutionContext.Capture();

                if (index > 0)
                {
                    // Go deeper into async stack
                    int nextIndex = index - 1;
                    await SetLocalsRecursivelyAsync(index - 1);
                    // Set is undone by the await
                    Assert.Null(asyncLocals[nextIndex].Value);
                }
            }

            void CountValueChanges(AsyncLocalValueChangedArgs<object> args)
            {
                if (!args.ThreadContextChanged)
                {
                    // Manual create, previous should be null
                    Assert.Null(args.PreviousValue);
                    Assert.IsType<int>(args.CurrentValue);
                    manuallySetCounts[(int)args.CurrentValue]++;
                }
                else
                {
                    // Automatic change, only one value should be not null
                    if (args.CurrentValue != null)
                    {
                        Assert.Null(args.PreviousValue);
                        Assert.IsType<int>(args.CurrentValue);
                        automaticallySetCounts[(int)args.CurrentValue]++;
                    }
                    else
                    {
                        Assert.Null(args.CurrentValue);
                        Assert.NotNull(args.PreviousValue);
                        Assert.IsType<int>(args.PreviousValue);
                        automaticallyUnsetCounts[(int)args.PreviousValue]++;
                    }
                }
            }

            void ClearCounts()
            {
                Array.Clear(manuallySetCounts, 0, count);
                Array.Clear(automaticallyUnsetCounts, 0, count);
                Array.Clear(automaticallySetCounts, 0, count);
            }
        }

        // The data structure that holds AsyncLocals changes based on size;
        // so it needs to be tested at a variety of sizes
        public static IEnumerable<object[]> GetCounts()
            => Enumerable.Range(1, 40).Select(i => new object[] { i });
    }
}
