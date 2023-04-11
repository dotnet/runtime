// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;
public class bug1
{
    public struct VT1
    {
        public int a0;
        public int a1;
        public double a3;
        public long a9;
    }
    public static VT1 vtstatic = new VT1();
    public static int f()
    {
        return Convert.ToInt32(Convert.ToInt32(Convert.ToInt32(vtstatic.a9 / 3 + vtstatic.a3)) % (Convert.ToInt32(vtstatic.a1 * vtstatic.a0) - Convert.ToInt32(Convert.ToInt32(2) % Convert.ToInt32(Convert.ToInt32(2) % (Convert.ToInt32(9))))));
    }
    [Fact]
    public static int TestEntryPoint()
    {
        vtstatic.a0 = 3;
        vtstatic.a1 = 2;
        vtstatic.a3 = 6;
        vtstatic.a9 = 1;
        f();
        return 100;
    }
}
