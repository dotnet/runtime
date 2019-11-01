// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// test that .cctor of NotInlined and Inlined (class/struct) is called when Foo.Meth_In and Foo_Meth_NotIn 
// is invoked in multinested scenario.

using System;
using System.IO;
using System.Runtime.CompilerServices;

public class Bar
{
	public static void BarMeth_In()
	{
		Foo.Meth_In();
	}

	public static void BarMeth_NotIn()
	{
		Foo.Meth_NotIn();
	}

	public static void BarValMeth_In()
	{
		Foo.ValMeth_In();
	}

	public static void BarValMeth_NotIn()
	{
		Foo.ValMeth_NotIn();
	}

}

		
		
public class Foo
{
	public static void Meth_In()
	{
		// NotInlined.NotInlinedMeth is not inlined
		NotInlined.NotInlinedMeth<int>();
	}

	public static void Meth_NotIn()
	{
		// Inlined.InlinedMeth is  inlined
		Inlined.InlinedMeth<char>();
	}

	public static void ValMeth_In()
	{
		// NotInlinedVal.NotInlinedValMeth is not inlined
		NotInlinedVal.NotInlinedValMeth<String>();
	}

	public static void ValMeth_NotIn()
	{
		// InlinedVal.InlinedValMeth is  inlined
		InlinedVal.InlinedValMeth<Bar>();
	}
}



public class NotInlined
{

	static NotInlined()
	{
		Console.WriteLine("Inside NotInlined::.cctor");
		File.WriteAllText("notinlined.txt", "inside .cctor");
	}

	[MethodImpl(MethodImplOptions.NoInlining)] 
	public static void NotInlinedMeth<T>()
	{
	}
}


public class Inlined
{

	static Inlined()
	{
		Console.WriteLine("Inside Inlined::.cctor");
		File.WriteAllText("inlined.txt", "inside .cctor");
	}

	public static void InlinedMeth<T>()
	{
	}
}


public struct NotInlinedVal
{

	static NotInlinedVal()
	{
		Console.WriteLine("Inside NotInlinedVal::.cctor");
		File.WriteAllText("notinlinedval.txt", "inside .cctor");
	}

	[MethodImpl(MethodImplOptions.NoInlining)] 
	public static void NotInlinedValMeth<T>()
	{
	}
}


public struct InlinedVal
{

	static InlinedVal()
	{
		Console.WriteLine("Inside InlinedVal::.cctor");
		File.WriteAllText("inlinedval.txt", "inside .cctor");
	}

	public static void InlinedValMeth<T>()
	{
	}
}


public class Test
{
	public static int Main()
	{
		Bar.BarMeth_In();
		Bar.BarMeth_NotIn();

		Bar.BarValMeth_In();
		Bar.BarValMeth_NotIn();

		if (!File.Exists("inlined.txt") || !File.Exists("notinlined.txt") || !File.Exists("inlinedval.txt") || !File.Exists("notinlinedval.txt") )
		{
			Console.WriteLine("FAIL: Cctor wasn't called");
			return 101;
		}
		else
		{
			Console.WriteLine("PASS: Cctor was called");
			File.Delete("inlined.txt");
			File.Delete("notinlined.txt");
			File.Delete("inlinedval.txt");
			File.Delete("notinlinedval.txt");
			return 100;
		}
		
	}
}
