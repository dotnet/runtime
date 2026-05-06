// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Xunit;

public delegate void MyCallback();

public class ForeignUnhandled
{
    [DllImport("ForeignUnhandledNative")]
    public static extern void InvokeCallbackOnNewThread(MyCallback callback);

    [ThreadStatic]
    private static Exception lastEx;
    private static bool expectUnhandledException = false;

    private static bool Handler(Exception ex)
    {
        lastEx = ex;
        return true;
    }

    private static void SetHandler()
    {
        System.Runtime.ExceptionServices.ExceptionHandling.SetUnhandledExceptionHandler(Handler);
    }

    // test-wide setup
    static ForeignUnhandled()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, _) =>
        {
            if (expectUnhandledException &&
                lastEx == null)
            {
                Environment.Exit(100);
            }
        };

        SetHandler();
    }

    public static void RunTest()
    {
        // sanity check, the handler should be working in a separate thread
        Thread th = new Thread(() =>
        {
            try
            {
                lastEx = null;
                throw new Exception("here is an unhandled exception1");
                Assert.Fail();
            }
            finally
            {
                Assert.Equal("here is an unhandled exception1", lastEx.Message);
            }
        });

        th.Start();
        th.Join();

        expectUnhandledException = true;
        InvokeCallbackOnNewThread(() => {
            try
            {
                lastEx = null;
                throw new Exception("here is an unhandled exception2");
                Assert.Fail();
            }
            finally
            {
                Assert.Null(lastEx);
            }
        });

        Assert.Fail();
    }

    public static int Main()
    {
        RunTest();

        // should not reach here
        return 42;
    }
}
