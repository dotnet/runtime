// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//generic classes A and B

public class HelloWorld
{
    public static int Main()
    {
        try { B<string> b = new B<string>(GetName()); }
        catch (System.Exception)
        {
            System.Console.WriteLine("PASS");
            return 100;
        }
        System.Console.WriteLine("FAIL");
        return -1;
    }
    public static string GetName() { throw new System.Exception(); }
}

public class B<T> : A<T>
{
    public B(string name)
    {
        System.Console.WriteLine("Creating object B({0})", name);
    }
}
