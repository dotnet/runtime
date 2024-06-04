// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.IO;
using Xunit;

class MyException : Exception
{
}

public class Help
{
	public static Exception s_exceptionToThrow;
	public static bool s_matchingException;

	public static Object s_object = new object();
}

public class A<T> where T : Exception
{

	public void InstanceFunctionWithFewArgs()
	{
		try
		{
			throw Help.s_exceptionToThrow;
		}
		catch (T match)
		{
			if (!Help.s_matchingException)
				throw new Exception("This should not have been caught here", match);

			Console.WriteLine("Caught matching " + match.GetType());
		}
		catch(Exception mismatch)
		{
			if (Help.s_matchingException)
				throw new Exception("Should have been caught above", mismatch);

			Console.WriteLine("Expected mismatch " + mismatch.GetType());
		}
	}

	public void InstanceFunctionWithManyArgs(int i, int j, int k, object o)
	{
		try
		{
			throw Help.s_exceptionToThrow;
		}
		catch (T match)
		{
			if (!Help.s_matchingException)
				throw new Exception("This should not have been caught here", match);

			Console.WriteLine("Caught matching " + match.GetType());
		}
		catch(Exception mismatch)
		{
			if (Help.s_matchingException)
				throw new Exception("Should have been caught above", mismatch);

			Console.WriteLine("Expected mismatch " + mismatch.GetType());
		}
	}

	public static void StaticFunctionWithFewArgs()
	{
		try
		{
			throw Help.s_exceptionToThrow;
		}
		catch (T match)
		{
			if (!Help.s_matchingException)
				throw new Exception("This should not have been caught here", match);

			Console.WriteLine("Caught matching " + match.GetType());
		}
		catch(Exception mismatch)
		{
			if (Help.s_matchingException)
				throw new Exception("Should have been caught above", mismatch);

			Console.WriteLine("Expected mismatch " + mismatch.GetType());
		}	}

	public static void StaticFunctionWithManyArgs(int i, int j, int k, object o)
	{
		try
		{
			throw Help.s_exceptionToThrow;
		}
		catch (T match)
		{
			if (!Help.s_matchingException)
				throw new Exception("This should not have been caught here", match);

			Console.WriteLine("Caught matching " + match.GetType());
		}
		catch(Exception mismatch)
		{
			if (Help.s_matchingException)
				throw new Exception("Should have been caught above", mismatch);

			Console.WriteLine("Expected mismatch " + mismatch.GetType());
		}
	}

	public static void GenericFunctionWithFewArgs<X>() where X : Exception
	{
		try
		{
			throw Help.s_exceptionToThrow;
		}
		catch (X match)
		{
			if (!Help.s_matchingException)
				throw new Exception("This should not have been caught here", match);

			Console.WriteLine("Caught matching " + match.GetType());
		}
		catch(Exception mismatch)
		{
			if (Help.s_matchingException)
				throw new Exception("Should have been caught above", mismatch);

			Console.WriteLine("Expected mismatch " + mismatch.GetType());
		}
	}

	public static void GenericFunctionWithManyArgs<X>(int i, int j, int k, object o) where X : Exception
	{
		try
		{
			throw Help.s_exceptionToThrow;
		}
		catch (X match)
		{
			if (!Help.s_matchingException)
				throw new Exception("This should not have been caught here", match);

			Console.WriteLine("Caught matching " + match.GetType());
		}
		catch(Exception mismatch)
		{
			if (Help.s_matchingException)
				throw new Exception("Should have been caught above", mismatch);

			Console.WriteLine("Expected mismatch " + mismatch.GetType());
		}
	}
}

public struct Struct<T> where T : Exception
{

	public void StructInstanceFunctionWithFewArgs()
	{
		try
		{
			throw Help.s_exceptionToThrow;
		}
		catch (T match)
		{
			if (!Help.s_matchingException)
				throw new Exception("This should not have been caught here", match);

			Console.WriteLine("Caught matching " + match.GetType());
		}
		catch(Exception mismatch)
		{
			if (Help.s_matchingException)
				throw new Exception("Should have been caught above", mismatch);

			Console.WriteLine("Expected mismatch " + mismatch.GetType());
		}
	}

	public void StructInstanceFunctionWithManyArgs(int i, int j, int k, object o)
	{
		try
		{
			throw Help.s_exceptionToThrow;
		}
		catch (T match)
		{
			if (!Help.s_matchingException)
				throw new Exception("This should not have been caught here", match);

			Console.WriteLine("Caught matching " + match.GetType());
		}
		catch(Exception mismatch)
		{
			if (Help.s_matchingException)
				throw new Exception("Should have been caught above", mismatch);

			Console.WriteLine("Expected mismatch " + mismatch.GetType());
		}
	}
}

