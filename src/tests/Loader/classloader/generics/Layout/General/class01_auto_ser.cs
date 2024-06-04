// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;



public struct ValX1<T> {}
public struct ValX2<T,U> {}
public struct ValX3<T,U,V>{}
public class RefX1<T> {}
public class RefX2<T,U> {}
public class RefX3<T,U,V>{}


[StructLayout(LayoutKind.Auto)]
public class Gen<T>
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
		
	public void VerifyLayout()
	{
		Test_class01_auto_ser.Eval(_int0 == 0);
		Test_class01_auto_ser.Eval(_int1 == int.MaxValue) ;
		Test_class01_auto_ser.Eval(_double0 == 0) ;
		Test_class01_auto_ser.Eval(_double1 == double.MaxValue) ;
		Test_class01_auto_ser.Eval(_string0.Equals("string0"));
		Test_class01_auto_ser.Eval(_string1.Equals("string1"));
		Test_class01_auto_ser.Eval(_Guid0 == new Guid());
		Test_class01_auto_ser.Eval(_Guid1 == new Guid(1,2,3,4,5,6,7,8,9,10,11));	
	}
}

public class Test_class01_auto_ser
{
	public static int counter = 0;
	public static bool result = true;
	public static void Eval(bool exp)
	{
		counter++;
		if (!exp)
		{
			result = exp;
			Console.WriteLine("Test Failed at location: " + counter);
		}
	
	}
	
	[Fact]
	public static int TestEntryPoint()
	{
		new Gen<int>().VerifyLayout();
		new Gen<double>().VerifyLayout();
		new Gen<string>().VerifyLayout();
		new Gen<object>().VerifyLayout();
		new Gen<Guid>().VerifyLayout();

		new Gen<int[]>().VerifyLayout();
		new Gen<double[,]>().VerifyLayout();
		new Gen<string[][][]>().VerifyLayout();
		new Gen<object[,,,]>().VerifyLayout();
		new Gen<Guid[][,,,][]>().VerifyLayout();

		new Gen<RefX1<int>[]>().VerifyLayout(); 
		new Gen<RefX1<double>[,]>().VerifyLayout();
		new Gen<RefX1<string>[][][]>().VerifyLayout();
		new Gen<RefX1<object>[,,,]>().VerifyLayout();
		new Gen<RefX1<Guid>[][,,,][]>().VerifyLayout();

		new Gen<ValX1<int>[]>().VerifyLayout(); 
		new Gen<ValX1<double>[,]>().VerifyLayout();
		new Gen<ValX1<string>[][][]>().VerifyLayout();
		new Gen<ValX1<object>[,,,]>().VerifyLayout();
		new Gen<ValX1<Guid>[][,,,][]>().VerifyLayout();


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
