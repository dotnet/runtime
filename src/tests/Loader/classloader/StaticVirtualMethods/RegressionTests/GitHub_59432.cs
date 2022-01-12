// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

class Program
{
    static int Main()
    {
        Console.WriteLine("Start");
        SomeClass someInstance = new();
        CalledMethod();
        Console.WriteLine("Done");
        return 100;
    }

    static void CalledMethod()
    {
        SomeClass.SomeStaticMethodInClass();
    }
}

interface ISomeInterface
{
    static int SomeStaticMethod() => 42;
    static abstract int SomeStaticAbstractMethod();
}

class SomeClass : ISomeInterface
{
    static int ISomeInterface.SomeStaticAbstractMethod() => 42;
    public static int SomeStaticMethodInClass() => 42;
}
