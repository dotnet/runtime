// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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