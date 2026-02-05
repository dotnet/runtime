// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using System.Threading.Tasks;
using TestLibrary;
using Xunit;

public class NoEffectInMainThread
{
    [ThreadStatic]
    private static Exception lastEx;

    private static bool expectUnhandledException = false;
    private static bool finallyHasRun = false;

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
    static NoEffectInMainThread()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, _) =>
        {
            if (expectUnhandledException &&
                lastEx == null &&
                !finallyHasRun)
            {
                Environment.Exit(100);
            }
        };

        SetHandler();
    }

    public static int Main()
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
        try
        {
            lastEx = null;
            throw new Exception("here is an unhandled exception2");
            Assert.Fail();
        }
        finally
        {
            finallyHasRun = true;
        }

        // should not reach here
        return 42;
    }
}
