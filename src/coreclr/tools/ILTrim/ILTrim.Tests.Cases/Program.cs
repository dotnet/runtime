// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable 649

class FirstType
{
    static void Method() { }
}

[Kept]
class Program
{
    static int Field;

    [Kept]
    static int FirstMethod() => 300;

    [Kept]
    static int Main()
    {
        return FirstMethod();
    }

    static void LastMethod(int someParameter) { }
}

class AnotherType
{
    static void Method() { }
}
