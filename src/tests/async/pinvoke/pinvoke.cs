// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

public class Async2PInvoke
{
    [Fact]
    public static void TestEntryPoint()
    {
        AsyncEntryPoint().Wait();
    }

    private static async Task AsyncEntryPoint()
    {
        unsafe
        {
            Assert.Equal(5, GetFPtr()());
        }

        await Task.Yield();

        unsafe
        {
            Assert.Equal(5, GetFPtr()());
        }

        await Task.Yield();

        unsafe
        {
            Assert.Equal(5, GetFPtr()());
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe delegate* unmanaged<int> GetFPtr() => &GetValue;

    [UnmanagedCallersOnly]
    private static int GetValue() => 5;
}
