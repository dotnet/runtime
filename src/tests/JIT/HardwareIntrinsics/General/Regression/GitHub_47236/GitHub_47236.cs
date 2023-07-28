// Licensed to the .NET Foundation under one or more agreements.(
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

namespace GitHub_47236;
public static class Program
{
    [Fact]
    public static void TestVector256()
    {
        // Test get_Zero() optimization:
        Assert.Equal("<0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0>", Vector256.Create(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0).ToString());
        Assert.Equal("<0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1>", Vector256.Create(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1).ToString());
        Assert.Equal("<0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector256.Create(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert.Equal("<0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector256.Create(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert.Equal("<0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector256.Create(0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert.Equal("<1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector256.Create(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert.Equal("<0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0>", Vector256.Create(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0).ToString());
        Assert.Equal("<0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1>", Vector256.Create(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1).ToString());
        Assert.Equal("<0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1>", Vector256.Create(0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert.Equal("<0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector256.Create(0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert.Equal("<0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector256.Create(0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert.Equal("<1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector256.Create(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert.Equal("<1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0>", Vector256.Create(1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0).ToString());
        Assert.Equal("<0, 0, 0, 0, 0, 0, 0, 0>", Vector256.Create(0, 0, 0, 0, 0, 0, 0, 0).ToString());
        Assert.Equal("<0, 0, 0, 0, 0, 0, 0, 1>", Vector256.Create(0, 0, 0, 0, 0, 0, 0, 1).ToString());
        Assert.Equal("<0, 0, 0, 0, 1, 1, 1, 1>", Vector256.Create(0, 0, 0, 0, 1, 1, 1, 1).ToString());
        Assert.Equal("<0, 0, 0, 1, 1, 1, 1, 1>", Vector256.Create(0, 0, 0, 1, 1, 1, 1, 1).ToString());
        Assert.Equal("<0, 1, 1, 1, 1, 1, 1, 1>", Vector256.Create(0, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert.Equal("<1, 1, 1, 1, 1, 1, 1, 1>", Vector256.Create(1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert.Equal("<1, 1, 1, 1, 0, 0, 0, 0>", Vector256.Create(1, 1, 1, 1, 0, 0, 0, 0).ToString());
        Assert.Equal("<0, 0, 0, 0>", Vector256.Create(0, 0, 0, 0).ToString());
        Assert.Equal("<0, 0, 1, 1>", Vector256.Create(0, 0, 1, 1).ToString());
        Assert.Equal("<0, 1, 1, 1>", Vector256.Create(0, 1, 1, 1).ToString());
        Assert.Equal("<1, 1, 1, 1>", Vector256.Create(1, 1, 1, 1).ToString());
        Assert.Equal("<1, 1, 0, 0>", Vector256.Create(1, 1, 0, 0).ToString());

        // Test get_AllBitSet() optimization:
        Assert.Equal("<-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1>", Vector256.Create(-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1).ToString());
        Assert.Equal("<-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 1>", Vector256.Create(-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 1).ToString());
        Assert.Equal("<-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector256.Create(-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert.Equal("<-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector256.Create(-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert.Equal("<-1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector256.Create(-1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert.Equal("<1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector256.Create(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert.Equal("<-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1>", Vector256.Create(-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1).ToString());
        Assert.Equal("<-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 1>", Vector256.Create(-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 1).ToString());
        Assert.Equal("<-1, -1, -1, -1, -1, -1, -1, -1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector256.Create(-1, -1, -1, -1, -1, -1, -1, -1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert.Equal("<-1, -1, -1, -1, -1, -1, -1, 1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector256.Create(-1, -1, -1, -1, -1, -1, -1, 1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert.Equal("<-1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector256.Create(-1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert.Equal("<1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector256.Create(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert.Equal("<1, 1, 1, 1, 1, 1, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1>", Vector256.Create(1, 1, 1, 1, 1, 1, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1).ToString());
        Assert.Equal("<-1, -1, -1, -1, -1, -1, -1, -1>", Vector256.Create(-1, -1, -1, -1, -1, -1, -1, -1).ToString());
        Assert.Equal("<-1, -1, -1, -1, -1, -1, -1, 1>", Vector256.Create(-1, -1, -1, -1, -1, -1, -1, 1).ToString());
        Assert.Equal("<-1, -1, -1, -1, 1, 1, 1, 1>", Vector256.Create(-1, -1, -1, -1, 1, 1, 1, 1).ToString());
        Assert.Equal("<-1, -1, -1, 1, 1, 1, 1, 1>", Vector256.Create(-1, -1, -1, 1, 1, 1, 1, 1).ToString());
        Assert.Equal("<-1, 1, 1, 1, 1, 1, 1, 1>", Vector256.Create(-1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert.Equal("<1, 1, 1, 1, 1, 1, 1, 1>", Vector256.Create(1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert.Equal("<1, 1, 1, 1, -1, -1, -1, -1>", Vector256.Create(1, 1, 1, 1, -1, -1, -1, -1).ToString());
        Assert.Equal("<-1, -1, -1, -1>", Vector256.Create(-1, -1, -1, -1).ToString());
        Assert.Equal("<-1, -1, 1, 1>", Vector256.Create(-1, -1, 1, 1).ToString());
        Assert.Equal("<-1, 1, 1, 1>", Vector256.Create(-1, 1, 1, 1).ToString());
        Assert.Equal("<1, 1, 1, 1>", Vector256.Create(1, 1, 1, 1).ToString());
        Assert.Equal("<1, 1, -1, -1>", Vector256.Create(1, 1, -1, -1).ToString());
    }

    [Fact]
    public static void TestVector128()
    {
        // Test get_Zero() optimization:
        Assert.Equal("<0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0>", Vector128.Create(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0).ToString());
        Assert.Equal("<0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1>", Vector128.Create(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1).ToString());
        Assert.Equal("<0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1>", Vector128.Create(0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert.Equal("<0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector128.Create(0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert.Equal("<0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector128.Create(0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert.Equal("<1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector128.Create(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert.Equal("<1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0>", Vector128.Create(1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0).ToString());
        Assert.Equal("<0, 0, 0, 0, 0, 0, 0, 0>", Vector128.Create(0, 0, 0, 0, 0, 0, 0, 0).ToString());
        Assert.Equal("<0, 0, 0, 0, 0, 0, 0, 1>", Vector128.Create(0, 0, 0, 0, 0, 0, 0, 1).ToString());
        Assert.Equal("<0, 0, 0, 0, 1, 1, 1, 1>", Vector128.Create(0, 0, 0, 0, 1, 1, 1, 1).ToString());
        Assert.Equal("<0, 0, 0, 1, 1, 1, 1, 1>", Vector128.Create(0, 0, 0, 1, 1, 1, 1, 1).ToString());
        Assert.Equal("<0, 1, 1, 1, 1, 1, 1, 1>", Vector128.Create(0, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert.Equal("<1, 1, 1, 1, 1, 1, 1, 1>", Vector128.Create(1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert.Equal("<1, 1, 1, 1, 0, 0, 0, 0>", Vector128.Create(1, 1, 1, 1, 0, 0, 0, 0).ToString());
        Assert.Equal("<0, 0, 0, 0>", Vector128.Create(0, 0, 0, 0).ToString());
        Assert.Equal("<0, 0, 1, 1>", Vector128.Create(0, 0, 1, 1).ToString());
        Assert.Equal("<0, 1, 1, 1>", Vector128.Create(0, 1, 1, 1).ToString());
        Assert.Equal("<1, 1, 1, 1>", Vector128.Create(1, 1, 1, 1).ToString());
        Assert.Equal("<1, 1, 0, 0>", Vector128.Create(1, 1, 0, 0).ToString());
        Assert.Equal("<0, 0>", Vector128.Create(0, 0).ToString());
        Assert.Equal("<0, 1>", Vector128.Create(0, 1).ToString());
        Assert.Equal("<1, 1>", Vector128.Create(1, 1).ToString());
        Assert.Equal("<1, 0>", Vector128.Create(1, 0).ToString());

        // Test get_AllBitSet() optimization:
        Assert.Equal("<-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1>", Vector128.Create(-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1).ToString());
        Assert.Equal("<-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 1>", Vector128.Create(-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 1).ToString());
        Assert.Equal("<-1, -1, -1, -1, -1, -1, -1, -1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector128.Create(-1, -1, -1, -1, -1, -1, -1, -1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert.Equal("<-1, -1, -1, -1, -1, -1, -1, 1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector128.Create(-1, -1, -1, -1, -1, -1, -1, 1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert.Equal("<-1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector128.Create(-1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert.Equal("<1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1>", Vector128.Create(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert.Equal("<1, 1, 1, 1, 1, 1, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1>", Vector128.Create(1, 1, 1, 1, 1, 1, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1).ToString());
        Assert.Equal("<-1, -1, -1, -1, -1, -1, -1, -1>", Vector128.Create(-1, -1, -1, -1, -1, -1, -1, -1).ToString());
        Assert.Equal("<-1, -1, -1, -1, -1, -1, -1, 1>", Vector128.Create(-1, -1, -1, -1, -1, -1, -1, 1).ToString());
        Assert.Equal("<-1, -1, -1, -1, 1, 1, 1, 1>", Vector128.Create(-1, -1, -1, -1, 1, 1, 1, 1).ToString());
        Assert.Equal("<-1, -1, -1, 1, 1, 1, 1, 1>", Vector128.Create(-1, -1, -1, 1, 1, 1, 1, 1).ToString());
        Assert.Equal("<-1, 1, 1, 1, 1, 1, 1, 1>", Vector128.Create(-1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert.Equal("<1, 1, 1, 1, 1, 1, 1, 1>", Vector128.Create(1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert.Equal("<1, 1, 1, 1, -1, -1, -1, -1>", Vector128.Create(1, 1, 1, 1, -1, -1, -1, -1).ToString());
        Assert.Equal("<-1, -1, -1, -1>", Vector128.Create(-1, -1, -1, -1).ToString());
        Assert.Equal("<-1, -1, 1, 1>", Vector128.Create(-1, -1, 1, 1).ToString());
        Assert.Equal("<-1, 1, 1, 1>", Vector128.Create(-1, 1, 1, 1).ToString());
        Assert.Equal("<1, 1, 1, 1>", Vector128.Create(1, 1, 1, 1).ToString());
        Assert.Equal("<1, 1, -1, -1>", Vector128.Create(1, 1, -1, -1).ToString());
        Assert.Equal("<-1, -1>", Vector128.Create(-1, -1).ToString());
        Assert.Equal("<-1, 1>", Vector128.Create(-1, 1).ToString());
        Assert.Equal("<1, -1>", Vector128.Create(1, -1).ToString());
    }

    [Fact]
    public static void TestVector64()
    {
        // Test get_Zero() optimization:
        Assert.Equal("<0, 0, 0, 0, 0, 0, 0, 0>", Vector64.Create(0, 0, 0, 0, 0, 0, 0, 0).ToString());
        Assert.Equal("<0, 0, 0, 0, 0, 0, 0, 1>", Vector64.Create(0, 0, 0, 0, 0, 0, 0, 1).ToString());
        Assert.Equal("<0, 0, 0, 0, 1, 1, 1, 1>", Vector64.Create(0, 0, 0, 0, 1, 1, 1, 1).ToString());
        Assert.Equal("<0, 0, 0, 1, 1, 1, 1, 1>", Vector64.Create(0, 0, 0, 1, 1, 1, 1, 1).ToString());
        Assert.Equal("<0, 1, 1, 1, 1, 1, 1, 1>", Vector64.Create(0, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert.Equal("<1, 1, 1, 1, 1, 1, 1, 1>", Vector64.Create(1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert.Equal("<1, 1, 1, 1, 0, 0, 0, 0>", Vector64.Create(1, 1, 1, 1, 0, 0, 0, 0).ToString());
        Assert.Equal("<0, 0, 0, 0>", Vector64.Create(0, 0, 0, 0).ToString());
        Assert.Equal("<0, 0, 1, 1>", Vector64.Create(0, 0, 1, 1).ToString());
        Assert.Equal("<0, 1, 1, 1>", Vector64.Create(0, 1, 1, 1).ToString());
        Assert.Equal("<1, 1, 1, 1>", Vector64.Create(1, 1, 1, 1).ToString());
        Assert.Equal("<1, 1, 0, 0>", Vector64.Create(1, 1, 0, 0).ToString());
        Assert.Equal("<0, 0>", Vector64.Create(0, 0).ToString());
        Assert.Equal("<0, 1>", Vector64.Create(0, 1).ToString());
        Assert.Equal("<1, 1>", Vector64.Create(1, 1).ToString());
        Assert.Equal("<1, 0>", Vector64.Create(1, 0).ToString());

        // Test get_AllBitSet() optimization:
        Assert.Equal("<-1, -1, -1, -1, -1, -1, -1, -1>", Vector64.Create(-1, -1, -1, -1, -1, -1, -1, -1).ToString());
        Assert.Equal("<-1, -1, -1, -1, -1, -1, -1, 1>", Vector64.Create(-1, -1, -1, -1, -1, -1, -1, 1).ToString());
        Assert.Equal("<-1, -1, -1, -1, 1, 1, 1, 1>", Vector64.Create(-1, -1, -1, -1, 1, 1, 1, 1).ToString());
        Assert.Equal("<-1, -1, -1, 1, 1, 1, 1, 1>", Vector64.Create(-1, -1, -1, 1, 1, 1, 1, 1).ToString());
        Assert.Equal("<-1, 1, 1, 1, 1, 1, 1, 1>", Vector64.Create(-1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert.Equal("<1, 1, 1, 1, 1, 1, 1, 1>", Vector64.Create(1, 1, 1, 1, 1, 1, 1, 1).ToString());
        Assert.Equal("<1, 1, 1, 1, -1, -1, -1, -1>", Vector64.Create(1, 1, 1, 1, -1, -1, -1, -1).ToString());
        Assert.Equal("<-1, -1, -1, -1>", Vector64.Create(-1, -1, -1, -1).ToString());
        Assert.Equal("<-1, -1, 1, 1>", Vector64.Create(-1, -1, 1, 1).ToString());
        Assert.Equal("<-1, 1, 1, 1>", Vector64.Create(-1, 1, 1, 1).ToString());
        Assert.Equal("<1, 1, 1, 1>", Vector64.Create(1, 1, 1, 1).ToString());
        Assert.Equal("<1, 1, -1, -1>", Vector64.Create(1, 1, -1, -1).ToString());
        Assert.Equal("<-1, -1>", Vector64.Create(-1, -1).ToString());
        Assert.Equal("<-1, 1>", Vector64.Create(-1, 1).ToString());
        Assert.Equal("<1, -1>", Vector64.Create(1, -1).ToString());
    }
}
