// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This is regression test for VSW 341477
// we were getting an assert failure due to using non-ASCII characters.

using System;

class Test
{
	
	static int Main(string[] args)
	{
		Hello<string> mystr = new Hello<string>("PASS");

		mystr.InstanceMethod<A>();

		return 100;
		
	}
}

public class A
{
	public A() {}
}

public class Hello<li??>
{
	public li?? a;		
	public Hello (li?? t)
	{
		a = t;
		Console.WriteLine (a.ToString ());
	}

	public ???? InstanceMethod<????> () where ???? : new()
	{
		return new ????();
		
	}
}

