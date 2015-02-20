// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

public static unsafe class StringComparerTests
{
    [Fact]
    public static void TestCurrent()
    {
        VerifyComparer(StringComparer.CurrentCulture, false);
        VerifyComparer(StringComparer.CurrentCultureIgnoreCase, true);
    }

    [Fact]
    public static void TestOrdinal()
    {
        VerifyComparer(StringComparer.Ordinal, false);
        VerifyComparer(StringComparer.OrdinalIgnoreCase, true);
    }

    private static void VerifyComparer(StringComparer sc, bool ignoreCase)
    {
        String s1 = "Hello";
        String s1a = "Hello";
        String s1A = "HELLO";
        String s2 = "There";

        bool b;
        int i;

        b = sc.Equals(s1, s1a);
        Assert.True(b);
        b = ((IEqualityComparer)sc).Equals(s1, s1a);
        Assert.True(b);

        i = sc.Compare(s1, s1a);
        Assert.True(i == 0);
        i = ((IComparer)sc).Compare(s1, s1a);
        Assert.True(i == 0);

        b = sc.Equals(s1, s1);
        Assert.True(b);
        b = ((IEqualityComparer)sc).Equals(s1, s1);
        Assert.True(b);
        i = sc.Compare(s1, s1);
        Assert.True(i == 0);
        i = ((IComparer)sc).Compare(s1, s1);
        Assert.True(i == 0);

        b = sc.Equals(s1, s2);
        Assert.False(b);
        b = ((IEqualityComparer)sc).Equals(s1, s2);
        Assert.False(b);
        i = sc.Compare(s1, s2);
        Assert.True(i < 0);
        i = ((IComparer)sc).Compare(s1, s2);
        Assert.True(i < 0);

        b = sc.Equals(s1, s1A);
        if (ignoreCase)
            Assert.True(b);
        else
            Assert.False(b);

        b = ((IEqualityComparer)sc).Equals(s1, s1A);
        if (ignoreCase)
            Assert.True(b);
        else
            Assert.False(b);

        i = sc.Compare(s1, s1A);
        if (ignoreCase)
            Assert.True(i == 0);
        else
            Assert.True(i != 0);

        i = ((IComparer)sc).Compare(s1, s1A);
        if (ignoreCase)
            Assert.True(i == 0);
        else
            Assert.True(i != 0);
    }
}

