// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
