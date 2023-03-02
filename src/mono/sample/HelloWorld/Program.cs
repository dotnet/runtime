// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

interface IDefault
{
    public void Method1()
    {
        Console.WriteLine("Interface Method1");
    }

    public object Method2()
    {
        Console.WriteLine("Interface Method2");
        return null;
    }
}

abstract class ClassA : IDefault
{
    virtual public void Method1()
    {
        Console.WriteLine("ClassA Method 1");
    }
}

class ClassB : ClassA
{
    public virtual object Method2()
    {
        Console.WriteLine("ClassB Method2");
        return null;
    }
}

namespace HelloWorld
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            bool isMono = typeof(object).Assembly.GetType("Mono.RuntimeStructs") != null;
            Console.WriteLine($"Hello World {(isMono ? "from Mono!" : "from CoreCLR!")}");
            Console.WriteLine(typeof(object).Assembly.FullName);
            Console.WriteLine(System.Reflection.Assembly.GetEntryAssembly ());
            Console.WriteLine(System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);

            IDefault c = new ClassB();

            c.Method1();
            c.Method2();
            
        }
    }
}
