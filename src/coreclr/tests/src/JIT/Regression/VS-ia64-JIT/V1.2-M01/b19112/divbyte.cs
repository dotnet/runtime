// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

class foo
{

#pragma warning disable 0414
    public static sbyte a, b, c;
#pragma warning restore 0414

    public static int Main()
    {

        a = 19;
        b = 3;

        div();

        return 100;
    }

    public static void div()
    {

        sbyte b = 3;

        c = (sbyte)(a / b);
    }

}
