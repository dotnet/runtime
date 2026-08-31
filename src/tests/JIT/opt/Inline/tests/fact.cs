// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class Fact {
    static int factTR(int n, int a) {
        if (n <= 1) return a;
        return factTR(n - 1, a * n);
    }
    
    static int fact(int n) {
        return factTR(n, 1);
    }

    static int factR(int n) {
        if (n <= 1) return 1;
        return n * factR(n - 1);
    }

    static int factRx(int n, int a = 0, int b = 0, int c = 0) {
        if (n <= 1) return 1;
        return n * factRx(n - 1, a, b, c);
    }

    [Fact]
    public static int TestEntryPoint() {
        int resultTR = fact(6);
        int resultR = factR(6);
        int resultRx = factRx(6);
        Console.WriteLine("fact(6) = {0}", resultTR);
        Console.WriteLine("factR(6) = {0}", resultR);
        Console.WriteLine("factRx(6) = {0}", resultRx);
        bool good = resultTR == resultR && resultTR == resultRx && resultR == 720;
        return (good ? 100 : -1);
    }
}
