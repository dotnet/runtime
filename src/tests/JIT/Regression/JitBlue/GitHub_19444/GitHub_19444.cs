// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This test case illustrates a bug where the JIT_ByRefWriteBarrier was not
// included in IsIPInMarkedJitHelper on non-32-bit-x86 platforms.

using System;
using Xunit;

class C0
{
}

struct S0
{
    public C0 F0;
    public ulong F4;
}

class C1
{
    public S0 F3;
}

struct S1
{
    public S0 F3;
}

struct S2
{
    public C0 F0;
    public uint F4;
}

class C3
{
    public S2 F3;
}

struct S3
{
    public S2 F3;
}

public class GitHub_19444
{
    static S1 s_38;
    static C1 s_43;
    static S3 s_1;
    static C3 s_2;

    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            s_38.F3 = s_43.F3;
            s_1.F3 = s_2.F3;
        }
        catch (System.NullReferenceException)
        {
            Console.WriteLine("PASS");
            return 100;
        }
        Console.WriteLine("FAIL");
        return -1;
    }
}

