// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable 1998 // Async method with no "await" operators.

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
}
