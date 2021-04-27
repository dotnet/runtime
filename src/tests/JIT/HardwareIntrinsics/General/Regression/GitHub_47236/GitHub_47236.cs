// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

public static class Program
{
    private static int s_returnCode = 100;

    public static int Main(string[] args)
    {
        TestVector256();
        TestVector128();
        TestVector64();
        return s_returnCode;
    }

    public static void TestVector256()
    {
        // Test get_Zero() optimization:
        Assert("<0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0>", Vector256.Create(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0).ToString());
        Assert("<0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1>", Vector256.Create(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1).ToString());
        Assert("<0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector256.Create(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert("<0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector256.Create(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert("<0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector256.Create(0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert("<1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector256.Create(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert("<0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0>", Vector256.Create(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0).ToString());
        Assert("<0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1>", Vector256.Create(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1).ToString());
        Assert("<0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1>", Vector256.Create(0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert("<0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector256.Create(0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert("<0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector256.Create(0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert("<1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector256.Create(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert("<1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0>", Vector256.Create(1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0).ToString());
        Assert("<0, 0, 0, 0, 0, 0, 0, 0>", Vector256.Create(0, 0, 0, 0, 0, 0, 0, 0).ToString());
        Assert("<0, 0, 0, 0, 0, 0, 0, 1>", Vector256.Create(0, 0, 0, 0, 0, 0, 0, 1).ToString());
        Assert("<0, 0, 0, 0, 1, 1, 1, 1>", Vector256.Create(0, 0, 0, 0, 1, 1, 1, 1).ToString());
        Assert("<0, 0, 0, 1, 1, 1, 1, 1>", Vector256.Create(0, 0, 0, 1, 1, 1, 1, 1).ToString());
        Assert("<0, 1, 1, 1, 1, 1, 1, 1>", Vector256.Create(0, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert("<1, 1, 1, 1, 1, 1, 1, 1>", Vector256.Create(1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert("<1, 1, 1, 1, 0, 0, 0, 0>", Vector256.Create(1, 1, 1, 1, 0, 0, 0, 0).ToString());
        Assert("<0, 0, 0, 0>", Vector256.Create(0, 0, 0, 0).ToString());
        Assert("<0, 0, 1, 1>", Vector256.Create(0, 0, 1, 1).ToString());
        Assert("<0, 1, 1, 1>", Vector256.Create(0, 1, 1, 1).ToString());
        Assert("<1, 1, 1, 1>", Vector256.Create(1, 1, 1, 1).ToString());
        Assert("<1, 1, 0, 0>", Vector256.Create(1, 1, 0, 0).ToString());

        // Test get_AllBitSet() optimization:
        Assert("<-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1>", Vector256.Create(-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1).ToString());
        Assert("<-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 1>", Vector256.Create(-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 1).ToString());
        Assert("<-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector256.Create(-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert("<-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector256.Create(-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert("<-1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector256.Create(-1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert("<1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector256.Create(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert("<-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1>", Vector256.Create(-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1).ToString());
        Assert("<-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 1>", Vector256.Create(-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 1).ToString());
        Assert("<-1, -1, -1, -1, -1, -1, -1, -1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector256.Create(-1, -1, -1, -1, -1, -1, -1, -1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert("<-1, -1, -1, -1, -1, -1, -1, 1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector256.Create(-1, -1, -1, -1, -1, -1, -1, 1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert("<-1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector256.Create(-1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert("<1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector256.Create(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert("<1, 1, 1, 1, 1, 1, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1>", Vector256.Create(1, 1, 1, 1, 1, 1, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1).ToString());
        Assert("<-1, -1, -1, -1, -1, -1, -1, -1>", Vector256.Create(-1, -1, -1, -1, -1, -1, -1, -1).ToString());
        Assert("<-1, -1, -1, -1, -1, -1, -1, 1>", Vector256.Create(-1, -1, -1, -1, -1, -1, -1, 1).ToString());
        Assert("<-1, -1, -1, -1, 1, 1, 1, 1>", Vector256.Create(-1, -1, -1, -1, 1, 1, 1, 1).ToString());
        Assert("<-1, -1, -1, 1, 1, 1, 1, 1>", Vector256.Create(-1, -1, -1, 1, 1, 1, 1, 1).ToString());
        Assert("<-1, 1, 1, 1, 1, 1, 1, 1>", Vector256.Create(-1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert("<1, 1, 1, 1, 1, 1, 1, 1>", Vector256.Create(1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert("<1, 1, 1, 1, -1, -1, -1, -1>", Vector256.Create(1, 1, 1, 1, -1, -1, -1, -1).ToString());
        Assert("<-1, -1, -1, -1>", Vector256.Create(-1, -1, -1, -1).ToString());
        Assert("<-1, -1, 1, 1>", Vector256.Create(-1, -1, 1, 1).ToString());
        Assert("<-1, 1, 1, 1>", Vector256.Create(-1, 1, 1, 1).ToString());
        Assert("<1, 1, 1, 1>", Vector256.Create(1, 1, 1, 1).ToString());
        Assert("<1, 1, -1, -1>", Vector256.Create(1, 1, -1, -1).ToString());
    }

    public static void TestVector128()
    {
        // Test get_Zero() optimization:
        Assert("<0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0>", Vector128.Create(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0).ToString());
        Assert("<0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1>", Vector128.Create(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1).ToString());
        Assert("<0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1>", Vector128.Create(0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert("<0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector128.Create(0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert("<0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector128.Create(0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert("<1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector128.Create(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert("<1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0>", Vector128.Create(1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0).ToString());
        Assert("<0, 0, 0, 0, 0, 0, 0, 0>", Vector128.Create(0, 0, 0, 0, 0, 0, 0, 0).ToString());
        Assert("<0, 0, 0, 0, 0, 0, 0, 1>", Vector128.Create(0, 0, 0, 0, 0, 0, 0, 1).ToString());
        Assert("<0, 0, 0, 0, 1, 1, 1, 1>", Vector128.Create(0, 0, 0, 0, 1, 1, 1, 1).ToString());
        Assert("<0, 0, 0, 1, 1, 1, 1, 1>", Vector128.Create(0, 0, 0, 1, 1, 1, 1, 1).ToString());
        Assert("<0, 1, 1, 1, 1, 1, 1, 1>", Vector128.Create(0, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert("<1, 1, 1, 1, 1, 1, 1, 1>", Vector128.Create(1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert("<1, 1, 1, 1, 0, 0, 0, 0>", Vector128.Create(1, 1, 1, 1, 0, 0, 0, 0).ToString());
        Assert("<0, 0, 0, 0>", Vector128.Create(0, 0, 0, 0).ToString());
        Assert("<0, 0, 1, 1>", Vector128.Create(0, 0, 1, 1).ToString());
        Assert("<0, 1, 1, 1>", Vector128.Create(0, 1, 1, 1).ToString());
        Assert("<1, 1, 1, 1>", Vector128.Create(1, 1, 1, 1).ToString());
        Assert("<1, 1, 0, 0>", Vector128.Create(1, 1, 0, 0).ToString());
        Assert("<0, 0>", Vector128.Create(0, 0).ToString());
        Assert("<0, 1>", Vector128.Create(0, 1).ToString());
        Assert("<1, 1>", Vector128.Create(1, 1).ToString());
        Assert("<1, 0>", Vector128.Create(1, 0).ToString());

        // Test get_AllBitSet() optimization:
        Assert("<-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1>", Vector128.Create(-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1).ToString());
        Assert("<-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 1>", Vector128.Create(-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 1).ToString());
        Assert("<-1, -1, -1, -1, -1, -1, -1, -1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector128.Create(-1, -1, -1, -1, -1, -1, -1, -1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert("<-1, -1, -1, -1, -1, -1, -1, 1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector128.Create(-1, -1, -1, -1, -1, -1, -1, 1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert("<-1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector128.Create(-1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert("<1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector128.Create(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert("<1, 1, 1, 1, 1, 1, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1>", Vector128.Create(1, 1, 1, 1, 1, 1, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1).ToString());
        Assert("<-1, -1, -1, -1, -1, -1, -1, -1>", Vector128.Create(-1, -1, -1, -1, -1, -1, -1, -1).ToString());
        Assert("<-1, -1, -1, -1, -1, -1, -1, 1>", Vector128.Create(-1, -1, -1, -1, -1, -1, -1, 1).ToString());
        Assert("<-1, -1, -1, -1, 1, 1, 1, 1>", Vector128.Create(-1, -1, -1, -1, 1, 1, 1, 1).ToString());
        Assert("<-1, -1, -1, 1, 1, 1, 1, 1>", Vector128.Create(-1, -1, -1, 1, 1, 1, 1, 1).ToString());
        Assert("<-1, 1, 1, 1, 1, 1, 1, 1>", Vector128.Create(-1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert("<1, 1, 1, 1, 1, 1, 1, 1>", Vector128.Create(1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert("<1, 1, 1, 1, -1, -1, -1, -1>", Vector128.Create(1, 1, 1, 1, -1, -1, -1, -1).ToString());
        Assert("<-1, -1, -1, -1>", Vector128.Create(-1, -1, -1, -1).ToString());
        Assert("<-1, -1, 1, 1>", Vector128.Create(-1, -1, 1, 1).ToString());
        Assert("<-1, 1, 1, 1>", Vector128.Create(-1, 1, 1, 1).ToString());
        Assert("<1, 1, 1, 1>", Vector128.Create(1, 1, 1, 1).ToString());
        Assert("<1, 1, -1, -1>", Vector128.Create(1, 1, -1, -1).ToString());
        Assert("<-1, -1>", Vector128.Create(-1, -1).ToString());
        Assert("<-1, 1>", Vector128.Create(-1, 1).ToString());
        Assert("<1, -1>", Vector128.Create(1, -1).ToString());
    }

    public static void TestVector64()
    {
        // Test get_Zero() optimization:
        Assert("<0, 0, 0, 0, 0, 0, 0, 0>", Vector64.Create(0, 0, 0, 0, 0, 0, 0, 0).ToString());
        Assert("<0, 0, 0, 0, 0, 0, 0, 1>", Vector64.Create(0, 0, 0, 0, 0, 0, 0, 1).ToString());
        Assert("<0, 0, 0, 0, 1, 1, 1, 1>", Vector64.Create(0, 0, 0, 0, 1, 1, 1, 1).ToString());
        Assert("<0, 0, 0, 1, 1, 1, 1, 1>", Vector64.Create(0, 0, 0, 1, 1, 1, 1, 1).ToString());
        Assert("<0, 1, 1, 1, 1, 1, 1, 1>", Vector64.Create(0, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert("<1, 1, 1, 1, 1, 1, 1, 1>", Vector64.Create(1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert("<1, 1, 1, 1, 0, 0, 0, 0>", Vector64.Create(1, 1, 1, 1, 0, 0, 0, 0).ToString());
        Assert("<0, 0, 0, 0>", Vector64.Create(0, 0, 0, 0).ToString());
        Assert("<0, 0, 1, 1>", Vector64.Create(0, 0, 1, 1).ToString());
        Assert("<0, 1, 1, 1>", Vector64.Create(0, 1, 1, 1).ToString());
        Assert("<1, 1, 1, 1>", Vector64.Create(1, 1, 1, 1).ToString());
        Assert("<1, 1, 0, 0>", Vector64.Create(1, 1, 0, 0).ToString());
        Assert("<0, 0>", Vector64.Create(0, 0).ToString());
        Assert("<0, 1>", Vector64.Create(0, 1).ToString());
        Assert("<1, 1>", Vector64.Create(1, 1).ToString());
        Assert("<1, 0>", Vector64.Create(1, 0).ToString());

        // Test get_AllBitSet() optimization:
        Assert("<-1, -1, -1, -1, -1, -1, -1, -1>", Vector64.Create(-1, -1, -1, -1, -1, -1, -1, -1).ToString());
        Assert("<-1, -1, -1, -1, -1, -1, -1, 1>", Vector64.Create(-1, -1, -1, -1, -1, -1, -1, 1).ToString());
        Assert("<-1, -1, -1, -1, 1, 1, 1, 1>", Vector64.Create(-1, -1, -1, -1, 1, 1, 1, 1).ToString());
        Assert("<-1, -1, -1, 1, 1, 1, 1, 1>", Vector64.Create(-1, -1, -1, 1, 1, 1, 1, 1).ToString());
        Assert("<-1, 1, 1, 1, 1, 1, 1, 1>", Vector64.Create(-1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert("<1, 1, 1, 1, 1, 1, 1, 1>", Vector64.Create(1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert("<1, 1, 1, 1, -1, -1, -1, -1>", Vector64.Create(1, 1, 1, 1, -1, -1, -1, -1).ToString());
        Assert("<-1, -1, -1, -1>", Vector64.Create(-1, -1, -1, -1).ToString());
        Assert("<-1, -1, 1, 1>", Vector64.Create(-1, -1, 1, 1).ToString());
        Assert("<-1, 1, 1, 1>", Vector64.Create(-1, 1, 1, 1).ToString());
        Assert("<1, 1, 1, 1>", Vector64.Create(1, 1, 1, 1).ToString());
        Assert("<1, 1, -1, -1>", Vector64.Create(1, 1, -1, -1).ToString());
        Assert("<-1, -1>", Vector64.Create(-1, -1).ToString());
        Assert("<-1, 1>", Vector64.Create(-1, 1).ToString());
        Assert("<1, -1>", Vector64.Create(1, -1).ToString());
    }

    private static void Assert(string expected, string actual, [CallerLineNumber] int line = 0)
    {
        if (expected != actual)
        {
            s_returnCode++;
            Console.WriteLine($"{expected} != {actual}, L{line}");
        }
    }
}
