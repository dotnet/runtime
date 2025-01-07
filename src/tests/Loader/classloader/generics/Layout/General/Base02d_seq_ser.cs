// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// regression test for 245437

using System;
using System.Runtime.InteropServices;
using Xunit;




[StructLayout(LayoutKind.Sequential)]
public class GenBase<T>
{
	public T Fld10;
	
	public int _int0 = 0;
	public double _double0 = 0;
	public string _string0 = "string0";
	public Guid _Guid0 = new Guid();
	
	public T Fld11;

	public int _int1 = int.MaxValue;
	public double _double1 = double.MaxValue;
	public string _string1 = "string1";
	public Guid _Guid1 = new Guid(1,2,3,4,5,6,7,8,9,10,11);

	public T Fld12;

}


[StructLayout(LayoutKind.Explicit)]
public class Gen<T> : GenBase<T>
{
	[FieldOffset(0)]public T sFld10;
	
	[FieldOffset(16)]public int _sint0 = 0;
	[FieldOffset(24)]public double _sdouble0 = 0;
	[FieldOffset(32)]public string _sstring0 = "string0";
	[FieldOffset(40)]public Guid _sGuid0 = new Guid();
	
	[FieldOffset(56)]public T sFld11;

	[FieldOffset(72)]public int _sint1 = int.MaxValue;
	[FieldOffset(80)]public double _sdouble1 = double.MaxValue;
	[FieldOffset(88)]public string _sstring1 = "string1";
	[FieldOffset(96)]public Guid _sGuid1 = new Guid(1,2,3,4,5,6,7,8,9,10,11);

	[FieldOffset(112)]public T sFld12;
	
}

public class Test_Base02d_seq_ser
{	
	public static void RunTest1()
	{
		new Gen<int>();
	}

	public static void RunTest2()
	{
		new Gen<double>();
	}

	public static void RunTest3()
	{
		new Gen<string>();
	}
	
	[Fact]
	public static int TestEntryPoint()
	{
		bool result = true;
		try
		{	
			RunTest1();
			result = false;
		}
		catch (TypeLoadException)
		{
			// expected
		}

		try
		{
			RunTest2();
			result = false;
		}
		catch (TypeLoadException)
		{
			// expected			
		}

		
		try
		{
			RunTest3();
			result = false;
		}
		catch (TypeLoadException)
		{
			// expected
		}
		


		if (result)
		{
			Console.WriteLine("Test Passed");
			return 100;
		}
		else
		{
			Console.WriteLine("Test Failed");
			return 1;
		}
	}
		
}
