// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

interface IFoo<in T>
{
    static virtual string DoStatic() => typeof(T).ToString();
}

interface IFoo2<in T>
{
    static abstract string DoStatic();
}

class Fooer<T> : IFoo2<T>
{
    public static string DoStatic() => typeof(T).ToString();
}

class Program : IFoo<object>
{
    static string CallStatic<T, U>() where T : IFoo<U> => T.DoStatic();
    static string CallStatic2<T, U>() where T : IFoo2<U> => T.DoStatic();

    static int Main()
    {
        string staticResult1 = CallStatic<Program, string>();
        Console.WriteLine("SVM call result #1: {0} (System.Object expected - using default interface implementation)", staticResult1);
        string staticResult2 = CallStatic2<Fooer<object>, string>();
        Console.WriteLine("SVM call result #2: {0} (System.Object expected - using implementation in a helper class)", staticResult2);
        return (staticResult1 == "System.Object" && staticResult2 == "System.Object" ? 100 : 101);
    }
}