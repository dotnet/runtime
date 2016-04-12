// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/* Test various combinations of constraints with legal parameter types by instantiating the types

CONSTRAINTS:

default ctor
reference type 
valuetype
default ctor, reference type
default ctor, valuetype

Test each constraint with (whatever applies for positive testing)
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
	
	static void Check(Case mytest, string testName)
    	{

		Console.Write("Test"+testNumber + ": " + testName);
		++testNumber;

		
		try
		{
			mytest();

			
			Console.WriteLine(" : PASS");
			return;
		}
		catch (TypeLoadException e)
		{
			Console.WriteLine("\nFAIL: Caught unexpected TypeLoadException: " + e);
			pass = false;
			return;		
		}
	
		catch (Exception e) 
		{
			Console.WriteLine("\nFAIL: Caught unexpected exception: " + e);
			pass = false;
		}	

	}
	

	public static int Main()
	{
		pass = true;

		Console.WriteLine("\nType: A<T> where T : new()\n");
		
		Console.WriteLine("POSITIVE TESTS");
		Check(new Case(DefaultCtorConstraint.Test1), "Generic argument is a class with default ctor");
		Check(new Case(DefaultCtorConstraint.Test3), "Generic argument is a struct (valuetypes have public nullary ctors by default)");
		Check(new Case(DefaultCtorConstraint.Test5), "Generic argument is an mscorlib class with default ctor");
		Check(new Case(DefaultCtorConstraint.Test8), "Generic argument is an enum");
   		Check(new Case(DefaultCtorConstraintGenTypes.Test1), "Generic argument is a generic class with default ctor");
		Check(new Case(DefaultCtorConstraintGenTypes.Test3), "Generic argument is a generic struct");
		Check(new Case(DefaultCtorConstraintGenTypes.Test6), "Generic argument is Nullable<T>");
	

		Console.WriteLine("\nType: A<T> where T : class()\n");

		Console.WriteLine("POSITIVE TESTS");
		Check(new Case(ClassConstraint.Test1), "Generic argument is a class with default ctor");
		Check(new Case(ClassConstraint.Test2), "Generic argument is a class with no default ctor");
		Check(new Case(ClassConstraint.Test4), "Generic argument is a delegate");
		Check(new Case(ClassConstraint.Test5), "Generic argument is an mscorlib class with default ctor");
		Check(new Case(ClassConstraint.Test6), "Generic argument is an mscorlib abstract class with no default ctor");
		Check(new Case(ClassConstraint.Test7), "Generic argument is an interface");
		Check(new Case(ClassConstraint.Test9), "Generic argument is an array of classes with default ctor");
		Check(new Case(ClassConstraintGenTypes.Test1), "Generic argument is a generic class with default ctor");
		Check(new Case(ClassConstraintGenTypes.Test2), "Generic argument is a generic class with no default ctor");
		Check(new Case(ClassConstraintGenTypes.Test5), "Generic argument is a generic interface");

		
		Console.WriteLine("\nType: A<T> where T : struct()\n");

		Console.WriteLine("POSITIVE TESTS");
		Check(new Case(StructConstraint.Test3), "Generic argument is a struct");
		Check(new Case(StructConstraint.Test8), "Generic argument is an enum");
		Check(new Case(StructConstraint.Test9), "Generic argument is an mscorlib struct");
		Check(new Case(StructConstraintGenTypes.Test3), "Generic argument is a generic struct");
		Check(new Case(StructConstraintGenTypes.Test6), "Generic argument is a generic mscorlib struct");



		Console.WriteLine("\nType: A<T> where T : class(), new() \n");

		Console.WriteLine("POSITIVE TESTS");
		Check(new Case(DefaultCtorAndClassConstraint.Test1), "Generic argument is a class with default ctor");
		Check(new Case(DefaultCtorAndClassConstraint.Test5), "Generic argument is a is an mscorlib class with default ctor");	
		Check(new Case(DefaultCtorAndClassConstraintGenTypes.Test1), "Generic argument is a generic class with default ctor");
		
	
		Console.WriteLine("\nType: A<T> where T : struct(), new()\n");

		Console.WriteLine("POSITIVE TESTS");
		Check(new Case(DefaultCtorAndStructConstraint.Test3), "Generic argument is a struct");
		Check(new Case(DefaultCtorAndStructConstraint.Test9), "Generic argument is an mscorlib struct");
		Check(new Case(DefaultCtorAndStructConstraintGenTypes.Test3), "Generic argument is a generic struct");
		Check(new Case(DefaultCtorAndStructConstraintGenTypes.Test6), "Generic argument is a generic mscorlib struct");
		Check(new Case(DefaultCtorAndStructConstraint.Test8), "Generic argument is an enum");
	

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

