// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
Test_Class_ImplicitOverrideVirtualNewslotFinal the following matrix for classes with virtual newslot final methods (implicit override):

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
	public  int method1()
	{
		return 1;
	}
	
	public  int method2<T>()
	{
		return 2;
	}
}


public class C2<T> : I 
{
	public  int method1()
	{
		return 3;
	}
	
	public  int method2<U>()
	{
		return 4;
	}
}



public class C3Int : IGen<int>
{
	public  int method1()
	{
		return 5;
	}
	
	public  int method2<U>()
	{
		return 6;
	}
}

public class C3String : IGen<string>
{
	public  int method1()
	{
		return 5;
	}
	
	public  int method2<U>()
	{
		return 6;
	}
}


public class C3Object: IGen<object>
{
	public  int method1()
	{
		return 5;
	}
	
	public  int method2<U>()
	{
		return 6;
	}
}

public class C4<T> : IGen<T>
{
	public  int method1()
	{
		return 7;
	}
	
	public  int method2<U>()
	{
		return 8;
	}

}




public class Test_Class_ImplicitOverrideVirtualNewslotFinal
{

	public static bool pass = true;
	

	public static void TestNonGenInterface_NonGenType()
	{
		I ic1 = new C1();

		// TEST1: test generic virtual method
		if (ic1.method2<int>() != 2 ||  ic1.method2<string>() != 2   ||  ic1.method2<object>() != 2 
		   || ic1.method2<A<int>>() != 2   ||  ic1.method2<S<object>>() != 2  )
		{
			Console.WriteLine("Failed at TestNonGenInterface_NonGenType: generic method");
			pass = false;
		}	
	}

	public static void TestNonGenInterface_GenType()
	{
		I ic2Int = new C2<int>();
		I ic2Object = new C2<object>();
		I ic2String = new C2<string>();

		// TEST2: test non generic virtual method
		if ( ic2Int.method1() != 3 || ic2String.method1() != 3   || ic2Object.method1() != 3 )
		{
			Console.WriteLine("Failed at TestNonGenInterface_GenType: non generic method");
			pass = false;
		}	

		
		// TEST3: test generic virtual method
		if (ic2Int.method2<int>() != 4 || ic2Int.method2<object>() != 4|| ic2Int.method2<string>() != 4
     		     || ic2Int.method2<A<int>>() != 4 || ic2Int.method2<S<string>>() != 4
     		     
     		     || ic2String.method2<int>() != 4 || ic2String.method2<object>() != 4|| ic2String.method2<string>() != 4
     		     || ic2String.method2<A<int>>() != 4 || ic2String.method2<S<string>>() != 4
     		     
     		     || ic2Object.method2<int>() != 4 || ic2Object.method2<object>() != 4|| ic2Object.method2<string>() != 4
     		     || ic2Object.method2<A<int>>() != 4 || ic2Object.method2<S<string>>() != 4
     		     )
		{
			Console.WriteLine("Failed at TestNonGenInterface_GenType: generic method");
			pass = false;
		}
	
	}

	public static void TestGenInterface_NonGenType()
	{
		IGen<int> iIntc3 = new C3Int();
		IGen<object> iObjectc3 = new C3Object();
		IGen<string> iStringc3 = new C3String();

		// TEST4: test non generic virtual method
		if ( iIntc3.method1() != 5 || iObjectc3.method1() != 5   || iStringc3.method1() != 5 )
		{
			Console.WriteLine("Failed at TestGenInterface_NonGenType: non generic method");
			pass = false;
		}	

		
		// TEST5: test generic virtual method
		if (iIntc3.method2<int>() != 6 || iIntc3.method2<object>() != 6|| iIntc3.method2<string>() != 6
     		     || iIntc3.method2<A<int>>() != 6 || iIntc3.method2<S<string>>() != 6
     		     
     		     || iObjectc3.method2<int>() != 6 || iObjectc3.method2<object>() != 6|| iObjectc3.method2<string>() != 6
     		     || iObjectc3.method2<A<int>>() != 6 || iObjectc3.method2<S<string>>() != 6
     		     
     		     || iStringc3.method2<int>() != 6 || iStringc3.method2<object>() != 6|| iStringc3.method2<string>() != 6
     		     || iStringc3.method2<A<int>>() != 6 || iStringc3.method2<S<string>>() != 6
     		     )
		{
			Console.WriteLine("Failed at TestGenInterface_NonGenType: generic method");
			pass = false;
		}

	}

	public static void TestGenInterface_GenType()
	{
		IGen<int> iGenC4Int = new C4<int>();
		IGen<object> iGenC4Object = new C4<object>();
		IGen<string> iGenC4String = new C4<string>();

		// TEST6: test non generic virtual method
		if ( iGenC4Int.method1() != 7 || iGenC4Object.method1() != 7   || iGenC4String.method1() != 7 )
		{
			Console.WriteLine("Failed at TestGenInterface_GenType: non generic method");
			pass = false;
		}	

		
		// TEST7: test generic virtual method
		if (iGenC4Int.method2<int>() != 8 || iGenC4Int.method2<object>() != 8 || iGenC4Int.method2<string>() != 8
     		     || iGenC4Int.method2<A<int>>() != 8 || iGenC4Int.method2<S<string>>() != 8
     		     
     		     || iGenC4Object.method2<int>() != 8 || iGenC4Object.method2<object>() != 8  || iGenC4Object.method2<string>() !=  8
     		     || iGenC4Object.method2<A<int>>() != 8 || iGenC4Object.method2<S<string>>() != 8
     		     
     		     || iGenC4String.method2<int>() != 8 || iGenC4String.method2<object>() != 8 || iGenC4String.method2<string>() != 8
     		     || iGenC4String.method2<A<int>>() != 8 || iGenC4String.method2<S<string>>() != 8
     		     )
		{
			Console.WriteLine("Failed at TestGenInterface_GenType: generic method");
			pass = false;
		}
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
