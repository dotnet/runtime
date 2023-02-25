// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
class C
{
    public int x = 5;
    public int y = 7;
}

public class T
{
    public static bool GLOBAL = true;

    [Fact]
    public static int TestEntryPoint()
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
