// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

public class Foo
{
    static public int Main(string[] args)
    {
        double inf = Double.PositiveInfinity;
        System.Console.WriteLine(System.Math.Atan2(inf, inf));
        System.Console.WriteLine(System.Math.Atan2(inf, -inf));
        System.Console.WriteLine(System.Math.Atan2(-inf, inf));
        System.Console.WriteLine(System.Math.Atan2(-inf, -inf));
        return 100;
    }
}
