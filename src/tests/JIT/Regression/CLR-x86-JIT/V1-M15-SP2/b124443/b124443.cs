// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;
delegate void TestDelegate();
public class ArrayBounds
{
	internal static void f1a()
	{
		int [] a = new int[4];
		for (int i=0; i < a.Length; --i)
		{
			a[i]=1; 
		}
	}
	internal static void f2a()
	{
		int [] a = new int[4];
		for (int i=0; i < a.Length; --i)
		{
			int b = a[i];
		}
	}
	internal static void f3a()
	{
		int [] a = new int[4];
		for (int i=0; i < a.Length; --i)
		{
			Console.WriteLine(a[i]);
		}
	}
	internal static void f4a()
	{
		int [] a = new int[4];
		for (int i=0; i < a.Length; a[i]=i,--i)
		{
			// empty
		}
	}
	
	// ++i
	internal static void f1b()
	{
		int [] a = new int[4];
		for (int i=0; i <= a.Length; ++i)
		{
			a[i]=1; 
		}
	}
	internal static void f2b()
	{
		int [] a = new int[4];
		for (int i=0; i <= a.Length; ++i)
		{
			int b = a[i];
		}
	}
	internal static void f3b()
	{
		int [] a = new int[4];
		for (int i=0; i <= a.Length; ++i)
		{
			Console.WriteLine(a[i]);
		}
	}
	internal static void f4b()
	{
		int [] a = new int[4];
		for (int i=0; i <= a.Length; a[i]=i,++i)
		{
			// empty
		}
	}

	// ++i, 0x7fff
	internal static void f1c()
	{
		bool [] a = new bool[0x7fff];
		for (short i=0x7ff0; i < a.Length+1; ++i)
		{
			a[i]=true; 
		}
	}
	internal static void f2c()
	{
		bool [] a = new bool[0x7fff];
		for (short i=0x7ff0; i < a.Length+1; ++i)
		{
			bool b = a[i];
		}
	}
	internal static void f3c()
	{
		bool [] a = new bool[0x7fff];
		for (short i=0x7ffe; i < a.Length+1; ++i)
		{
			Console.WriteLine(a[i]);
		}
	}
	internal static void f4c()
	{
		bool [] a = new bool[0x7fff];
		for (short i=0x7ff0; i < a.Length+1; ++i)
		{
			a[i] = true;
		}
	}

	static int RunTests(TestDelegate d)
	{
		try
		{
			Console.Write(d.Method.Name + ": ");
			d();
		}
		catch (IndexOutOfRangeException)
		{
			Console.WriteLine("IndexOutOfRangeException caught as expected");
			return 100;
		}
		catch (Exception e)
		{
			Console.WriteLine("FAILED");
			Console.WriteLine(e);
			return 1;
		}

		Console.WriteLine("PASSED");
		return 100;
	}

	[Fact]
	public static int TestEntryPoint()
	{
		if (RunTests(new TestDelegate(f1a))!=100) return 1;
		if (RunTests(new TestDelegate(f2a))!=100) return 1;
		if (RunTests(new TestDelegate(f3a))!=100) return 1;
		if (RunTests(new TestDelegate(f4a))!=100) return 1;
		if (RunTests(new TestDelegate(f1b))!=100) return 1;
		if (RunTests(new TestDelegate(f2b))!=100) return 1;
		if (RunTests(new TestDelegate(f3b))!=100) return 1;
		if (RunTests(new TestDelegate(f4b))!=100) return 1;
		if (RunTests(new TestDelegate(f1c))!=100) return 1;
		if (RunTests(new TestDelegate(f2c))!=100) return 1;
		if (RunTests(new TestDelegate(f3c))!=100) return 1;
		if (RunTests(new TestDelegate(f4c))!=100) return 1;

		Console.WriteLine();
		Console.WriteLine("PASSED");
		return 100;
	}
}
