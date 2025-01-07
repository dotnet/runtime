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

[StructLayout(LayoutKind.Sequential)]
public class GenInt : GenBase<int>
{	
	public void VerifyLayout()
	{
		Test_Base01b_seq.Eval(_int0 == 0);
		Test_Base01b_seq.Eval(_int1 == int.MaxValue) ;
		Test_Base01b_seq.Eval(_double0 == 0) ;
		Test_Base01b_seq.Eval(_double1 == double.MaxValue) ;
		Test_Base01b_seq.Eval(base._string0.Equals("string0"));
		Test_Base01b_seq.Eval(base._string1.Equals("string1"));
		Test_Base01b_seq.Eval(_Guid0 == new Guid());
		Test_Base01b_seq.Eval(_Guid1 == new Guid(1,2,3,4,5,6,7,8,9,10,11));	
	}
}

[StructLayout(LayoutKind.Sequential)]
public class GenDouble: GenBase<double>
{	
	public void VerifyLayout()
	{
		Test_Base01b_seq.Eval(_int0 == 0);
		Test_Base01b_seq.Eval(_int1 == int.MaxValue) ;
		Test_Base01b_seq.Eval(_double0 == 0) ;
		Test_Base01b_seq.Eval(_double1 == double.MaxValue) ;
		Test_Base01b_seq.Eval(base._string0.Equals("string0"));
		Test_Base01b_seq.Eval(base._string1.Equals("string1"));
		Test_Base01b_seq.Eval(_Guid0 == new Guid());
		Test_Base01b_seq.Eval(_Guid1 == new Guid(1,2,3,4,5,6,7,8,9,10,11));	
	}
}

[StructLayout(LayoutKind.Sequential)]
public class GenString : GenBase<String>
{	
	public void VerifyLayout()
	{
		Test_Base01b_seq.Eval(_int0 == 0);
		Test_Base01b_seq.Eval(_int1 == int.MaxValue) ;
		Test_Base01b_seq.Eval(_double0 == 0) ;
		Test_Base01b_seq.Eval(_double1 == double.MaxValue) ;
		Test_Base01b_seq.Eval(base._string0.Equals("string0"));
		Test_Base01b_seq.Eval(base._string1.Equals("string1"));
		Test_Base01b_seq.Eval(_Guid0 == new Guid());
		Test_Base01b_seq.Eval(_Guid1 == new Guid(1,2,3,4,5,6,7,8,9,10,11));	
	}
}

[StructLayout(LayoutKind.Sequential)]
public class GenObject : GenBase<object>
{	
	public void VerifyLayout()
	{
		Test_Base01b_seq.Eval(_int0 == 0);
		Test_Base01b_seq.Eval(_int1 == int.MaxValue) ;
		Test_Base01b_seq.Eval(_double0 == 0) ;
		Test_Base01b_seq.Eval(_double1 == double.MaxValue) ;
		Test_Base01b_seq.Eval(base._string0.Equals("string0"));
		Test_Base01b_seq.Eval(base._string1.Equals("string1"));
		Test_Base01b_seq.Eval(_Guid0 == new Guid());
		Test_Base01b_seq.Eval(_Guid1 == new Guid(1,2,3,4,5,6,7,8,9,10,11));	
	}
}

[StructLayout(LayoutKind.Sequential)]
public class GenGuid : GenBase<Guid>
{	
	public void VerifyLayout()
	{
		Test_Base01b_seq.Eval(_int0 == 0);
		Test_Base01b_seq.Eval(_int1 == int.MaxValue) ;
		Test_Base01b_seq.Eval(_double0 == 0) ;
		Test_Base01b_seq.Eval(_double1 == double.MaxValue) ;
		Test_Base01b_seq.Eval(base._string0.Equals("string0"));
		Test_Base01b_seq.Eval(base._string1.Equals("string1"));
		Test_Base01b_seq.Eval(_Guid0 == new Guid());
		Test_Base01b_seq.Eval(_Guid1 == new Guid(1,2,3,4,5,6,7,8,9,10,11));	
	}
}

