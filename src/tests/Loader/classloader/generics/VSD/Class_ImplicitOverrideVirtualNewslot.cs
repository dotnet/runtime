// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
Test_Class_ImplicitOverrideVirtualNewslot the following matrix for classes with virtual newslot methods (implicit override):

Non-Generic virtual methods:
						Non-generic Interface	Generic Interface
		Non-generic type  			N/A			Test4
		Generic type				Test2			Test6


Generic virtual methods:
						Non-generic Interface	Generic Interface
		Non-generic type			Test1 			Test5
		Generic type				Test3			Test7 

*/

using System;
using Xunit;


public class A<T>
{}

public struct S<T>
{}


public interface I 
{
	int method1();
	int method2<T>();
}

public interface IGen<T>
{
	int method1();
	int method2<M>();
}


public class C1 : I
{
	public virtual int method1()
	{
		return 1;
	}
	
	public virtual int method2<T>()
	{
		return 2;
	}
}


public class C2<T> : I 
{
	public virtual int method1()
	{
		return 3;
	}
	
	public virtual int method2<U>()
	{
		return 4;
	}
}



public class C3Int : IGen<int>
{
	public virtual int method1()
	{
		return 5;
	}
	
	public virtual int method2<U>()
	{
		return 6;
	}
}

public class C3String : IGen<string>
{
	public virtual int method1()
	{
		return 5;
	}
	
	public virtual int method2<U>()
	{
		return 6;
	}
}


public class C3Object: IGen<object>
{
	public virtual int method1()
	{
		return 5;
	}
	
	public virtual int method2<U>()
	{
		return 6;
	}
}

public class C4<T> : IGen<T>
{
	public virtual int method1()
	{
		return 7;
	}
	
	public virtual int method2<U>()
	{
		return 8;
	}

}




public class Test_Class_ImplicitOverrideVirtualNewslot
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
		I ic1 = new C1();

		// TEST1: test generic virtual method
		Eval( (ic1.method2<int>().ToString()).Equals("2") );
		Eval( (ic1.method2<string>() .ToString()).Equals("2") );
		Eval( (ic1.method2<object>().ToString()).Equals("2") );
		Eval( (ic1.method2<A<int>>().ToString()).Equals("2") );
		Eval( (ic1.method2<S<object>>().ToString()).Equals("2") );
	}

	public static void TestNonGenInterface_GenType()
	{
		I ic2Int = new C2<int>();
		I ic2Object = new C2<object>();
		I ic2String = new C2<string>();

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
		IGen<int> iIntc3 = new C3Int();
		IGen<object> iObjectc3 = new C3Object();
		IGen<string> iStringc3 = new C3String();

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
		IGen<int> iGenC4Int = new C4<int>();
		IGen<object> iGenC4Object = new C4<object>();
		IGen<string> iGenC4String = new C4<string>();

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
