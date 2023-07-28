// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public class AA
{
    public static sbyte Static2()
    { return (new sbyte[1])[0]; }
    public static int Static4(sbyte param1)
    { return (((byte)9u) - AA.Static2()); }
    public static byte Static5()
    { return ((byte[])((Array)null))[AA.Static4(AA.Static2())]; }
    static void Main1()
    { Static5(); }
    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            Main1();
        }
        catch (NullReferenceException)
        {
            return 100;
        }
        return 101;
    }
}
