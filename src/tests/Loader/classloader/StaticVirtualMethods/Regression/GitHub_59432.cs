// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;
using TestLibrary;

public class Program
{
    [ActiveIssue("needs triage", TestPlatforms.tvOS)]
    [Fact]
    public static void TestEntryPoint()
    {
        Console.WriteLine("Start");
        SomeClass someInstance = new();
        CalledMethod();
        Console.WriteLine("Done");
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
