// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
public class ovf
{

    internal static void f()
    {

        uint x = 0xfffffffe;
        uint y = 0xfffffffe;

        checked
        {

            uint z = x * y;
        }

    }

    [Fact]
    public static int TestEntryPoint()
    {
        try { f(); }
        catch (System.OverflowException) { return 100; }
        return 1;
    }

}
