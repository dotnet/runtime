// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
public class foo
{

#pragma warning disable 0414
    public static sbyte a, b, c;
#pragma warning restore 0414

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

        sbyte b = 3;

        c = (sbyte)(a / b);
    }

}
