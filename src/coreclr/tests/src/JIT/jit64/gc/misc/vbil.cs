// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

class C
{
    public int x = 5;
    public int y = 7;
}

class T
{
    public static bool GLOBAL = true;

    public static int Main()
    {
        C c = new C();

        if (GLOBAL)
        {
            System.Console.WriteLine(c.x);
        }
        else
        {
            System.Console.WriteLine(c.y);
        }
        return 100;
    }
}
