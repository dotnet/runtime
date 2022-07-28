// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

internal class BaseException : Exception { }
internal class DerivedException : BaseException { }

public class Test_cast_throw
{
    [Fact]
    public static int TestEntryPoint()
    {
        BaseException ex = new DerivedException();
        try
        {
            if (ex is DerivedException)
                throw (DerivedException)ex;
        }
        catch (DerivedException)
        {
            goto continue_1;
        }
        Console.WriteLine("failed(1)");
        return 101;

    continue_1:
        try
        {
            if (ex is DerivedException)
                throw (DerivedException)ex;
        }
        catch (DerivedException)
        {
            goto continue_2;
        }
        Console.WriteLine("failed(2)");
        return 102;

    continue_2:
        Console.WriteLine("Good");
        return 100;
    }
}
