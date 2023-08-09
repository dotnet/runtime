// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
public class foo
{

    public static short a, b, c;

    [Fact]
    public static int TestEntryPoint()
    {

        a = 19;
        b = 3;

        div();

        return 100;
    }

    internal static void div()
    {

        c = (short)(a / b);
    }

}
