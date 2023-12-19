// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
We are testing the following scenario:


interface I<T> {}

[in Class_ImplicitOverrideVirtualNewslot.cs]
class C<T> : I<T> { virtual newslot methods}

class D<T> : C<T> {virtual NEWSLOT methods} 

--> When invoking I::method<T>() we should get the parent's implementation.


*/

using System;
using Xunit;




public class CC1 : C1
{
	public new virtual int method1()
	{
		return 10;
	}
	
	public new virtual  int method2<T>()
	{
		return 20;
	}
}


public class CC2<T> : C2<T>
{
	public new virtual int method1()
	{
		return 30;
	}
	
	public new virtual int method2<U>()
	{
		return 40;
	}
}



public class CC3Int : C3Int
{
	public new virtual int method1()
	{
		return 50;
	}
	
	public new virtual int method2<U>()
	{
		return 60;
	}
}

public class CC3String : C3String
{
	public new virtual int method1()
	{
		return 50;
	}
	
	public new virtual int method2<U>()
	{
		return 60;
	}
}


public class CC3Object: C3Object
{
	public new virtual int method1()
	{
		return 50;
	}
	
	public new virtual int method2<U>()
	{
		return 60;
	}
}

public class CC4<T> : C4<T>
{
	public new virtual int method1()
	{
		return 70;
	}
	
	public new virtual int method2<U>()
	{
		return 80;
	}

}




public class Test_Class2_ImplicitOverrideVirtualNewslot
{

	public static int counter = 0;
	public static bool pass = true;
	
	public static void Eval(bool exp)
	{
		counter++;
		if (!exp)
		{
			pass = exp;
			Console.WriteLine("Test Failed at location: " + counter);
		}
	
	}
	
	

	public static void TestNonGenInterface_NonGenType()
	{
		I ic1 = new CC1();


		// since CC1's method doesn't have newslot, in both cases we should get CC1's method

		// TEST1: test generic virtual method
		Eval( (ic1.method2<int>().ToString()).Equals("2") );
		Eval( (ic1.method2<string>() .ToString()).Equals("2") );
		Eval( (ic1.method2<object>().ToString()).Equals("2") );
		Eval( (ic1.method2<A<int>>().ToString()).Equals("2") );
		Eval( (ic1.method2<S<object>>().ToString()).Equals("2") );
	}

	public static void TestNonGenInterface_GenType()
	{
		I ic2Int = new CC2<int>();
		I ic2Object = new CC2<object>();
		I ic2String = new CC2<string>();


		// TEST2: test non generic virtual method

 		Eval( (ic2Int.method1().ToString()).Equals("3") );
		Eval( (ic2String.method1().ToString()).Equals("3") );
		Eval( (ic2Object.method1().ToString()).Equals("3") );

		
		
		// TEST3: test generic virtual method

		Eval( (ic2Int.method2<int>().ToString()).Equals("4") );
		Eval( (ic2Int.method2<object>().ToString()).Equals("4") );
		Eval( (ic2Int.method2<string>().ToString()).Equals("4") );
		Eval( (ic2Int.method2<A<int>>().ToString()).Equals("4") );
		Eval( (ic2Int.method2<S<string>>().ToString()).Equals("4") );
		
		Eval( (ic2String.method2<int>().ToString()).Equals("4") );
		Eval( (ic2String.method2<object>().ToString()).Equals("4") );
		Eval( (ic2String.method2<string>().ToString()).Equals("4") );
		Eval( (ic2String.method2<A<int>>().ToString()).Equals("4") );
		Eval( (ic2String.method2<S<string>>().ToString()).Equals("4") );

		Eval( (ic2Object.method2<int>().ToString()).Equals("4") );
		Eval( (ic2Object.method2<object>().ToString()).Equals("4") );
		Eval( (ic2Object.method2<string>().ToString()).Equals("4") );
		Eval( (ic2Object.method2<A<int>>().ToString()).Equals("4") );
		Eval( (ic2Object.method2<S<string>>().ToString()).Equals("4") );
	
	}

