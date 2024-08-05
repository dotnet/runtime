// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// test that .cctor of NotInlined and Inlined (class/struct) is called when Foo.Meth_In and Foo_Meth_NotIn is invoked.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

public class Foo<T>
{
	public static void Meth_In()
	{
		// NotInlined.NotInlinedMeth is not inlined
		NotInlined<int>.NotInlinedMeth();
	}

	public static void Meth_NotIn()
	{
		// Inlined.InlinedMeth is  inlined
		Inlined<int>.InlinedMeth();
	}

	public static void ValMeth_In()
	{
		// NotInlinedVal.NotInlinedValMeth is not inlined
		NotInlinedVal<int>.NotInlinedValMeth();
	}

	public static void ValMeth_NotIn()
	{
		// InlinedVal.InlinedValMeth is  inlined
		InlinedVal<int>.InlinedValMeth();
	}
}

public class NotInlined<T>
{

	static NotInlined()
	{
		Console.WriteLine("Inside NotInlined::.cctor");
		File.WriteAllText("notinlined.txt", "inside .cctor");
	}

	[MethodImpl(MethodImplOptions.NoInlining)] 
	public static void NotInlinedMeth()
	{
	}
}


public class Inlined<T>
{

	static Inlined()
	{
		Console.WriteLine("Inside Inlined::.cctor");
		File.WriteAllText("inlined.txt", "inside .cctor");
	}

	public static void InlinedMeth()
	{
	}
}


public struct NotInlinedVal<T>
{

	static NotInlinedVal()
	{
		Console.WriteLine("Inside NotInlinedVal::.cctor");
		File.WriteAllText("notinlinedval.txt", "inside .cctor");
	}

	[MethodImpl(MethodImplOptions.NoInlining)] 
	public static void NotInlinedValMeth()
	{
	}
}


public struct InlinedVal<T>
{

	static InlinedVal()
	{
		Console.WriteLine("Inside InlinedVal::.cctor");
		File.WriteAllText("inlinedval.txt", "inside .cctor");
	}

	public static void InlinedValMeth()
	{
	}
}


public class Test_GenTypeInlined
{
	[Fact]
	public static int TestEntryPoint()
	{
		Foo<int>.Meth_In();
		Foo<char>.Meth_NotIn();

		Foo<InlinedVal<int>>.ValMeth_In();
		Foo<NotInlined<bool>>.ValMeth_NotIn();

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
