// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;
public class testout1
{
    [Fact]
    public static int TestEntryPoint()
    {
        Console.WriteLine("In First func (doing 5 iters - not unrolled)");
        Test1(2);
        Console.WriteLine("In Second func (doing 4 iters - is unrolled, and very slow)");
        Test2(2);
        Console.WriteLine("Done");
        return 100;
    }
    static int Test1(int Par)
    {
        int A, B, C, D, E, F, G, H, I;
        for (A = 0; A <= 5; A++)
            for (B = 0; B <= 5; B++)
                for (C = 0; C <= 5; C++)
                    for (D = 0; D <= 5; D++)
                        for (E = 0; E <= 5; E++)
                            for (F = 0; F <= 5; F++)
                                for (G = 0; G <= 5; G++)
                                    for (H = 0; H <= 5; H++)
                                        for (I = 0; I <= 5; I++)
                                            Par += A * 2 - B * 3;
        return Par;
    }
    static int Test2(int Par)
    {
        int A, B, C, D, E, F, G, H, I;
        for (A = 0; A <= 4; A++)
            for (B = 0; B <= 4; B++)
                for (C = 0; C <= 4; C++)
                    for (D = 0; D <= 4; D++)
                        for (E = 0; E <= 4; E++)
                            for (F = 0; F <= 4; F++)
                                for (G = 0; G <= 4; G++)
                                    for (H = 0; H <= 4; H++)
                                        for (I = 0; I <= 4; I++)
                                            Par += A * 2 - B * 3;
        return Par;
    }
}
