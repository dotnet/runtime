// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//This pattern of interface implementation caused a buffer overflow and caused an AV (see bug DEV10_526434)

using System;
using Xunit;

public class HelloWorld
{
    [Fact]
    public static void TestEntryPoint()
    {
        C<object> c = new C<object>();

        Console.WriteLine("Pass: {0}", c);
    }
}

interface K<T> { void Print(); }
class C<T> : J<string, T>, K<T>
{
    public virtual void Print() { }
    public virtual void PrintJ() { }
}

interface I1<T> { void Print(); }
interface I2<T> { void Print(); }
interface I3<T> { void Print(); }
interface I4<T> { void Print(); }
interface I5<T> { void Print(); }
interface I6<T> { void Print(); }
interface J<T, U> : I1<T>, I2<T>, I3<T>, I4<T>, I5<T>, I6<T>
{
    void PrintJ();
}
