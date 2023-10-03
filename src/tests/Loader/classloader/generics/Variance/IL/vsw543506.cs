// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Regression test for VSW 543506
/*

Testing that generic argument types for co/contravariant generic types are boxed
(VSW 543506)
Test_vsw543506: under Loader\ClassLoader\Generics\Variance\IL
Positive and negative tests on casting valuetype instantiations such as
Positive:
"	IPos<int> is castable to IPos<int>   (exact match on value types)

Negative
"	IPos<int> is not castable to IPos<unsigned int>
IPos<int> is not castable to IPos<MyEnum>


*/
using System;
using Xunit;

public class C<T> : IPos<T>, INeg<T>
{
    public T produce()
    {
        return default(T);
    }

    public void consume(T t)
    {
    }
}

enum intEnum : int {}
enum uintEnum : uint {}

public class Test_vsw543506
{

	public static bool pass;
	public static void IsInstShouldFail<T>(object o)
	{
		Console.WriteLine("cast from " + o.GetType() + " to  " + typeof(T));

    		if (o is T) 
		{
			Console.WriteLine("isinst on object of type " + o.GetType() + " to " + typeof(T) + " succeeded. Expected failure");
			pass = false;
    		}
  	}

  	public static void IsInstShouldWork<T>(object o) 
	{
		Console.WriteLine("cast from " + o.GetType() + " to  " + typeof(T));

		if (!(o is T)) 
		{
			Console.WriteLine("isinst on object of type " + o.GetType() + " to " + typeof(T) + " failed. Expected success");
			pass = false;
		}
  	}

  	public static void CastClassShouldFail<T>(object o)
  	{
		Console.WriteLine("cast from " + o.GetType() + " to  " + typeof(T));

     		try
     		{
	
	     		T obj = (T)o;
	      		Console.WriteLine("cast on object of type " + o.GetType() + " to " + typeof(T) + " succeeded. Expected failure");
			pass = false;
     		}
     		catch (InvalidCastException)
     		{
     			// expecting to get an InvalidCastException
     		}
		catch (Exception e)
     		{
     			Console.WriteLine("Caught unexpected exception: " + e);
     			pass = false;
     		}
  	}


  	public static void CastClassShouldWork<T>(object o)
  	{
		Console.WriteLine("cast from " + o.GetType() + " to  " + typeof(T));

     		try
     		{
	     		T obj = (T)o;
     		}
     		catch (Exception e)
     		{
	 		Console.WriteLine("cast on object of type " + o.GetType() + " to " + typeof(T) + " failed. Expected success");
			Console.WriteLine(e);
			pass = false;
     		}
  	}

   	

