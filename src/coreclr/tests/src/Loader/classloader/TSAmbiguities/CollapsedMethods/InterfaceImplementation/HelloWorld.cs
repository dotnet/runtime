// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

#pragma warning disable 1956 //this is exactly what this is testing.

class HelloWorld
{
    static int Main()
    {
        I i = (I)new A2_IntInt();
        string res1 = i.Print(1);
        J<int> ji = (J<int>)new A2_IntInt();
        string res2 = ji.Print(1);
        J<string> js = (J<string>)new A2_StringString();
        string res3 = js.Print("");

        Console.WriteLine(res1);
        Console.WriteLine(res2);
        Console.WriteLine(res3);

        if (res1 == "A.Print(U)" && res2 == "A.Print(U)" && res3 == "A.Print(U)")
            return 100;
        return -1;
    }
}

// ----------------------------------------------------------------------------
class A<T,U>
{
    public virtual string Print(T t) { return "A.Print(T)"; }
    public virtual string Print(U u) { return "A.Print(U)"; }
}

interface I
{
    string Print(int i);
}

interface J<T>
{
    string Print(T t);
}

class A2_IntInt : A<int,int>, I, J<int>
{
}

class A2_StringString : A<string,string>, J<string>
{
}
