// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using System.Threading.Tasks;
using TestLibrary;
using Xunit;

public class UnhandledTrivial
{
    [ThreadStatic]
    private static Exception lastEx;

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
    static UnhandledTrivial()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, _) =>
        {
            Console.WriteLine();
            Console.WriteLine("====== EXCEPTION WAS UNHANDLED? WE DO NOT EXPECT ANY TRULY UNHANDLED EXCEPTIONS IN THIS TEST! ======");
            Console.WriteLine();
            Environment.Exit(42);
        };

        SetHandler();
    }

    [Fact]
    public static void SetTwiceFails()
    {
        // on the main thread
        // (exception does not go to the handler)
        try
        {
            Assert.Null(lastEx);
            SetHandler();
            Assert.Fail();
        }
        catch (InvalidOperationException ex)
        {
        }
        finally
        {
            Assert.Null(lastEx);
        }
    }

    [Fact]
    public static void SetNull()
    {
        try
        {
            Assert.Null(lastEx);
            System.Runtime.ExceptionServices.ExceptionHandling.SetUnhandledExceptionHandler(null);
            Assert.Fail();
        }
        catch (ArgumentNullException ex)
        {
        }
        finally
        {
            Assert.Null(lastEx);
        }
    }

    [Fact]
    public static void SetTwiceFailsUserThread()
    {
        // in a user thread
        Thread th = new Thread(() =>
        {
            try
            {
                Assert.Null(lastEx);
                SetHandler();
                Assert.Fail();
            }
            finally
            {
                Assert.IsType<InvalidOperationException>(lastEx);
            }
        });

        th.Start();
        th.Join();
    }

    [Fact]
    public static void SetTwiceFailsTPWorkitem()
    {
        // in a threadpool workitem
        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                lastEx = null;
                SetHandler();
                Assert.Fail();
            }
            finally
            {
                Assert.IsType<InvalidOperationException>(lastEx);
            }
        });
    }

    [Fact]
    public static void SetTwiceFailsInTask()
    {
        // in a task
        try
        {
            Task.Run(() =>
            {
                try
                {
                    lastEx = null;
                    SetHandler();
                    Assert.Fail();
                }
                finally
                {
                    // NB: Exception that leaves a Task turns into a Faulted result.
                    //     At the point of task completion it is not yet known
                    //     if exception is unhandled as it can be "observed" and handled later.
                    Assert.Null(lastEx);
                }
            }).Wait();
        }
        catch (AggregateException ex)
        {
            Assert.IsType<InvalidOperationException>(ex.InnerException);
        }
    }

    class InFinalizer
    {
        ~InFinalizer()
        {
            try
            {
                lastEx = null;
                SetHandler();
                Assert.Fail();
            }
            finally
            {
                Assert.IsType<InvalidOperationException>(lastEx);
            }
        }
    }

    [Fact]
    public static void SetTwiceFailsInFinalizer()
    {
        // in a finalizer
        for (int i = 0; i < 10; i++)
        {
            new InFinalizer();
        }

        for (int i = 0; i < 10; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}
