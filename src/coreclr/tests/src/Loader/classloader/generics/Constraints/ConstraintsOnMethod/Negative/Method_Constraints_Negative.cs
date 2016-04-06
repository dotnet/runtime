// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NEGATIVE TESTS
/* Test various combinations of constraints on methods 
CONSTRAINTS:

default ctor
reference type 
valuetype
default ctor, reference tyoe
default ctor, valuetype

Test each constraint with
- Class with default nullary ctor (Generic/Non generic)
- Class with no default nullary ctor (Generic/Non generic)
- Class from mscorlib with default nullary ctor
- Abstract Class from mscorlib with no default nullary ctor

- Struct from mscorlib (Generic/Non generic)
- Struct (Generic/Non generic)
- Enum (Generic/Non generic)

- Interface (Generic/Non generic)

- Array

- Delegate

- Nullable<T>
*/

using System;
using System.Security;

public class Test
{
	static bool pass;
	static int testNumber = 1;

       delegate void Case();
	

	static void Check(Case mytest, string testName, string type, string methodName, string violatingType)
    	{

		Console.Write("Test"+testNumber + ": " + testName);
		++testNumber;

		
		try
		{
			mytest();

			Console.WriteLine("\nFAIL: Did not catch expected TypeLoadException");
			pass = false;
		}
		catch (VerificationException e)
		{

		  	
			Test.CheckVerificationExceptionMessage(8311, e, type, methodName, violatingType);
		}
	
		catch (Exception e) 
		{
			Console.WriteLine("\nFAIL: Caught unexpected exception: " + e);
			pass = false;
		}	

	}


	 public static void CheckVerificationExceptionMessage(uint ResourceID, VerificationException e, string type, string methodName, string violatingType)
	 {
		// "Method %1.%2: type argument '%3' violates the constraint of type parameter '%4'."
        bool found1 = e.ToString().IndexOf(type + "." + methodName) >= 0;
        bool found2 = e.ToString().IndexOf(violatingType) >= 0;
        bool found3 = e.ToString().IndexOf("T") >= 0;
        
		if (!found1 || !found2 || !found3)
		{
		    Console.WriteLine(" : Exception message is incorrect");
		    Console.WriteLine("Expected: " + "Method " + type + "." + methodName + ": type argument '" + violatingType + "' violates the constraint of type parameter 'T'");
		    Console.WriteLine("Actual: " + e.Message.ToString());
	   	    pass = false;
		}
		else
		{
		    Console.WriteLine(" : Caught expected exception");
		}
	}

	

