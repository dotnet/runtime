// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NEGATIVE TESTS
/*

The following constraint combinations are tested:

- Interface-type, New() 
- class, Interface-type, New()
- struct, Interface-type, New()
- Class-type, Interface-type, New()
- Class-type, New()


Each is tested with type parameter (whichever is valid in negative tests):
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

		Console.WriteLine("\nNEGATIVE TESTS");

		Console.WriteLine("\nType: A<T> where T : I, new()\n");
		
	
		Check(new Case(Interface_DefaultCtorConstraint.Test1), "Generic argument is a class with no default ctor", "ClassNoCtor", "A`1[T]");
		Check(new Case(Interface_DefaultCtorConstraint .Test2), "Generic argument is an mscorlib class with default ctor",  "System.Object", "A`1[T]");
		Check(new Case(Interface_DefaultCtorConstraint.Test3), "Generic argument is a delegate", "Delegate1", "A`1[T]");
		Check(new Case(Interface_DefaultCtorConstraint .Test4), "Generic argument is an enum",  "Enum1", "A`1[T]");
		Check(new Case(Interface_DefaultCtorConstraint.Test5), "Generic argument is an mscorlib abstract class with no default ctor", "System.ValueType", "A`1[T]");
		Check(new Case(Interface_DefaultCtorConstraint.Test6), "Generic argument is an interface with no default ctor",  "NonGenInterface", "A`1[T]");

		Check(new Case(Interface_DefaultCtorConstraint.Test7), "Generic argument is an array of classes with default ctor", "ClassWithCtor[]", "A`1[T]");
		
		Check(new Case(Interface_DefaultCtorConstraint.Test8), "Generic argument is a generic class with no default ctor", "GenClassNoCtor[System.Int32]", "A`1[T]");
		Check(new Case(Interface_DefaultCtorConstraint.Test9), "Generic argument is a generic interface",  "GenInterface[System.Int32]", "A`1[T]");
		Check(new Case(Interface_DefaultCtorConstraint.Test10), "Generic argument is Nullable<T>",  "System.Nullable`1[System.Int32]", "A`1[T]");

		Console.WriteLine("\nType: A<T> where T : class, I, new() \n");

		Check(new Case(Interface_DefaultCtorAndClassConstraint.Test1), "Generic argument is a class with no default ctor", "ClassNoCtor", "D`1[T]");
		Check(new Case(Interface_DefaultCtorAndClassConstraint.Test2), "Generic argument is an mscorlib class with default ctor",  "System.Object", "D`1[T]");
		Check(new Case(Interface_DefaultCtorAndClassConstraint.Test3), "Generic argument is a delegate", "Delegate1", "D`1[T]");
		Check(new Case(Interface_DefaultCtorAndClassConstraint.Test4), "Generic argument is an enum",  "Enum1", "D`1[T]");
		Check(new Case(Interface_DefaultCtorAndClassConstraint.Test5), "Generic argument is an mscorlib abstract class with no default ctor", "System.ValueType", "D`1[T]");
		Check(new Case(Interface_DefaultCtorAndClassConstraint.Test6), "Generic argument is an interface with no default ctor",  "NonGenInterface", "D`1[T]");

		Check(new Case(Interface_DefaultCtorAndClassConstraint.Test7), "Generic argument is an array of classes with default ctor", "ClassWithCtor[]", "D`1[T]");
		
		Check(new Case(Interface_DefaultCtorAndClassConstraint.Test8), "Generic argument is a generic class with no default ctor", "GenClassNoCtor[System.Int32]", "D`1[T]");
		Check(new Case(Interface_DefaultCtorAndClassConstraint.Test9), "Generic argument is a generic interface",  "GenInterface[System.Int32]", "D`1[T]");
		Check(new Case(Interface_DefaultCtorAndClassConstraint.Test10), "Generic argument is Nullable<T>",  "System.Nullable`1[System.Int32]", "D`1[T]");

		Check(new Case(Interface_DefaultCtorAndClassConstraint.Test11), "Generic argument is a struct", "NonGenStruct", "D`1[T]");
		Check(new Case(Interface_DefaultCtorAndClassConstraint.Test12), "Generic argument is a generic struct", "GenStruct[System.Int32]", "D`1[T]");


		Console.WriteLine("\nType: A<T> where T : struct, I, new()\n");

		Check(new Case(Interface_DefaultCtorAndStructConstraint.Test1), "Generic argument is a class with default ctor", "ClassWithCtor", "E`1[T]");
		Check(new Case(Interface_DefaultCtorAndStructConstraint.Test2), "Generic argument is a class with no default ctor", "ClassNoCtor", "E`1[T]");
		Check(new Case(Interface_DefaultCtorAndStructConstraint.Test3), "Generic argument is an mscorlib class with default ctor", "System.Object", "E`1[T]");
		Check(new Case(Interface_DefaultCtorAndStructConstraint.Test4), "Generic argument is a delegate", "Delegate1", "E`1[T]");
		Check(new Case(Interface_DefaultCtorAndStructConstraint.Test5), "Generic argument is an enum",  "Enum1", "E`1[T]");
		Check(new Case(Interface_DefaultCtorAndStructConstraint.Test6), "Generic argument is an mscorlib abstract class with no default ctor", "System.ValueType", "E`1[T]");
		Check(new Case(Interface_DefaultCtorAndStructConstraint.Test7), "Generic argument is an interface", "NonGenInterface", "E`1[T]");	
		Check(new Case(Interface_DefaultCtorAndStructConstraint.Test8), "Generic argument is an array of classes with default ctor", "ClassWithCtor[]", "E`1[T]");		
		Check(new Case(Interface_DefaultCtorAndStructConstraint.Test9), "Generic argument is a generic class with no default ctor", "GenClassNoCtor[System.Int32]", "E`1[T]");
		Check(new Case(Interface_DefaultCtorAndStructConstraint.Test10), "Generic argument is a generic interface",  "GenInterface[System.Int32]", "E`1[T]");
		Check(new Case(Interface_DefaultCtorAndStructConstraint.Test11), "Generic argument is Nullable<T>",  "System.Nullable`1[System.Int32]", "E`1[T]");
		Check(new Case(Interface_DefaultCtorAndStructConstraint.Test12), "Generic argument is a generic class with default ctor", "GenClassWithCtor[System.Int32]", "E`1[T]");


		Console.WriteLine("\nType: A<T> where T : C, I, new()\n");
		

		Check(new Case(Interface_DefaultCtorAndClassTypeConstraint.Test1), "Generic argument is a class with default ctor, implements I, doesn't extend C", "ClassWithCtor", "F`1[T]");
		Check(new Case(Interface_DefaultCtorAndClassTypeConstraint.Test15), "Generic argument is a class with default ctor, doesn't implement I but extends C", "ClassWithCtor2", "F`1[T]");
		Check(new Case(Interface_DefaultCtorAndClassTypeConstraint.Test2), "Generic argument is a class with no default ctor", "ClassNoCtor", "F`1[T]");
		Check(new Case(Interface_DefaultCtorAndClassTypeConstraint.Test3), "Generic argument is an mscorlib class with default ctor", "System.Object", "F`1[T]");
		Check(new Case(Interface_DefaultCtorAndClassTypeConstraint.Test4), "Generic argument is a delegate", "Delegate1", "F`1[T]");
		Check(new Case(Interface_DefaultCtorAndClassTypeConstraint.Test5), "Generic argument is an enum",  "Enum1", "F`1[T]");
		Check(new Case(Interface_DefaultCtorAndClassTypeConstraint.Test6), "Generic argument is an mscorlib abstract class with no default ctor", "System.ValueType", "F`1[T]");
		Check(new Case(Interface_DefaultCtorAndClassTypeConstraint.Test7), "Generic argument is an interface", "NonGenInterface", "F`1[T]");	
		Check(new Case(Interface_DefaultCtorAndClassTypeConstraint.Test8), "Generic argument is an array of classes with default ctor", "ClassWithCtor[]", "F`1[T]");		
		Check(new Case(Interface_DefaultCtorAndClassTypeConstraint.Test9), "Generic argument is a generic class with no default ctor", "GenClassNoCtor[System.Int32]", "F`1[T]");
		Check(new Case(Interface_DefaultCtorAndClassTypeConstraint.Test10), "Generic argument is a generic interface",  "GenInterface[System.Int32]", "F`1[T]");
		Check(new Case(Interface_DefaultCtorAndClassTypeConstraint.Test11), "Generic argument is Nullable<T>",  "System.Nullable`1[System.Int32]", "F`1[T]");
		Check(new Case(Interface_DefaultCtorAndClassTypeConstraint.Test12), "Generic argument is a generic class with default ctor", "GenClassWithCtor[System.Int32]", "F`1[T]");
		Check(new Case(Interface_DefaultCtorAndClassTypeConstraint.Test13), "Generic argument is a struct", "NonGenStruct", "F`1[T]");
		Check(new Case(Interface_DefaultCtorAndClassTypeConstraint.Test14), "Generic argument is a generic struct", "GenStruct[System.Int32]", "F`1[T]");


		
		Console.WriteLine("\nType: A<T> where T : C,  new()\n");
		

		Check(new Case(Instantiation_DefaultCtorAndClassTypeConstraint.Test1), "Generic argument is a class with default ctor, implements I, doesn't extend C", "ClassWithCtor", "F`1[T]");
		Check(new Case(Instantiation_DefaultCtorAndClassTypeConstraint.Test2), "Generic argument is a class with no default ctor", "ClassNoCtor", "F`1[T]");
		Check(new Case(Instantiation_DefaultCtorAndClassTypeConstraint.Test3), "Generic argument is an mscorlib class with default ctor", "System.Object", "F`1[T]");
		Check(new Case(Instantiation_DefaultCtorAndClassTypeConstraint.Test4), "Generic argument is a delegate", "Delegate1", "F`1[T]");
		Check(new Case(Instantiation_DefaultCtorAndClassTypeConstraint.Test5), "Generic argument is an enum",  "Enum1", "F`1[T]");
		Check(new Case(Instantiation_DefaultCtorAndClassTypeConstraint.Test6), "Generic argument is an mscorlib abstract class with no default ctor", "System.ValueType", "F`1[T]");
		Check(new Case(Instantiation_DefaultCtorAndClassTypeConstraint.Test7), "Generic argument is an interface", "NonGenInterface", "F`1[T]");	
		Check(new Case(Instantiation_DefaultCtorAndClassTypeConstraint.Test8), "Generic argument is an array of classes with default ctor", "ClassWithCtor[]", "F`1[T]");		
		Check(new Case(Instantiation_DefaultCtorAndClassTypeConstraint.Test9), "Generic argument is a generic class with no default ctor", "GenClassNoCtor[System.Int32]", "F`1[T]");
		Check(new Case(Instantiation_DefaultCtorAndClassTypeConstraint.Test10), "Generic argument is a generic interface",  "GenInterface[System.Int32]", "F`1[T]");
		Check(new Case(Instantiation_DefaultCtorAndClassTypeConstraint.Test11), "Generic argument is Nullable<T>",  "System.Nullable`1[System.Int32]", "F`1[T]");
		Check(new Case(Instantiation_DefaultCtorAndClassTypeConstraint.Test12), "Generic argument is a generic class with default ctor", "GenClassWithCtor[System.Int32]", "F`1[T]");
		Check(new Case(Instantiation_DefaultCtorAndClassTypeConstraint.Test13), "Generic argument is a struct", "NonGenStruct", "F`1[T]");
		Check(new Case(Instantiation_DefaultCtorAndClassTypeConstraint.Test14), "Generic argument is a generic struct", "GenStruct[System.Int32]", "F`1[T]");


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

