// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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