// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
	Test_InterfaceInheritanceTest2 that variance is not inherited across interfaces.
	So if the parent interface is co/contra variant but the child interface is not variant 
	we can use the generic type parameter of the child in any position 
	(both as return parameter and argument type to a method)
*/

using System;
using Xunit;

public class C1<T> : I1<T> 
{
	public T produce()
    	{
       	return default(T);
    	}

    	public void consume(T t)
    	{
    	}
}

public class C2<T> : I2<T> 
{
	public T produce()
    	{
       	return default(T);
    	}

    	public void consume(T t)
    	{
    	}
}




public class Test_InterfaceInheritanceTest2
{	
	static bool pass;

       delegate void Case();

	public static void Test1a()
	{
		C1<int> obj = new C1<int>();
		
		int i = obj.produce();
		obj.consume(5);
	}


	public static void Test1b()
	{
		C1<object> obj = new C1<object>();	
		
		Object o = obj.produce();
		obj.consume(new Object());
	}



	public static void Test2a()
	{
		C2<int> obj = new C2<int>();
		
		int i = obj.produce();
		obj.consume(5);
	}


	public static void Test2b()
	{
		C2<object> obj = new C2<object>();	
		
		Object o = obj.produce();
		obj.consume(new Object());
	}



	static void Check(Case mytest, string testName)
    	{
		Console.WriteLine(testName);

		try
		{
			mytest();	
		}
		catch (Exception e) 
		{
			Console.WriteLine("FAIL: Caught unexpected exception: " + e);
			pass = false;
		}
	}
     
	

	
	
	
  	[Fact]
  	public static int TestEntryPoint() 
	{
		pass = true;

		Console.WriteLine("\nInherited interface : covariant");
		
		Check(new Case(Test1a), "Test 1a: Implementing interface: non variant"); // primitive generic param
		Check(new Case(Test1b), "Test 1b: Implementing interface: non variant"); // reference type generic param				

		Console.WriteLine("\nInherited interface : contravariant");
		
		Check(new Case(Test2a), "Test 2a: Implementing interface: non variant"); // primitive generic param
		Check(new Case(Test2b), "Test 2b: Implementing interface: non variant"); // reference type generic param

	

		
		if (pass)
		{
			Console.WriteLine("\nPASS");
			return 100;
		}
		else
		{
			Console.WriteLine("\nFAIL");
			return 101;
		}
  	}
}

