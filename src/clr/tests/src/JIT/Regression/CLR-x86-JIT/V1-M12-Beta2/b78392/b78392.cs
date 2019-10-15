// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