  	[Fact]
  	public static int TestEntryPoint() 
	{
		pass = true;
		
    		C<long> cl = new C<long>();
		C<object> co = new C<object>();
	  	C<IComparable> ci = new C<IComparable>();
   		

	    	// Primitives
	    	C<uint> cui = new C<uint>();
	    	C<intEnum> ciEnum = new C<intEnum>();
	    	C<uintEnum> cuiEnum = new C<uintEnum>();
	    	C<int> cint = new C<int>();
 

		// ==============================
		// TESTS for IsInst
		// ==============================
		Console.WriteLine("\n================================");
		Console.WriteLine("   Positive tests for IsInst:");
		Console.WriteLine("================================\n");

		
		// Exact match on value types

	    	IsInstShouldWork<IPos<int>>(cint);
	    	IsInstShouldWork<IPos<long>>(cl);
	    	IsInstShouldWork<IPos<uint>>(cui);


	    	IsInstShouldWork<INeg<int>>(cint);
	    	IsInstShouldWork<INeg<long>>(cl);
	    	IsInstShouldWork<INeg<uint>>(cui);

		// Runtime type tests
		IsInstShouldWork<IPos<object>>(ci);
	    	IsInstShouldWork<INeg<string>>(ci);
		
		Console.WriteLine("\n================================");
		Console.WriteLine("   Negative tests for IsInst:");
		Console.WriteLine("================================\n");

	    
	    	IsInstShouldFail<IPos<object>>(cl);
	    	IsInstShouldFail<INeg<long>>(ci);	    





		// IPos<unit> --> IPos<int>
	    	IsInstShouldFail<IPos<int>>(cui);

		// IPos<IComparable> --> IPos<uint>
	    	IsInstShouldFail<IPos<uint>>(ci);

		// IPos<intEnum> --> IPos<int>
	    	IsInstShouldFail<IPos<int>>(ciEnum);

		// IPos<IComparable> --> IPos<intEnum>
	    	IsInstShouldFail<IPos<intEnum>>(ci);

		// IPos<uintEnum> --> IPos<uint>
	    	IsInstShouldFail<IPos<uint>>(cuiEnum);
		
		// IPos<uint> --> IPos<uintEnum>
	    	IsInstShouldFail<IPos<uintEnum>>(cui);

		// IPos<intEnum> --> IPos<uint>
	    	IsInstShouldFail<IPos<uint>>(ciEnum);

		// IPos<uint> --> IPos<intEnum>
	    	IsInstShouldFail<IPos<intEnum>>(cui);

		// IPos<int> --> IPos<intEnum>
		IsInstShouldFail<IPos<intEnum>>(cint);

		// IPos<int> --> IPos<uint>
	    	IsInstShouldFail<IPos<uint>>(cint);


		// same for INeg<>
	    	IsInstShouldFail<IPos<int>>(cui);
	    	IsInstShouldFail<IPos<uint>>(ci);
	    	IsInstShouldFail<IPos<int>>(ciEnum);
	    	IsInstShouldFail<IPos<intEnum>>(ci);
	    	IsInstShouldFail<IPos<uint>>(cuiEnum);
	    	IsInstShouldFail<IPos<uintEnum>>(cui);
	    	IsInstShouldFail<IPos<uint>>(ciEnum);
	    	IsInstShouldFail<IPos<intEnum>>(cui);
		IsInstShouldFail<IPos<intEnum>>(cint);
	    	IsInstShouldFail<IPos<uint>>(cint);


		// ==============================
		// TESTS for CastClass
		// ==============================

		Console.WriteLine("\n================================");
		Console.WriteLine("   Positive tests for CastClass:");
		Console.WriteLine("================================\n");

		CastClassShouldWork<IPos<object>>(ci);
	    	CastClassShouldWork<INeg<string>>(ci);


	    	CastClassShouldWork<IPos<int>>(cint);
	    	CastClassShouldWork<IPos<long>>(cl);
	    	CastClassShouldWork<IPos<uint>>(cui);


		Console.WriteLine("\n================================");
		Console.WriteLine("   Negative tests for CastClass:");
		Console.WriteLine("================================\n");

	    	CastClassShouldFail<IPos<int>>(cui);
	    	CastClassShouldFail<IPos<uint>>(ci);
	    	CastClassShouldFail<IPos<int>>(ciEnum);
	    	CastClassShouldFail<IPos<intEnum>>(ci);
	    	CastClassShouldFail<IPos<uint>>(cuiEnum);
	    	CastClassShouldFail<IPos<uintEnum>>(cui);
	    	CastClassShouldFail<IPos<uint>>(ciEnum);
	    	CastClassShouldFail<IPos<intEnum>>(cui);
	    	CastClassShouldFail<IPos<intEnum>>(cint);
	    	CastClassShouldFail<IPos<uint>>(cint);
			
	    	CastClassShouldFail<INeg<int>>(cui);
	    	CastClassShouldFail<INeg<uint>>(ci);
	    	CastClassShouldFail<INeg<int>>(ciEnum);
	    	CastClassShouldFail<INeg<intEnum>>(ci);
	   	CastClassShouldFail<INeg<uint>>(cuiEnum);
	    	CastClassShouldFail<INeg<uintEnum>>(cui);
	   	CastClassShouldFail<INeg<uint>>(ciEnum);
	   	CastClassShouldFail<INeg<intEnum>>(cui);

	    	CastClassShouldFail<INeg<intEnum>>(cint);
	    	CastClassShouldFail<INeg<uint>>(cint);
		

	    	

	    	

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
