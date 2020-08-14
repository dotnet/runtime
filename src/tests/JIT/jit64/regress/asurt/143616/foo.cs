// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

public class foo
{
    public static int Main()
    {
        return bar.getX();
    }
}


public class bar
{
    static bar()
    {
        System.Console.WriteLine(": Executing class constructor of bar.");
        bar2.x = 100;
    }

    public static int getX()
    {
        int val = bar2.x;
        System.Console.WriteLine("bar2.x contains: " + val);
        return val;
    }
}


public class bar2
{
    static public int x;

    static bar2()
    {
        System.Console.WriteLine(": Executing class constructor of bar2.");
        bar2.x = -1;
    }
}
