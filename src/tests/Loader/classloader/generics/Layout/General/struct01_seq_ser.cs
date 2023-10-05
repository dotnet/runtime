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


[StructLayout(LayoutKind.Sequential)]
public struct Gen<T,U>
{
	public T Fld10;

	public int _int0;
	public double _double0; 
	public string _string0;
	public Guid _Guid0; 

	public U Fld20;
	
	public int _int1;
	public double _double1; 
	public string _string1;
	public Guid _Guid1; 
	
	public T Fld11;
	public U Fld21;

	public int _int2;
	public double _double2; 
	public string _string2;
	public Guid _Guid2; 

	public T Fld12;

	public int _int3;
	public double _double3; 
	public string _string3;
	public Guid _Guid3; 
	
	public U Fld22;

	public void VerifyLayout()
	{
		 _int0 = 0;
		 _double0 = 0;
		_string0 = "string0";
		 _Guid0 = new Guid();
	
		 _int1 = 1;
		 _double1 = 1;
		_string1 = "string1";
		_Guid1 = new Guid(1,2,3,4,5,6,7,8,9,10,11);
	
		_int2 = int.MaxValue;
		 _double2 = double.MaxValue;
		_string2 = "string2";
		_Guid2 = new Guid(11,21,31,41,51,61,71,81,91,101,111);

		_int3 = int.MinValue;
		 _double3 = double.MinValue;
		_string3 = "string3";
		_Guid3 = new Guid(12,22,32,42,52,62,72,82,92,102,112);
	
		Test_struct01_seq_ser.Eval(_int0 == 0);
		Test_struct01_seq_ser.Eval(_int1 == 1);
		Test_struct01_seq_ser.Eval(_int2 == int.MaxValue) ;
		Test_struct01_seq_ser.Eval(_int3 == int.MinValue) ;
		Test_struct01_seq_ser.Eval(_double0 == 0) ;
		Test_struct01_seq_ser.Eval(_double1== 1) ;
		Test_struct01_seq_ser.Eval(_double2 == double.MaxValue) ;
		Test_struct01_seq_ser.Eval(_double3 == double.MinValue) ;
		Test_struct01_seq_ser.Eval(_string0.Equals("string0"));
		Test_struct01_seq_ser.Eval(_string1.Equals("string1"));
		Test_struct01_seq_ser.Eval(_string2.Equals("string2"));
		Test_struct01_seq_ser.Eval(_string3.Equals("string3"));
		Test_struct01_seq_ser.Eval(_Guid0 == new Guid());
		Test_struct01_seq_ser.Eval(_Guid1 == new Guid(1,2,3,4,5,6,7,8,9,10,11));
		Test_struct01_seq_ser.Eval(_Guid2 == new Guid(11,21,31,41,51,61,71,81,91,101,111));	
		Test_struct01_seq_ser.Eval(_Guid3 == new Guid(12,22,32,42,52,62,72,82,92,102,112));	
	}
}

public class Test_struct01_seq_ser
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
		new Gen<int,int>().VerifyLayout();
		new Gen<int,double>().VerifyLayout();
		new Gen<int,string>().VerifyLayout();
		new Gen<int,object>().VerifyLayout();
		new Gen<int,Guid>().VerifyLayout();
		new Gen<int,RefX1<int>>().VerifyLayout();
		new Gen<int,RefX1<string>>().VerifyLayout();
		new Gen<int,RefX1<int[][,,,][]>>().VerifyLayout();
		new Gen<int,ValX1<int>>().VerifyLayout();
		new Gen<int,ValX1<string>>().VerifyLayout();
		new Gen<int,ValX1<int[][,,,][]>>().VerifyLayout();
		
		new Gen<double,int>().VerifyLayout();	
		new Gen<double,double>().VerifyLayout();
		new Gen<double,string>().VerifyLayout();	
		new Gen<double,object>().VerifyLayout();
		new Gen<double,Guid>().VerifyLayout();
		new Gen<double,RefX1<double>>().VerifyLayout();
		new Gen<double,RefX1<string>>().VerifyLayout();
		new Gen<double,RefX1<double[][,,,][]>>().VerifyLayout();
		new Gen<double,ValX1<double>>().VerifyLayout();
		new Gen<double,ValX1<string>>().VerifyLayout();
		new Gen<double,ValX1<double[][,,,][]>>().VerifyLayout();

		new Gen<string,int>().VerifyLayout();
		new Gen<string,double>().VerifyLayout();
		new Gen<string,string>().VerifyLayout();
		new Gen<string,object>().VerifyLayout();
		new Gen<string,Guid>().VerifyLayout();
		new Gen<string,RefX1<string>>().VerifyLayout();
		new Gen<string,RefX1<string>>().VerifyLayout();
		new Gen<string,RefX1<string[][,,,][]>>().VerifyLayout();
		new Gen<string,ValX1<string>>().VerifyLayout();
		new Gen<string,ValX1<string>>().VerifyLayout();
		new Gen<string,ValX1<string[][,,,][]>>().VerifyLayout();

		new Gen<object,int>().VerifyLayout();	
		new Gen<object,double>().VerifyLayout();
		new Gen<object,string>().VerifyLayout();	
		new Gen<object,object>().VerifyLayout();
		new Gen<object,Guid>().VerifyLayout();
		new Gen<object,RefX1<object>>().VerifyLayout();
		new Gen<object,RefX1<string>>().VerifyLayout();
		new Gen<object,RefX1<object[][,,,][]>>().VerifyLayout();
		new Gen<object,ValX1<object>>().VerifyLayout();
		new Gen<object,ValX1<string>>().VerifyLayout();
		new Gen<object,ValX1<object[][,,,][]>>().VerifyLayout();

		new Gen<Guid,int>().VerifyLayout();
		new Gen<Guid,double>().VerifyLayout();
		new Gen<Guid,string>().VerifyLayout();	
		new Gen<Guid,object>().VerifyLayout();
		new Gen<Guid,Guid>().VerifyLayout();
		new Gen<Guid,RefX1<Guid>>().VerifyLayout();
		new Gen<Guid,RefX1<string>>().VerifyLayout();
		new Gen<Guid,RefX1<Guid[][,,,][]>>().VerifyLayout();
		new Gen<Guid,ValX1<Guid>>().VerifyLayout();
		new Gen<Guid,ValX1<string>>().VerifyLayout();
		new Gen<Guid,ValX1<Guid[][,,,][]>>().VerifyLayout();

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
