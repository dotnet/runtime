// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/* Test various combinations of constraints with illegal parameter types by instantiating the type

*/

using System;



public class Test
{
	static bool pass;
	static int testNumber = 1;

       delegate void Case();	

	
	static void Check(Case mytest,  string testName, string genArgNumber, string violatingType, string type, string typeParam)
    	{

		Console.Write("Test"+testNumber + ": " + testName);
		++testNumber;

		
		try
		{
			mytest();

			Console.WriteLine("\nFAIL: Did not catch expected TypeLoadException");
			pass = false;
		}
		catch (TypeLoadException e)
		{
		  	// Unhandled Exception: System.TypeLoadException: %0, '%1', on '%2' 
		  	// violates the constraint of type parameter '%3'.
		  	
			Test.CheckTypeLoadExceptionMessage(8310, genArgNumber, e, violatingType, type, typeParam);
		}
	
		catch (Exception e) 
		{
			Console.WriteLine("\nFAIL: Caught unexpected exception: " + e);
			pass = false;
		}	

	}


	 public static void CheckTypeLoadExceptionMessage(uint ResourceID, string genArgNumber, TypeLoadException e, string violatingType, string type, string typeParam )
	 {
         if (
            !e.ToString().Contains(genArgNumber) ||
            !e.ToString().Contains(violatingType) ||
            !e.ToString().Contains(type) ||
            !e.ToString().Contains(typeParam))
        {
		    Console.WriteLine("Exception message is incorrect");
	   	    pass = false;
        }
		else
		{
		    Console.WriteLine("Caught expected exception");
		}	
	}

	public static int Main()
	{
		pass = true;

		Console.WriteLine("\nType: GClass<T> where T : class\n");
		Console.WriteLine("NEGATIVE TESTS");
		
		Check(new Case(RunTest.Test1), "class G1<P> : GClass<P> where P : struct {}, insantiate with class","0",  "P", "GClass`1[T]", "T");
		Check(new Case(RunTest.Test2), "class G1<P> : GClass<P> where P : struct {}, instantiate with valuetype", "0",  "P", "GClass`1[T]", "T");
		Check(new Case(RunTest.Test3), "class G2<P> : GClass<P> where P : I {}", "0",  "P", "GClass`1[T]", "T");
		Check(new Case(RunTest.Test4), "class G3<P> : GClass<P> where P : System.Object {}", "0", "P", "GClass`1[T]", "T");
		Check(new Case(RunTest.Test5), "class G4<P> : GClass<P> where P : System.ValueType {}", "0", "P", "GClass`1[T]", "T");
		Check(new Case(RunTest.Test6), "class G5<P> : GClass<P> where P : System.Enum {}", "0", "P", "GClass`1[T]", "T");		
	
		
		Console.WriteLine("\nType: GStruct<T> where T : struct\n");
		Console.WriteLine("NEGATIVE TESTS");

		
		Check(new Case(RunTest.Test7), "class G6<P> : IStruct<P> where P : class {}, insantiate with class","0",  "P", "GStruct`1[T]", "T");
		Check(new Case(RunTest.Test8), "class G6<P> : IStruct<P> where P : class {}, instantiate with valuetype", "0",  "P", "GStruct`1[T]", "T");
		Check(new Case(RunTest.Test9), "class G7<P> : IStruct<P> where P : A {}", "0",  "P", "GStruct`1[T]", "T");
		Check(new Case(RunTest.Test10), "class G8<P> : IStruct<P> where P : I {}", "0",  "P", "GStruct`1[T]", "T");

		Check(new Case(RunTest.Test14), "class G12<P> : IStruct<P> where P : System.ValueType {}", "0",  "P", "GStruct`1[T]", "T");
		Check(new Case(RunTest.Test15), "class G12<P> : IStruct<P> where P : System.Nullable<int> {}", "0",  "P", "GStruct`1[T]", "T");


		Console.WriteLine("\nType: GNew<T> where T : new() \n");
		Console.WriteLine("NEGATIVE TESTS");

		
		Check(new Case(RunTest.Test11), "class G9<P> : GNew<P> where P : A {}, insantiate with class","0",  "P", "GNew`1[T]", "T");
		Check(new Case(RunTest.Test12), "class G10<P> : GNew<P> where P : class {}, instantiate with valuetype", "0",  "P", "GNew`1[T]", "T");
		Check(new Case(RunTest.Test13), "class G11<P> : GNew<P> where P : I {}", "0",  "P", "GNew`1[T]", "T");


		
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


