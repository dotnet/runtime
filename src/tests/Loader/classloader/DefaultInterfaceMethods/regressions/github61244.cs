// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

class Program
{
    static int Main(string[] args)
    {
        new TestClass();
        return 100;
    }
}

public interface FirstInterface<T>
{
    void SomeFunc1(T someParam);
}

public interface SecondInterface<T1, T2> : FirstInterface<T1>
{
    void FirstInterface<T1>.SomeFunc1(T1 someParam) => Console.WriteLine ("Test");
}

public class TestClass : SecondInterface<int, string>{}
