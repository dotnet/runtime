// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
