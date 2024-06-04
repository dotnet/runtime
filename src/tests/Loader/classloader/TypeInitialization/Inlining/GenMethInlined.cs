// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// test that .cctor of NotInlined and Inlined (class/struct) is called when Foo.Meth_In and Foo_Meth_NotIn 
// is invoked.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

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
		Inlined.InlinedMeth<Foo>();
	}

	public static void ValMeth_In()
	{
		// NotInlinedVal.NotInlinedValMeth is not inlined
		NotInlinedVal.NotInlinedValMeth<char>();
	}

	public static void ValMeth_NotIn()
	{
		// InlinedVal.InlinedValMeth is  inlined
		InlinedVal.InlinedValMeth<NotInlined>();
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


public class Test_GenMethInlined
{
	[Fact]
	public static int TestEntryPoint()
	{
		Foo.Meth_In();
		Foo.Meth_NotIn();

		Foo.ValMeth_In();
		Foo.ValMeth_NotIn();

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
