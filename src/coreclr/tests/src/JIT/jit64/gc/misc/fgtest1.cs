// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

class T
{
    public static int x = 4;

    public static int Main()
    {
        int exitcode = -1;
        try
        {
            Console.WriteLine(1);

            if (x == 5)
            {
                Console.WriteLine(2);
            }
            else
            {
                Console.WriteLine(3);
                throw new Exception();
            }

            Console.WriteLine(4);
        }
        catch
        {
            goto L;
        }

        Console.WriteLine(5);
        return exitcode;
    L:
        Console.WriteLine(6);
        exitcode = 100;
        return exitcode;
    }
}
