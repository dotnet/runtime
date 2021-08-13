// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
public class TestDependency
{
    public static int IntAdd(int a, int b)
    {
        System.Console.WriteLine("rodei o intadd");
        int c = a + b;
        return c;
    }
}
public class SomeAttribute : Attribute
{
    public SomeAttribute(SomeEnum something)
    {
        Something = something;
    }

    public SomeEnum Something { get; }
}

[Some(SomeEnum.Something)] // <-- comment this to make blazor WASM debugging work
public class SomeClass
{

}
