// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
class foo
{
    static int Main()
    {
        byte[,] Param = new byte[2, 2];
        Param[0, 0] = 1;
        Param[1, 1] = 2;

        byte[,] Stuff = new byte[3, 3];
        Stuff[Param[0, 0], Param[1, 1]] = 1;
        Console.WriteLine(Stuff[1, 2]);
        return 100;
    }
}