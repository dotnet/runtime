// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
public class Program
{
    // v2 -> v1 -> v2 -> v1
    public static async Task Foo()
    {
        await Task.Yield();
        try
        {
#line 12 "Program.cs"
            await V1Methods.Test0(Foo1);
        }
        catch (NotImplementedException)
        {
            throw;
        }
    }

    private static async Task<int> Foo1(int i)
    {
        await Task.Yield();
        try
        {
#line 26 "Program.cs"
            await Foo2(i);
            return i * 2;
        }
        catch (NotImplementedException)
        {
            throw;
        }
    }

    private static async Task<int> Foo2(int i)
    {
        try
        {
            await Task.Yield();
            await Task.Yield();
            await Task.Yield();
            for (int j = i; j > 0; j--)
            {
                await Foo2(j - 1);
            }
            if (i == 0)
                await V1Methods.Test2(i);
        }
        finally
        {
            Console.WriteLine($"In finally block of Foo2 with {i}");
#line 53 "Program.cs"
            throw new NotImplementedException("Not Found from Foo2");
        }
    }

    public static async Task Bar(int i)
    {
        if (i == 0)
#line 61 "Program.cs"
            throw new Exception("Exception from Bar");
#line 63 "Program.cs"
        await Bar(i - 1);
    }

    public static async Task<int> Baz()
    {
#line 69 "Program.cs"
        throw new Exception("Exception from Baz");
    }

    public static async Task<int> Qux(int i)
    {
        await Task.Yield();
        try
        {
            return 9 / i;
        }
        catch (DivideByZeroException)
        {
#line 82 "Program.cs"
            throw new DivideByZeroException("Exception from Qux");
        }
    }

    // also v2 v1 chaining but this time we don't have finally
    public static async Task Quux()
    {
        await Task.Yield();
        try
        {
#line 93 "Program.cs"
            await V1Methods.Test0(Quux1);
        }
        catch (NotImplementedException)
        {
            throw;
        }
    }

    private static async Task<int> Quux1(int i)
    {
        try
        {
            await Task.Yield();
#line 107 "Program.cs"
            throw new NotImplementedException("Not Found from Quux1");
        }
        catch (NotImplementedException)
        {
            throw;
        }
    }

    public static async Task<int> Quuux()
    {
        var task = Quuux2();
        Console.WriteLine("hello from Quuux");
#line 120 "Program.cs"
        return await task;
    }

    private static async Task<int> Quuux2()
    {
        await Task.Yield();
#line 127 "Program.cs"
        throw new Exception("Exception from Quuux2");
    }
}
