// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Runtime_129527;

using System.Runtime.Intrinsics.Arm;
using System.Threading.Tasks;
using Xunit;

public struct S1
{
    public long F3;
}

public interface IRuntime
{
    void WriteLine<T>(string site, T value);
}

public sealed class Runtime : IRuntime
{
    public void WriteLine<T>(string site, T value)
    {
    }
}

public static class Runtime_129527
{
    private static IRuntime s_rt = new Runtime();
    private static bool[] s_8 = new bool[1];
    private static long s_13;

    [Fact]
    public static int TestEntryPoint()
    {
        M0();
        return s_13 == 0 ? 100 : 101;
    }

    private static void M0()
    {
        uint var9 = default;
        S1[] var24 = new S1[1];
        ulong var34 = default;
        try
        {
            var9 = 1;
            s_8[0] |= false;
        }
        catch
        {
        }

        for (int lvar31 = -2147483646; lvar31 > -2147483648; lvar31--)
        {
            ulong vr0 = (ulong)(lvar31 - var9++);
            short var33 = Crc32.Arm64.IsSupported ? (short)Crc32.Arm64.ComputeCrc32C(0, vr0) : (short)vr0;
            s_13 = var24[0].F3;
            M6().GetAwaiter().GetResult();
            s_rt.WriteLine("c_122", var34);
            s_rt.WriteLine("c_126", var33);
        }
    }

    private static async Task M6()
    {
        await Task.Yield();
    }
}
