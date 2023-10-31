// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

public class Async2SimpleEH
{
    [Fact]
    public static void Test()
    {
        Task.Run(AsyncEntry).Wait();
    }

    public static async Task AsyncEntry()
    {
        int result = await Handler();
        Assert.Equal(42, result);
    }

    public static async2 int Handler()
    {
        try
        {
            return await Throw(42);
        }
        catch (IntegerException ex)
        {
            System.Console.WriteLine(ex);
            return ex.Value;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async2 int Throw(int value)
    {
        await Task.Yield();
        throw new IntegerException(value);
    }

    public class IntegerException : Exception
    {
        public int Value;
        public IntegerException(int value) => Value = value;
    }
}
