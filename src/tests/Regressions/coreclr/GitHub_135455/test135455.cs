// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_135455
{
    private class ThrowingCctor
    {
        static ThrowingCctor() { }
        public static readonly int Value = Throw();
        private static int Throw() => throw new InvalidOperationException();
    }

    private class ThrowingCctor<T>
    {
        static ThrowingCctor() { }
        public static readonly int Value = Throw();
        private static int Throw() => throw new InvalidOperationException();
    }

    // The static field access is the first instruction of both try regions, so an exception thrown by the class
    // constructor has to be attributed to the regions themselves, not to the code before them.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CctorExceptionAtTryStart()
    {
        int result = 0;
        try
        {
            try
            {
                GC.KeepAlive(ThrowingCctor.Value);
            }
            finally
            {
                result |= 1;
            }
        }
        catch (TypeInitializationException)
        {
            result |= 2;
        }
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CctorExceptionAtTryStart<T>()
    {
        int result = 0;
        try
        {
            try
            {
                GC.KeepAlive(ThrowingCctor<T>.Value);
            }
            finally
            {
                result |= 1;
            }
        }
        catch (TypeInitializationException)
        {
            result |= 2;
        }
        return result;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        // The first access runs the class constructor; the second rethrows the recorded failure.
        // Shared generic code gets the static base through a generic lookup.
        for (int i = 0; i < 2; i++)
        {
            Assert.Equal(3, CctorExceptionAtTryStart());
            Assert.Equal(3, CctorExceptionAtTryStart<string>());
        }
    }
}
