// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Runtime.CompilerServices;
public static class V1Methods
{
    [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
    public static async Task Test0(Func<int, Task> method)
    {
#line 1 "Program.cs"
        await Test1(method);
        await Task.Yield();
    }

    [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
    public static async Task Test1(Func<int, Task> method)
    {
        try
        {
#line 2 "Program.cs"
            await method(3);
        }
        catch (Exception ex) when (ex.Message.Contains("404"))
        {
            Console.WriteLine($"Caught exception in Test1 with: {ex}");
        }
    }

    [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
    public static async Task Test2(int i)
    {
        throw new NullReferenceException("Exception from Test2");
    }
}
