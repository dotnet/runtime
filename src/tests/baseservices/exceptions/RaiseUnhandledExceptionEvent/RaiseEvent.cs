// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Runtime.ExceptionServices;
using TestLibrary;
using Xunit;

public class RaiseEvent
{
    private ref struct HandlerRegistration
    {
        private readonly UnhandledExceptionEventHandler _handler;
        public HandlerRegistration(UnhandledExceptionEventHandler handler)
        {
            _handler = handler;
            AppDomain.CurrentDomain.UnhandledException += _handler;
        }

        public void Dispose()
        {
            AppDomain.CurrentDomain.UnhandledException -= _handler;
        }
    }

    [ThreadStatic]
    static object? s_ExceptionObject;

    [Fact]
    public static void Validate_ExceptionPassedToHandler()
    {
        Console.WriteLine(nameof(Validate_ExceptionPassedToHandler));

        s_ExceptionObject = null;
        Exception ex = new();
        try
        {
            using HandlerRegistration registration = new(Handler);
            ExceptionHandling.RaiseUnhandledExceptionEvent(ex);
        }
        catch (Exception e)
        {
            Assert.Fail(e.ToString());
        }
        Assert.Equal(ex, s_ExceptionObject);

        static void Handler(object sender, UnhandledExceptionEventArgs args)
        {
            Assert.True(args.IsTerminating);
            s_ExceptionObject = args.ExceptionObject;
        }
    }

    class ShouldBeCaughtException : Exception { }

    [Fact]
    public static void Validate_HandlerThrowingExceptions()
    {
        Console.WriteLine(nameof(Validate_HandlerThrowingExceptions));

        Exception ex = new();
        try
        {
            using HandlerRegistration registration = new(Handler);
            ExceptionHandling.RaiseUnhandledExceptionEvent(ex);
        }
        catch (Exception e)
        {
            Assert.Fail(e.ToString());
        }

        static void Handler(object sender, UnhandledExceptionEventArgs args)
            => throw new ShouldBeCaughtException();
    }

    [ThreadStatic]
    static bool s_UnhandledExceptionHandlerCalled;

    [Fact]
    public static void Validate_UnhandledExceptionHandler_NotCalled()
    {
        Console.WriteLine(nameof(Validate_UnhandledExceptionHandler_NotCalled));

        s_UnhandledExceptionHandlerCalled = false;
        Exception ex = new();
        try
        {
            ExceptionHandling.SetUnhandledExceptionHandler((Exception _) =>
            {
                s_UnhandledExceptionHandlerCalled = true;
                return false;
            });

            using HandlerRegistration registration = new(Handler);
            ExceptionHandling.RaiseUnhandledExceptionEvent(ex);
        }
        catch (Exception e)
        {
            Assert.Fail(e.ToString());
        }
        Assert.False(s_UnhandledExceptionHandlerCalled);

        static void Handler(object sender, UnhandledExceptionEventArgs args)
        {
        }
    }

    [Fact]
    public static void Validate_InvalidArgument()
    {
        Console.WriteLine(nameof(Validate_InvalidArgument));

        {
            using HandlerRegistration registration = new(Handler);
            Assert.Throws<ArgumentNullException>(() => ExceptionHandling.RaiseUnhandledExceptionEvent(null!));
        }

        static void Handler(object sender, UnhandledExceptionEventArgs args)
        {
        }
    }
}