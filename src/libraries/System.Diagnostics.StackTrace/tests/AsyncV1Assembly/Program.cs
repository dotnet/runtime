// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
public static class V1Methods
{
    public static async Task Test0(Func<int, Task> method)
    {
#line 8 "Program.cs"
        await Test1(method);
        await Task.Yield();
    }

    public static async Task Test1(Func<int, Task> method)
    {
        try
        {
#line 17 "Program.cs"
            await method(3);
        }
        catch (Exception ex) when (ex.Message.Contains("404"))
        {
            Console.WriteLine($"Caught exception in Test1 with: {ex}");
        }
    }

    public static async Task Test2(int i)
    {
        Console.WriteLine($"In Test2 with {i}");
        throw new NullReferenceException("Exception from Test2");
    }
}
