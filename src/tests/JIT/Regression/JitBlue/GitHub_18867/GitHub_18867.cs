// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

interface IRT
{
    void WriteLine<T>(T val);
}

class CRT : IRT
{
    public static object line;
    public void WriteLine<T>(T val) => line = val;
}

public class Program
{
    static IRT s_rt;
    static byte[] s_1 = new byte[] { 0 };
    static int s_3;
    static short[] s_8 = new short[] { -1 };

    [Fact]
    public static int TestEntryPoint()
    {
        s_rt = new CRT();
        M11(s_8, 0, 0, 0, true, s_1);
        return ((int)CRT.line == -1) ? 100 : 1;
    }

    // Test case for a lvNormalizeOnLoad related issue in assertion propagation.
    // A "normal" lclvar is substituted with a "normalize on load" lclvar (arg3),
    // that results in load normalization being skipped.

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ushort M11(short[] arg0, ushort arg1, short arg3, byte arg4, bool arg7, byte[] arg10)
    {
        if (arg7)
        {
            ulong var4 = (ulong)s_3;

            // mov edi, gword ptr [classVar[0x2c44174]]
            // cmp dword ptr [edi + 4], 0
            // jbe SHORT G_M17557_IG06
            // movsx edi, word ptr [edi + 8]
            // mov word ptr [ebp + 14H], di ; word only store
            arg3 = s_8[0];

            short var5 = arg3;
            s_rt.WriteLine(var4);

            // call CORINFO_HELP_VIRTUAL_FUNC_PTR
            // mov ecx, edi
            // mov edx, dword ptr [ebp + 14H] ; dword load, no sign extension
            // call eax
            s_rt.WriteLine((int)var5);
        }

        if (!arg7)
        {
            var vr7 = arg0[0];
        }

        arg10[0] = arg4;
        return arg1;
    }
}
