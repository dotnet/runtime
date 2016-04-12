// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/* Test various combinations of constraints with illegal parameter types by invoking static method on the generic type
CONSTRAINTS:

default ctor
reference type 
valuetype
default ctor, reference tyoe
default ctor, valuetype

Test each constraint with (whichever applies to negative testing)
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


public class Test
{
	static bool pass;
	static int testNumber = 1;

       delegate void Case();
	

	static void Check(Case mytest, string testName, string violatingType, string type)
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
		  	
			Test.CheckTypeLoadExceptionMessage(8310, e, violatingType, type);
		}
	
		catch (Exception e) 
		{
			Console.WriteLine("\nFAIL: Caught unexpected exception: " + e);
			pass = false;
		}	

	}


	 public static void CheckTypeLoadExceptionMessage(uint ResourceID, TypeLoadException e, string violatingType, string type )
	 {
         if (
            !e.ToString().Contains("0") ||
            !e.ToString().Contains(violatingType) ||
            !e.ToString().Contains(type) ||
            !e.ToString().Contains("T"))
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

		Console.WriteLine("\nType: A<T> where T : new()\n");
		Console.WriteLine("\nNEGATIVE TESTS");
		
		Check(new Case(SM_DefaultCtorConstraint.Test2), "Generic argument is a class with no default ctor", "ClassNoCtor", "A`1[T]");
		Check(new Case(SM_DefaultCtorConstraint.Test4), "Generic argument is a delegate", "Delegate1", "A`1[T]");
		Check(new Case(SM_DefaultCtorConstraint.Test6), "Generic argument is an mscorlib abstract class with no default ctor", "System.ValueType", "A`1[T]");
		Check(new Case(SM_DefaultCtorConstraint.Test7), "Generic argument is an interface with no default ctor", "NonGenInterface", "A`1[T]");
		Check(new Case(SM_DefaultCtorConstraint.Test9), "Generic argument is an array of classes with default ctor", "ClassWithCtor[]", "A`1[T]");
		Check(new Case(SM_DefaultCtorConstraintGenTypes.Test2), " Generic argument is a generic class with no default ctor", "GenClassNoCtor[System.Int32]", "A`1[T]");
		Check(new Case(SM_DefaultCtorConstraintGenTypes.Test5), "Generic argument is a generic interface", "GenInterface[System.Int32]", "A`1[T]");


		Console.WriteLine("\nType: A<T> where T : class()\n");
		Console.WriteLine("\nNEGATIVE TESTS");
		
		Check(new Case(SM_ClassConstraint.Test3), "Generic argument is a struct", "NonGenStruct", "B`1[T]");
		Check(new Case(SM_ClassConstraint.Test8), "Generic argument is an enum", "Enum1", "B`1[T]");
		Check(new Case(SM_ClassConstraintGenTypes.Test3), "Generic argument is a generic struct with default ctor", "GenStruct[System.Int32]",  "B`1[T]");
		Check(new Case(SM_ClassConstraintGenTypes.Test6), "Generic argument is Nullable<T>", "System.Nullable`1[System.Int32]",  "B`1[T]");

		Console.WriteLine("\nType: A<T> where T : struct()\n");
		Console.WriteLine("\nNEGATIVE TESTS");

		Check(new Case(SM_StructConstraint.Test1), "Generic argument is a class with default ctor", "ClassWithCtor", "C`1[T]");
		Check(new Case(SM_StructConstraint.Test2), "Generic argument is a class with no default ctor", "ClassNoCtor", "C`1[T]");
		Check(new Case(SM_StructConstraint.Test4), "Generic argument is a delegate", "Delegate1", "C`1[T]");
		Check(new Case(SM_StructConstraint.Test5), "Generic argument is an mscorlib class with default ctor", "System.Object", "C`1[T]");
		Check(new Case(SM_StructConstraint.Test6), "Generic argument is an mscorlib abstract class with no default ctor", "System.ValueType", "C`1[T]");
		Check(new Case(SM_StructConstraint.Test7), "Generic argument is an interface", "NonGenInterface", "C`1[T]");
		Check(new Case(SM_StructConstraint.Test10), "Generic argument is an array of classes with default ctor", "ClassWithCtor[]", "C`1[T]");
		Check(new Case(SM_StructConstraintGenTypes.Test1), "Generic argument is a generic class with default ctor", "GenClassWithCtor[System.Int32]", "C`1[T]");
		Check(new Case(SM_StructConstraintGenTypes.Test2), "Generic argument is a generic class with no default ctor", "GenClassNoCtor[System.Int32]", "C`1[T]");
		Check(new Case(SM_StructConstraintGenTypes.Test5), "Generic argument is a generic interface", "GenInterface[System.Int32]", "C`1[T]");
		Check(new Case(SM_StructConstraintGenTypes.Test7), "Generic argument is Nullable<T>", "System.Nullable`1[System.Int32]",  "C`1[T]");



		Console.WriteLine("\nType: A<T> where T : class(), new() \n");
		Console.WriteLine("\nNEGATIVE TESTS");

		Check(new Case(SM_DefaultCtorAndClassConstraint.Test2), "Generic argument is a class with no default ctor", "ClassNoCtor", "D`1[T]");
		Check(new Case(SM_DefaultCtorAndClassConstraint.Test3), "Generic argument is a struct", "NonGenStruct", "D`1[T]");
		Check(new Case(SM_DefaultCtorAndClassConstraint.Test4), "Generic argument is a delegate", "Delegate1", "D`1[T]");
		Check(new Case(SM_DefaultCtorAndClassConstraint.Test6), "Generic argument is an mscorlib abstract class with no default ctor", "System.ValueType", "D`1[T]");
		Check(new Case(SM_DefaultCtorAndClassConstraint.Test9), "Generic argument is an mscorlib struct",  "System.DateTime", "D`1[T]");
 		Check(new Case(SM_DefaultCtorAndClassConstraint.Test7), "Generic argument is an interface", "NonGenInterface", "D`1[T]");
		Check(new Case(SM_DefaultCtorAndClassConstraint.Test8), "Generic argument is an enum", "Enum1", "D`1[T]");
		Check(new Case(SM_DefaultCtorAndClassConstraint.Test10), "Generic argument is an array of classes with default ctor", "ClassWithCtor[]", "D`1[T]");
		Check(new Case(SM_DefaultCtorAndClassConstraintGenTypes.Test3), "Generic argument is a generic struct", "GenStruct[System.Int32]", "D`1[T]");
		Check(new Case(SM_DefaultCtorAndClassConstraintGenTypes.Test2), "Generic argument is a generic class with no default ctor", "GenClassNoCtor[System.Int32]", "D`1[T]");
		Check(new Case(SM_DefaultCtorAndClassConstraintGenTypes.Test5), "Generic argument is a generic interface", "GenInterface[System.Int32]", "D`1[T]");
		Check(new Case(SM_DefaultCtorAndClassConstraintGenTypes.Test6), "Generic argument is a generic mscorlib struct", "System.Collections.Generic.KeyValuePair`2[NonGenStruct,System.Int32]", "D`1[T]");
		Check(new Case(SM_DefaultCtorAndClassConstraintGenTypes.Test7), "Generic argument is Nullable<T>", "System.Nullable`1[System.Int32]",  "D`1[T]");
		

		Console.WriteLine("\nType: A<T> where T : struct(), new()\n");
		Console.WriteLine("\nNEGATIVE TESTS");

		Check(new Case(SM_DefaultCtorAndStructConstraint.Test1), "Generic argument is a class with default ctor", "ClassWithCtor", "E`1[T]");
		Check(new Case(SM_DefaultCtorAndStructConstraint.Test2), "Generic argument is a class with no default ctor", "ClassNoCtor", "E`1[T]");
		Check(new Case(SM_DefaultCtorAndStructConstraint.Test4), "Generic argument is a delegate", "Delegate1", "E`1[T]");
		Check(new Case(SM_DefaultCtorAndStructConstraint.Test5), "Generic argument is an mscorlib class with default ctor", "System.Object", "E`1[T]");
		Check(new Case(SM_DefaultCtorAndStructConstraint.Test6), "Generic argument is an mscorlib abstract class with no default ctor", "System.ValueType", "E`1[T]");
		Check(new Case(SM_DefaultCtorAndStructConstraint.Test7), "Generic argument is an interface", "NonGenInterface", "E`1[T]");
		Check(new Case(SM_DefaultCtorAndStructConstraint.Test10), "Generic argument is an array of classes with default ctor", "NonGenStruct[]", "E`1[T]");
		Check(new Case(SM_DefaultCtorAndStructConstraintGenTypes.Test1), "Generic argument is a generic class with default ctor", "GenClassWithCtor[System.Int32]", "E`1[T]");
		Check(new Case(SM_DefaultCtorAndStructConstraintGenTypes.Test2), "Generic argument is a generic class with no default ctor", "GenClassNoCtor[System.Int32]", "E`1[T]");
		Check(new Case(SM_DefaultCtorAndStructConstraintGenTypes.Test5), "Generic argument is a generic interface", "GenInterface[System.Int32]", "E`1[T]");
		Check(new Case(SM_DefaultCtorAndStructConstraintGenTypes.Test7), "Generic argument is Nullable<T>", "System.Nullable`1[System.Int32]",  "E`1[T]");


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

