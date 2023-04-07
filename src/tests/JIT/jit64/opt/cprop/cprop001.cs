// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
public class LIM
{
    public static int accumulator = 0;

    [Fact]
    public static int TestEntryPoint()
    {
        int x;
        int a = GetInt(1);

        for (int i = 0; i < 5; i++)
        {
            x = a;
            Accumulate(x);
            x = GetInt(0);
            Accumulate(x);
        }

        if (accumulator == 5)
        {
            System.Console.WriteLine("Pass");
            return 100;
        }

        System.Console.WriteLine("!!!FAIL!!!");
        return -1;
    }



    public static int GetInt(int x)
    {
        try
        {
            return x;
        }
        catch
        {
            throw;
        }
    }


    internal static void Accumulate(int x)
    {
        try
        {
            accumulator += x;
        }
        catch
        {
            throw;
        }
    }
}
