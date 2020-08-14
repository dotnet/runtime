// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

class foo
{

    public static short a, b, c;

    public static int Main()
    {

        a = 19;
        b = 3;

        div();

        return 100;
    }

    public static void div()
    {

        c = (short)(a / b);
    }

}
