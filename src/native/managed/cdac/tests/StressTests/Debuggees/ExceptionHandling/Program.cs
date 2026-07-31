// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

/// <summary>
/// Exercises exception handling: try/catch/finally funclets, nested exceptions,
/// filter funclets, and rethrow.
/// </summary>
internal static class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TryCatchScenario()
    {
        object before = new object();
        try
        {
            object inside = new object();
            ThrowHelper();
            GC.KeepAlive(inside);
        }
        catch (InvalidOperationException ex)
        {
            object inCatch = new object();
            GC.KeepAlive(ex);
            GC.KeepAlive(inCatch);
        }
        GC.KeepAlive(before);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void ThrowHelper()
    {
        throw new InvalidOperationException("test exception");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TryFinallyScenario()
    {
        object outerRef = new object();
        try
        {
            object innerRef = new object();
            GC.KeepAlive(innerRef);
        }
        finally
        {
            object finallyRef = new object();
            GC.KeepAlive(finallyRef);
        }
        GC.KeepAlive(outerRef);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void NestedExceptionScenario()
    {
        object a = new object();
        try
        {
            try
            {
                object c = new object();
                throw new ArgumentException("inner");
            }
            catch (ArgumentException ex1)
            {
                GC.KeepAlive(ex1);
                throw new InvalidOperationException("outer", ex1);
            }
            finally
            {
                object d = new object();
                GC.KeepAlive(d);
            }
        }
        catch (InvalidOperationException ex2)
        {
            GC.KeepAlive(ex2);
        }
        GC.KeepAlive(a);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void FilterExceptionScenario()
    {
        object holder = new object();
        try
        {
            throw new ArgumentException("filter-test");
        }
        catch (ArgumentException ex) when (FilterCheck(ex))
        {
            GC.KeepAlive(ex);
        }
        GC.KeepAlive(holder);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool FilterCheck(Exception ex)
    {
        object filterLocal = new object();
        GC.KeepAlive(filterLocal);
        return ex.Message.Contains("filter");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void RethrowScenario()
    {
        object outerRef = new object();
        try
        {
            try
            {
                throw new ApplicationException("rethrow-test");
            }
            catch (ApplicationException)
            {
                object catchRef = new object();
                GC.KeepAlive(catchRef);
                throw;
            }
        }
        catch (ApplicationException ex)
        {
            GC.KeepAlive(ex);
        }
        GC.KeepAlive(outerRef);
    }

    static int Main()
    {
        for (int i = 0; i < 2; i++)
        {
            TryCatchScenario();
            TryFinallyScenario();
            NestedExceptionScenario();
            FilterExceptionScenario();
            RethrowScenario();
        }
        return 100;
    }
}