[StructLayout(LayoutKind.Sequential)]
public class GenConstructedReference : GenBase<RefX1<int>>
{	
	public void VerifyLayout()
	{
		Test_Base01b_seq.Eval(_int0 == 0);
		Test_Base01b_seq.Eval(_int1 == int.MaxValue) ;
		Test_Base01b_seq.Eval(_double0 == 0) ;
		Test_Base01b_seq.Eval(_double1 == double.MaxValue) ;
		Test_Base01b_seq.Eval(base._string0.Equals("string0"));
		Test_Base01b_seq.Eval(base._string1.Equals("string1"));
		Test_Base01b_seq.Eval(_Guid0 == new Guid());
		Test_Base01b_seq.Eval(_Guid1 == new Guid(1,2,3,4,5,6,7,8,9,10,11));	
	}
}

[StructLayout(LayoutKind.Sequential)]
public class GenConstructedValue: GenBase<ValX1<string>>
{	
	public void VerifyLayout()
	{
		Test_Base01b_seq.Eval(_int0 == 0);
		Test_Base01b_seq.Eval(_int1 == int.MaxValue) ;
		Test_Base01b_seq.Eval(_double0 == 0) ;
		Test_Base01b_seq.Eval(_double1 == double.MaxValue) ;
		Test_Base01b_seq.Eval(base._string0.Equals("string0"));
		Test_Base01b_seq.Eval(base._string1.Equals("string1"));
		Test_Base01b_seq.Eval(_Guid0 == new Guid());
		Test_Base01b_seq.Eval(_Guid1 == new Guid(1,2,3,4,5,6,7,8,9,10,11));	
	}
}

[StructLayout(LayoutKind.Sequential)]
public class GenInt1DArray : GenBase<int[]>
{	
	public void VerifyLayout()
	{
		Test_Base01b_seq.Eval(_int0 == 0);
		Test_Base01b_seq.Eval(_int1 == int.MaxValue) ;
		Test_Base01b_seq.Eval(_double0 == 0) ;
		Test_Base01b_seq.Eval(_double1 == double.MaxValue) ;
		Test_Base01b_seq.Eval(base._string0.Equals("string0"));
		Test_Base01b_seq.Eval(base._string1.Equals("string1"));
		Test_Base01b_seq.Eval(_Guid0 == new Guid());
		Test_Base01b_seq.Eval(_Guid1 == new Guid(1,2,3,4,5,6,7,8,9,10,11));	
	}
}

[StructLayout(LayoutKind.Sequential)]
public class GenString2DArray : GenBase<string[,]>
{	
	public void VerifyLayout()
	{
		Test_Base01b_seq.Eval(_int0 == 0);
		Test_Base01b_seq.Eval(_int1 == int.MaxValue) ;
		Test_Base01b_seq.Eval(_double0 == 0) ;
		Test_Base01b_seq.Eval(_double1 == double.MaxValue) ;
		Test_Base01b_seq.Eval(base._string0.Equals("string0"));
		Test_Base01b_seq.Eval(base._string1.Equals("string1"));
		Test_Base01b_seq.Eval(_Guid0 == new Guid());
		Test_Base01b_seq.Eval(_Guid1 == new Guid(1,2,3,4,5,6,7,8,9,10,11));	
	}
}

[StructLayout(LayoutKind.Sequential)]
public class GenIntJaggedArray : GenBase<int[][]>
{	
	public void VerifyLayout()
	{
		Test_Base01b_seq.Eval(_int0 == 0);
		Test_Base01b_seq.Eval(_int1 == int.MaxValue) ;
		Test_Base01b_seq.Eval(_double0 == 0) ;
		Test_Base01b_seq.Eval(_double1 == double.MaxValue) ;
		Test_Base01b_seq.Eval(base._string0.Equals("string0"));
		Test_Base01b_seq.Eval(base._string1.Equals("string1"));
		Test_Base01b_seq.Eval(_Guid0 == new Guid());
		Test_Base01b_seq.Eval(_Guid1 == new Guid(1,2,3,4,5,6,7,8,9,10,11));	
	}
}


public class Test_Base01b_seq
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
		new GenInt().VerifyLayout();
		new GenDouble().VerifyLayout();
		new GenString().VerifyLayout();
		new GenObject().VerifyLayout();
		new GenGuid().VerifyLayout();
		new GenConstructedReference().VerifyLayout();
		new GenConstructedValue().VerifyLayout();
		new GenInt1DArray().VerifyLayout();
		new GenString2DArray().VerifyLayout();
		new GenIntJaggedArray().VerifyLayout();
		
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
