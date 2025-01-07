// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using Xunit;

#pragma warning disable CS8500

unsafe public class Program
{

    public static void AsTypedReference<T>(ref T value, TypedReference* output)
    {
        *output = __makeref(value);
        value = (T)(object)"Hello";
    }

    [Fact]
    public static int TestEntryPoint()
    {
        // In this test, we try to reflect on a signature of a method that takes a TypedReference*.
        // This is not useful for much else than Reflection.Emit or Delegate.CreateDelegate.
        // (Do not Reflection.Invoke this - the TypedReference is likely going to point to a dead
        // temporary when the method returns.)
        var method = typeof(Program).GetMethod(nameof(Program.AsTypedReference));
        var s = method.ToString();
        Console.WriteLine(s);
        if (s != "Void AsTypedReference[T](T ByRef, TypedReference*)")
            return 1;

        return 100;
    }
}

#pragma warning restore CS8500