	public static void TestGenInterface_NonGenType()
	{
		IGen<int> iIntc3 = new CC3Int();
		IGen<object> iObjectc3 = new CC3Object();
		IGen<string> iStringc3 = new CC3String();

		// TEST4: test non generic virtual method

		Eval( (iIntc3.method1().ToString()).Equals("5") );
		Eval( (iObjectc3.method1().ToString()).Equals("5") );
		Eval( (iStringc3.method1().ToString()).Equals("5") );
		
		
		// TEST5: test generic virtual method
		Eval( (iIntc3.method2<int>().ToString()).Equals("6") );
		Eval( (iIntc3.method2<object>().ToString()).Equals("6") );
		Eval( (iIntc3.method2<string>().ToString()).Equals("6") );
		Eval( (iIntc3.method2<A<int>>().ToString()).Equals("6") );
		Eval( (iIntc3.method2<S<string>>().ToString()).Equals("6") );
		
		Eval( (iStringc3.method2<int>().ToString()).Equals("6") );
		Eval( (iStringc3.method2<object>().ToString()).Equals("6") );
		Eval( (iStringc3.method2<string>().ToString()).Equals("6") );
		Eval( (iStringc3.method2<A<int>>().ToString()).Equals("6") );
		Eval( (iStringc3.method2<S<string>>().ToString()).Equals("6") );

		Eval( (iObjectc3.method2<int>().ToString()).Equals("6") );
		Eval( (iObjectc3.method2<object>().ToString()).Equals("6") );
		Eval( (iObjectc3.method2<string>().ToString()).Equals("6") );
		Eval( (iObjectc3.method2<A<int>>().ToString()).Equals("6") );
		Eval( (iObjectc3.method2<S<string>>().ToString()).Equals("6") );

	}

	public static void TestGenInterface_GenType()
	{
		IGen<int> iGenC4Int = new CC4<int>();
		IGen<object> iGenC4Object = new CC4<object>();
		IGen<string> iGenC4String = new CC4<string>();

		
		// TEST6: test non generic virtual method

		Eval( (iGenC4Int.method1().ToString()).Equals("7") );
		Eval( (iGenC4Object.method1().ToString()).Equals("7") );
		Eval( (iGenC4String.method1().ToString()).Equals("7") );

		// TEST7: test generic virtual method

		Eval( (iGenC4Int.method2<int>().ToString()).Equals("8") );
		Eval( (iGenC4Int.method2<object>().ToString()).Equals("8") );
		Eval( (iGenC4Int.method2<string>().ToString()).Equals("8") );
		Eval( (iGenC4Int.method2<A<int>>().ToString()).Equals("8") );
		Eval( (iGenC4Int.method2<S<string>>().ToString()).Equals("8") );
		
		Eval( (iGenC4String.method2<int>().ToString()).Equals("8") );
		Eval( (iGenC4String.method2<object>().ToString()).Equals("8") );
		Eval( (iGenC4String.method2<string>().ToString()).Equals("8") );
		Eval( (iGenC4String.method2<A<int>>().ToString()).Equals("8") );
		Eval( (iGenC4String.method2<S<string>>().ToString()).Equals("8") );

		Eval( (iGenC4Object.method2<int>().ToString()).Equals("8") );
		Eval( (iGenC4Object.method2<object>().ToString()).Equals("8") );
		Eval( (iGenC4Object.method2<string>().ToString()).Equals("8") );
		Eval( (iGenC4Object.method2<A<int>>().ToString()).Equals("8") );
		Eval( (iGenC4Object.method2<S<string>>().ToString()).Equals("8") );

	}


	
	[Fact]
	public static int TestEntryPoint()
	{

		TestNonGenInterface_NonGenType();
		TestNonGenInterface_GenType();
		TestGenInterface_NonGenType();
		TestGenInterface_GenType();
		
		if (pass)
		{
			Console.WriteLine("PASS");
			return 100;
		}
		else
		{
			Console.WriteLine("FAIL");
			return 101;
		}
		
		
	}
}
