// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//Type load failed when compiling a class derived from a class that implicitly implements a generic method as a virtual method. 
//The base class and the interface are in a separate module
//DDB186874: the output was:
//     "Unhandled Exception: System.TypeLoadException: Method 'MyBase.Print' on type 'MyDerived' from assembly 'Test, 
//     Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' tried to implicitly implement an interface method with 
//     weaker type parameter constraints."




using System;


public class MyDerived : MyBase, I
{
}

class MyTest
{
    public static int Main()
    {
        I I1 = new MyDerived();

        System.Console.WriteLine("I1.Print<object>: " + I1.Print<object>());

        return 100;
    }
}
