// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


namespace b19112b;

using Xunit;
public class foo
{

    public static short a, b, c;

    [OuterLoop]
    [Fact]
    public static void TestEntryPoint()
    {
        a = 19;
        b = 3;

        div();
    }

    internal static void div()
    {

        c = (short)(a / b);
    }

}
