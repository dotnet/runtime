// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Runtime.CompilerServices;
public class Program
{
    // v2 -> v1 -> v2 -> v1
    [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
    public static async Task Foo()
    {
        await Task.Yield();
        try
        {
#line 1 "Program.cs"
            await V1Methods.Test0(Foo1);
        }
        catch (NotImplementedException)
        {
            throw;
        }
    }

    [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
    private static async Task<int> Foo1(int i)
    {
        await Task.Yield();
        try
        {
#line 2 "Program.cs"
            await Foo2(i);
            return i * 2;
        }
        catch (NotImplementedException)
        {
            throw;
        }
    }

    [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
    private static async Task<int> Foo2(int i)
    {
        try
        {
            await Task.Yield();
            await V1Methods.Test2(i);
        }
        finally
        {
#line 3 "Program.cs"
            throw new NotImplementedException("Not Found from Foo2");
        }
    }

    [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
    public static async Task Bar(int i)
    {
        if (i == 0)
#line 4 "Program.cs"
            throw new Exception("Exception from Bar");
#line 5 "Program.cs"
        await Bar(i - 1);
    }

    [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
    public static async Task<int> Baz()
    {
#line 6 "Program.cs"
        throw new Exception("Exception from Baz");
    }

    [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
    public static async Task<int> Qux(int i)
    {
        await Task.Yield();
        try
        {
            return 9 / i;
        }
        catch (DivideByZeroException)
        {
#line 7 "Program.cs"
            throw new DivideByZeroException("Exception from Qux");
        }
    }

    // also v2 v1 chaining but this time we don't have finally
    [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
    public static async Task Quux()
    {
        await Task.Yield();
        try
        {
#line 8 "Program.cs"
            await V1Methods.Test0(Quux1);
        }
        catch (NotImplementedException)
        {
            throw;
        }
    }

    [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
    private static async Task<int> Quux1(int i)
    {
        try
        {
            await Task.Yield();
#line 9 "Program.cs"
            throw new NotImplementedException("Not Found from Quux1");
        }
        catch (NotImplementedException)
        {
            throw;
        }
    }

    [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
    public static async Task<int> Quuux()
    {
        var task = Quuux2();
        await Task.Yield();
#line 10 "Program.cs"
        return await task;
    }

    [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
    private static async Task<int> Quuux2()
    {
        await Task.Yield();
#line 11 "Program.cs"
        throw new Exception("Exception from Quuux2");
    }
}
