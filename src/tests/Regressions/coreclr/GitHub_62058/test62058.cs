// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

public class Program
{
    private interface IFoo
    {
        bool IsValid { get; }
    }

    private class Foo : IFoo
    {
        public bool IsValid { get; set; }
    }

    public static int Main(string[] args)
    {
        bool warmup = new Foo().IsValid;
        CatchIgnore(() =>
        CatchRethrow(() =>
        {
            IFoo[] foos = {new Foo(), null};
            foreach (var foo in foos)
            {
                bool check = foo.IsValid;
            }
        }));

        return 100;
    }

    public static void CatchRethrow(Action action)
    {
        try
        {
            action.Invoke();
        }
        catch (Exception e)
        {
            Console.Out.WriteLine("catch");
            Console.Out.Flush();
            throw new Exception("catch", e);
        }
    }

    public static void CatchIgnore(Action action)
    {
        try
        {
            action.Invoke();
        }
        catch (Exception)
        {
            Console.Out.WriteLine("ignore");
            Console.Out.Flush();
        }
    }
}
