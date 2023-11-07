// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//Introducing a new virtual method (A) in a derived class (Foo2) via newslot 
//when a virtual method of the same name exists in the base class (Foo1), 
//and calling that method on an object of a class (Foo3) which inherits from Foo2, 
//implements an interace IFoo, and does not itself implement method A, 
//results in a call to Foo1's method A instead of Foo2's method A.

using System;
using Xunit;

class Foo3 : Foo2, IFoo
{
}

class Bar3<T> : Bar2<T>, IBar<T>
{
}

public class MainClass
{
	[Fact]
	public static int TestEntryPoint()
	{
		bool ok = true;
        int result;

        Foo3 x3 = new Foo3();
        Foo2 x2 = new Foo3();
        Foo1 x1 = new Foo3();
        IFoo x = new Foo3();

        Bar3<int> y3 = new Bar3<int>();
        Bar2<int> y2 = new Bar3<int>();
        Bar1<int> y1 = new Bar3<int>();
        IBar<int> y = new Bar3<int>();

        if ((result = x3.A()) != 2) { Console.WriteLine("NOT OK: calling Foo3.A() did not return 2! it returned " + result); ok = false; }
        if ((result = x2.A()) != 2) { Console.WriteLine("NOT OK: calling Foo2.A() did not return 2! it returned " + result); ok = false; }
        if ((result = x1.A()) != 1) { Console.WriteLine("NOT OK: calling Foo1.A() did not return 1! it returned " + result); ok = false; }
        if ((result = x.A()) != 2) { Console.WriteLine("NOT OK: calling IFoo.A() did not return 2! it returned " + result); ok = false; }

        if ((result = y3.A<string>()) != 2) { Console.WriteLine("NOT OK: calling Bar3.A() did not return 2! it returned " + result); ok = false; }
        if ((result = y2.A<string>()) != 2) { Console.WriteLine("NOT OK: calling Bar2.A() did not return 2! it returned " + result); ok = false; }
        if ((result = y1.A<string>()) != 1) { Console.WriteLine("NOT OK: calling Bar1.A() did not return 1! it returned " + result); ok = false; }
        if ((result = y.A<string>()) != 2) { Console.WriteLine("NOT OK: calling IBar.A() did not return 2! it returned " + result); ok = false; }

        return ((ok) ? (100) : (-1));
	}
}