public class GenericExceptions
{
	public static void InstanceFunctionWithFewArgs()
	{
		Help.s_matchingException = true;
		Help.s_exceptionToThrow = new MyException();
		(new A<Exception>()).InstanceFunctionWithFewArgs();
		(new A<MyException>()).InstanceFunctionWithFewArgs();

		Help.s_matchingException = false;
		Help.s_exceptionToThrow = new Exception();
		(new A<MyException>()).InstanceFunctionWithFewArgs();
	}

	public static void InstanceFunctionWithManyArgs()
	{
		Help.s_matchingException = true;
		Help.s_exceptionToThrow = new MyException();
		(new A<Exception>()).InstanceFunctionWithManyArgs(1, 2, 3, Help.s_object);
		(new A<MyException>()).InstanceFunctionWithManyArgs(1, 2, 3, Help.s_object);

		Help.s_matchingException = false;
		Help.s_exceptionToThrow = new Exception();
		(new A<MyException>()).InstanceFunctionWithManyArgs(1, 2, 3, Help.s_object);
	}

	public static void StaticFunctionWithFewArgs()
	{
		Help.s_matchingException = true;
		Help.s_exceptionToThrow = new MyException();
		A<Exception>.StaticFunctionWithFewArgs();
		A<MyException>.StaticFunctionWithFewArgs();

		Help.s_matchingException = false;
		Help.s_exceptionToThrow = new Exception();
		A<MyException>.StaticFunctionWithFewArgs();
	}

	public static void StaticFunctionWithManyArgs()
	{
		Help.s_matchingException = true;
		Help.s_exceptionToThrow = new MyException();
		A<Exception>.StaticFunctionWithManyArgs(1, 2, 3, Help.s_object);
		A<MyException>.StaticFunctionWithManyArgs(1, 2, 3, Help.s_object);

		Help.s_matchingException = false;
		Help.s_exceptionToThrow = new Exception();
		A<MyException>.StaticFunctionWithManyArgs(1, 2, 3, Help.s_object);
	}

	public static void GenericFunctionWithFewArgs()
	{
		Help.s_matchingException = true;
		Help.s_exceptionToThrow = new MyException();
		A<DivideByZeroException>.GenericFunctionWithFewArgs<Exception>();
		A<DivideByZeroException>.GenericFunctionWithFewArgs<MyException>();

		Help.s_matchingException = false;
		Help.s_exceptionToThrow = new Exception();
		A<DivideByZeroException>.GenericFunctionWithFewArgs<MyException>();
	}

	public static void GenericFunctionWithManyArgs()
	{
		Help.s_matchingException = true;
		Help.s_exceptionToThrow = new MyException();
		A<DivideByZeroException>.GenericFunctionWithManyArgs<Exception>(1, 2, 3, Help.s_object);
		A<DivideByZeroException>.GenericFunctionWithManyArgs<MyException>(1, 2, 3, Help.s_object);

		Help.s_matchingException = false;
		Help.s_exceptionToThrow = new Exception();
		A<DivideByZeroException>.GenericFunctionWithManyArgs<MyException>(1, 2, 3, Help.s_object);
	}

	public static void StructInstanceFunctionWithFewArgs()
	{
		Help.s_matchingException = true;
		Help.s_exceptionToThrow = new MyException();
		(new Struct<Exception>()).StructInstanceFunctionWithFewArgs();
		(new Struct<MyException>()).StructInstanceFunctionWithFewArgs();

		Help.s_matchingException = false;
		Help.s_exceptionToThrow = new Exception();
		(new Struct<MyException>()).StructInstanceFunctionWithFewArgs();
	}

	public static void StructInstanceFunctionWithManyArgs()
	{
		Help.s_matchingException = true;
		Help.s_exceptionToThrow = new MyException();
		(new Struct<Exception>()).StructInstanceFunctionWithManyArgs(1, 2, 3, Help.s_object);
		(new Struct<MyException>()).StructInstanceFunctionWithManyArgs(1, 2, 3, Help.s_object);

		Help.s_matchingException = false;
		Help.s_exceptionToThrow = new Exception();
		(new Struct<MyException>()).StructInstanceFunctionWithManyArgs(1, 2, 3, Help.s_object);
	}

        [System.Runtime.CompilerServices.MethodImpl(
          System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
	[Fact]
	public static int TestEntryPoint()
	{
	    try
	    {
		Console.WriteLine("This test checks that we can catch generic exceptions.");
		Console.WriteLine("All exceptions should be handled by the test itself");

		InstanceFunctionWithFewArgs();
		InstanceFunctionWithManyArgs();

		StaticFunctionWithFewArgs();
		StaticFunctionWithManyArgs();

		GenericFunctionWithFewArgs();
		GenericFunctionWithManyArgs();

		StructInstanceFunctionWithFewArgs();
		StructInstanceFunctionWithManyArgs();

	    }
	    catch(Exception)
	    {
		Console.WriteLine("Test Failed");
		return -1;
	    }

	    Console.WriteLine("Test Passed");
	    return 100;
	}

}