	public static int Main()
	{
		pass = true;

		Console.WriteLine("\nNEGATIVE TESTS");

		Console.WriteLine("\nType: A<T> where T : new()\n");
		
	
		Check(new Case(M_DefaultCtorConstraint.Test2), "Generic argument is a class with no default ctor", "A", "method1", "ClassNoCtor");
		Check(new Case(M_DefaultCtorConstraint.Test4), "Generic argument is a delegate", "A", "method1", "Delegate1");
		Check(new Case(M_DefaultCtorConstraint.Test6), "Generic argument is an mscorlib abstract class with no default ctor",  "A", "method1", "System.ValueType");
		Check(new Case(M_DefaultCtorConstraint.Test7), "Generic argument is an interface with no default ctor",  "A", "method1", "NonGenInterface");

		// WRONG?
		Check(new Case(M_DefaultCtorConstraint.Test9), "Generic argument is an array of classes with default ctor",  "A", "method1", "ClassWithCtor[]");
		
		Check(new Case(M_DefaultCtorConstraintGenTypes.Test2), " Generic argument is a generic class with no default ctor",  "A", "method1", "GenClassNoCtor[System.Int32]");
		Check(new Case(M_DefaultCtorConstraintGenTypes.Test5), "Generic argument is a generic interface",  "A", "method1",  "GenInterface[System.Int32]");


		Console.WriteLine("\nType: A<T> where T : class()\n");
		
		Check(new Case(M_ClassConstraint.Test3), "Generic argument is a struct",  "B", "method1", "NonGenStruct");
		Check(new Case(M_ClassConstraint.Test8), "Generic argument is an enum", "B", "method1", "Enum1");
		Check(new Case(M_ClassConstraintGenTypes.Test3), "Generic argument is a generic struct with default ctor", "B", "method1", "GenStruct[System.Int32]");
		Check(new Case(M_ClassConstraintGenTypes.Test6), "Generic argument is Nullable<T>", "B", "method1", "System.Nullable`1[System.Int32]");

		Console.WriteLine("\nType: A<T> where T : struct()\n");
		
		Check(new Case(M_StructConstraint.Test1), "Generic argument is a class with default ctor", "C", "method1",  "ClassWithCtor");
		Check(new Case(M_StructConstraint.Test2), "Generic argument is a class with no default ctor", "C", "method1",  "ClassNoCtor");
		Check(new Case(M_StructConstraint.Test4), "Generic argument is a delegate", "C", "method1",  "Delegate1");
		Check(new Case(M_StructConstraint.Test5), "Generic argument is an mscorlib class with default ctor", "C", "method1",  "System.Object");
		Check(new Case(M_StructConstraint.Test6), "Generic argument is an mscorlib abstract class with no default ctor", "C", "method1",  "System.ValueType");
		Check(new Case(M_StructConstraint.Test7), "Generic argument is an interface", "C", "method1",  "NonGenInterface");
		Check(new Case(M_StructConstraint.Test10), "Generic argument is an array of classes with default ctor", "C", "method1",  "ClassWithCtor[]");
		
		Check(new Case(M_StructConstraintGenTypes.Test1), "Generic argument is a generic class with default ctor", "C", "method1",  "GenClassWithCtor[System.Int32]");
		Check(new Case(M_StructConstraintGenTypes.Test2), "Generic argument is a generic class with no default ctor", "C", "method1",  "GenClassNoCtor[System.Int32]");
		Check(new Case(M_StructConstraintGenTypes.Test5), "Generic argument is a generic interface", "C", "method1",  "GenInterface[System.Int32]");
		Check(new Case(M_StructConstraintGenTypes.Test7), "Generic argument is Nullable<T>", "C", "method1",  "System.Nullable`1[System.Int32]");



		Console.WriteLine("\nType: A<T> where T : class(), new() \n");

		Check(new Case(M_DefaultCtorAndClassConstraint.Test2), "Generic argument is a class with no default ctor",  "D", "method1",  "ClassNoCtor");
		Check(new Case(M_DefaultCtorAndClassConstraint.Test3), "Generic argument is a struct", "D", "method1",  "NonGenStruct");
		Check(new Case(M_DefaultCtorAndClassConstraint.Test4), "Generic argument is a delegate", "D", "method1",  "Delegate1");
		Check(new Case(M_DefaultCtorAndClassConstraint.Test6), "Generic argument is an mscorlib abstract class with no default ctor", "D", "method1",  "System.ValueType");
		Check(new Case(M_DefaultCtorAndClassConstraint.Test9), "Generic argument is an mscorlib struct",  "D", "method1",  "System.DateTime");
 		Check(new Case(M_DefaultCtorAndClassConstraint.Test7), "Generic argument is an interface", "D", "method1",  "NonGenInterface");
		Check(new Case(M_DefaultCtorAndClassConstraint.Test8), "Generic argument is an enum", "D", "method1",  "Enum1");
		Check(new Case(M_DefaultCtorAndClassConstraint.Test10), "Generic argument is an array of classes with default ctor", "D", "method1",  "ClassWithCtor[]");

		
		Check(new Case(M_DefaultCtorAndClassConstraintGenTypes.Test3), "Generic argument is a generic struct", "D", "method1",  "GenStruct[System.Int32]");
		Check(new Case(M_DefaultCtorAndClassConstraintGenTypes.Test2), "Generic argument is a generic class with no default ctor", "D", "method1",  "GenClassNoCtor[System.Int32]");
		Check(new Case(M_DefaultCtorAndClassConstraintGenTypes.Test5), "Generic argument is a generic interface", "D", "method1",  "GenInterface[System.Int32]");
		Check(new Case(M_DefaultCtorAndClassConstraintGenTypes.Test6), "Generic argument is a generic mscorlib struct", "D", "method1",  "System.Collections.Generic.KeyValuePair`2[NonGenStruct,System.Int32]");
		Check(new Case(M_DefaultCtorAndClassConstraintGenTypes.Test7), "Generic argument is Nullable<T>", "D", "method1",  "System.Nullable`1[System.Int32]");
		

		Console.WriteLine("\nType: A<T> where T : struct(), new()\n");

		Check(new Case(M_DefaultCtorAndStructConstraint.Test1), "Generic argument is a class with default ctor", "E", "method1",  "ClassWithCtor");
		Check(new Case(M_DefaultCtorAndStructConstraint.Test2), "Generic argument is a class with no default ctor", "E", "method1",  "ClassNoCtor");
		Check(new Case(M_DefaultCtorAndStructConstraint.Test4), "Generic argument is a delegate", "E", "method1",  "Delegate1");
		Check(new Case(M_DefaultCtorAndStructConstraint.Test5), "Generic argument is an mscorlib class with default ctor", "E", "method1",  "System.Object");
		Check(new Case(M_DefaultCtorAndStructConstraint.Test6), "Generic argument is an mscorlib abstract class with no default ctor", "E", "method1",  "System.ValueType");
		Check(new Case(M_DefaultCtorAndStructConstraint.Test7), "Generic argument is an interface", "E", "method1",  "NonGenInterface");

		Check(new Case(M_DefaultCtorAndStructConstraint.Test10), "Generic argument is an array of classes with default ctor", "E", "method1",  "NonGenStruct[]");
		
		Check(new Case(M_DefaultCtorAndStructConstraintGenTypes.Test1), "Generic argument is a generic class with default ctor", "E", "method1",  "GenClassWithCtor[System.Int32]");
		Check(new Case(M_DefaultCtorAndStructConstraintGenTypes.Test2), "Generic argument is a generic class with no default ctor", "E", "method1",  "GenClassNoCtor[System.Int32]");
		Check(new Case(M_DefaultCtorAndStructConstraintGenTypes.Test5), "Generic argument is a generic interface", "E", "method1",  "GenInterface[System.Int32]");
		Check(new Case(M_DefaultCtorAndStructConstraintGenTypes.Test7), "Generic argument is Nullable<T>", "E", "method1",  "System.Nullable`1[System.Int32]");


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

