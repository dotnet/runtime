// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

// Adapted from:
// Fuzzlyn v1.5 on 2022-07-24 15:28:54
// Run on X86 Windows
// Seed: 9097970668016732717
// Reduced from 177.2 KiB to 1.0 KiB in 00:04:15
// Hits JIT assert in Release:
// Assertion failed '!comp->opts.compJitEarlyExpandMDArrays' in 'Program:Main(Fuzzlyn.ExecutionServer.IRuntime)' during 'Lowering nodeinfo' (IL size 111; hash 0xade6b36b; FullOpts)
// 
//     File: D:\a\_work\1\s\src\coreclr\jit\lower.cpp Line: 264
// 
public class Program
{
    public static int PASS = 100;
    public static int FAIL = 0;
    public static IRuntime s_rt;
    public static bool s_1;
    public static bool[, ] s_12;
    public static int s_22;
    public static byte[] s_27;
    public static ushort s_33;
    public static bool s_43;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test()
    {
        try
        {
            try
            {
            }
            finally
            {
                if (s_12[0, 0])
                {
                    int vr3 = s_22;
                }
            }
        }
        finally
        {
            if (s_1)
            {
                ushort vr4 = s_33--;
                s_rt.WriteLine(vr4);
            }
            else
            {
                byte vr5 = s_27[0];
                s_rt.WriteLine(vr5);
            }

            bool vr6 = s_43;
            s_rt.WriteLine(vr6);
        }
    }

    public static int Main()
    {
        try
        {
            Test();
        }
        catch (NullReferenceException)
        {
            // This is expected
        }
        catch (Exception)
        {
            return FAIL;
        }

        return PASS;
    }
}

public interface IRuntime
{
    void WriteLine<T>(T value);
}

public class Runtime : IRuntime
{
    public void WriteLine<T>(T value) => System.Console.WriteLine(value);
}
