// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using System.Threading.Tasks;
using TestLibrary;
using Xunit;

public class HandlerRefuses
{
    [ThreadStatic]
    private static Exception lastEx;

    private static bool shouldReturnFalseFromFilter = false;
    private static bool expectUnhandledException = false;

    private static bool Handler(Exception ex)
    {
        if (shouldReturnFalseFromFilter)
        {
            return false;
        }

        lastEx = ex;
        return true;
    }

    private static void SetHandler()
    {
        System.Runtime.ExceptionServices.ExceptionHandling.SetUnhandledExceptionHandler(Handler);
    }

    // test-wide setup
    static HandlerRefuses()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, _) =>
        {
            if (expectUnhandledException)
            {
                Environment.Exit(100);
            }
        };

        SetHandler();
    }

    [Fact]
    public static void Test1()
    {
        shouldReturnFalseFromFilter = false;
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

        shouldReturnFalseFromFilter = true;
        expectUnhandledException = true;
        th = new Thread(() =>
        {
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

        th.Start();
        th.Join();
    }
}
