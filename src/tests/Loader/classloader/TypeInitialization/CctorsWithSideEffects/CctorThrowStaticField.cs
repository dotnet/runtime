// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*

using TestLibrary;
A .cctor has only one chance to run in any appdomain. 
If it fails, the 2nd time we try to access a static field we check if .cctor has been run. And it has, but failed so we fail again.

Test_CctorThrowStaticField throws an exception inside .cctor.
Try to access a static field twice.
Expected: Should return the same exception.

*/

using System;
using System.Runtime.CompilerServices;
using Xunit;
using TestLibrary;


public class A 
{
	public static int i;
	
	static A()
	{
		Console.WriteLine("In A.cctor");

		A.i = 5;
		
		throw new Exception();
	}
}


public struct B 
{
	public static int i;
	
	static B()
	{
		Console.WriteLine("In B.cctor");

		B.i = 5;
		
		throw new Exception();
	}
}


public class Test_CctorThrowStaticField
{	
 [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
	[Fact]
	public static int TestEntryPoint()
	{ 
		bool result = true;
		
		try
		{
			Console.WriteLine("Accessing class's static field");
			Console.WriteLine("A.i: " +A.i);
			Console.WriteLine("Did not catch expected TypeInitializationException exception");
			result = false;
		}
		catch (TypeInitializationException)
		{
			Console.WriteLine("Caught expected exception 1st time");
		}
		catch (Exception e)
		{
			Console.WriteLine("Caught unexpected exception 1st time: " + e);
			result = false;
		}


		try
		{
			Console.WriteLine("A.i: " +A.i);
			Console.WriteLine("Did not catch expected TypeInitializationException exception");
			result = false;
		}
		catch (TypeInitializationException)
		{
			Console.WriteLine("Caught expected exception 2nd time\n");
		}
		catch (Exception e)
		{
			Console.WriteLine("Caught unexpected exception 2nd time: " + e);
			result = false;
		}


		Console.WriteLine("Accessing struct's static field");
		try
		{
			Console.WriteLine("B.i: " +B.i);
			Console.WriteLine("Did not catch expected TypeInitializationException exception");
			result = false;
		}
		catch (TypeInitializationException)
		{
			Console.WriteLine("Caught expected exception 1st time");
		}
		catch (Exception e)
		{
			Console.WriteLine("Caught unexpected exception 1st time: " + e);
			result = false;
		}


		try
		{
			Console.WriteLine("B.i: " +B.i);
			Console.WriteLine("Did not catch expected TypeInitializationException exception");
			result = false;
		}
		catch (TypeInitializationException)
		{
			Console.WriteLine("Caught expected exception 2nd time\n");
		}
		catch (Exception e)
		{
			Console.WriteLine("Caught unexpected exception 2nd time: " + e);
			result = false;
		}

		if (result)
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

public class ReentrantClassInitialization
{
    private sealed class Success { }
    private sealed class Failure { }

    private static class State<T>
    {
        public static int DirectCalls;
        public static int FirstCalls;
        public static int SecondCalls;
    }

    private static class Direct<T>
    {
        public static int Value;

        static Direct()
        {
            Assert.Equal(1, ++State<T>.DirectCalls);
            RuntimeHelpers.RunClassConstructor(typeof(Direct<T>).TypeHandle);
            RuntimeHelpers.RunClassConstructor(typeof(Direct<T>).TypeHandle);
            Assert.Equal(0, Value);
            Value = 42;
            if (typeof(T) == typeof(Failure))
            {
                throw new InvalidOperationException("Direct initialization failed");
            }
        }
    }

    private static class First<T>
    {
        public static int Value;

        static First()
        {
            Assert.Equal(1, ++State<T>.FirstCalls);
            RuntimeHelpers.RunClassConstructor(typeof(Second<T>).TypeHandle);
            Assert.Equal(7, Second<T>.Value);
            Value = 42;
            if (typeof(T) == typeof(Failure))
            {
                throw new InvalidOperationException("Indirect initialization failed");
            }
        }
    }

    private static class Second<T>
    {
        public static int Value;

        static Second()
        {
            Assert.Equal(1, ++State<T>.SecondCalls);
            // Neither rejected acquisition may release First's still-held entry lock.
            RuntimeHelpers.RunClassConstructor(typeof(First<T>).TypeHandle);
            RuntimeHelpers.RunClassConstructor(typeof(First<T>).TypeHandle);
            Assert.Equal(0, First<T>.Value);
            Value = 7;
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public static void ReentryAndRepeatedAccess(bool indirect, bool throws)
    {
        if (throws)
        {
            CheckRepeatedAccess<Failure>(indirect);
        }
        else
        {
            CheckRepeatedAccess<Success>(indirect);
        }
    }

    private static void CheckRepeatedAccess<T>(bool indirect)
    {
        Type type = indirect ? typeof(First<T>) : typeof(Direct<T>);
        Func<int> readValue = indirect ? () => First<T>.Value : () => Direct<T>.Value;
        Exception cachedException = null;

        for (int i = 0; i < 3; i++)
        {
            if (typeof(T) == typeof(Failure))
            {
                TypeInitializationException exception = Assert.Throws<TypeInitializationException>(
                    () => RuntimeHelpers.RunClassConstructor(type.TypeHandle));
                Assert.IsType<InvalidOperationException>(exception.InnerException);
                Assert.Equal(indirect ? "Indirect initialization failed" : "Direct initialization failed",
                    exception.InnerException.Message);
                if (cachedException is not null)
                {
                    Assert.Same(cachedException, exception.InnerException);
                }
                cachedException = exception.InnerException;
                exception = Assert.Throws<TypeInitializationException>(() => { _ = readValue(); });
                Assert.Same(cachedException, exception.InnerException);
            }
            else
            {
                RuntimeHelpers.RunClassConstructor(type.TypeHandle);
                Assert.Equal(42, readValue());
            }

            Assert.Equal(1, indirect ? State<T>.FirstCalls : State<T>.DirectCalls);
            if (indirect)
            {
                Assert.Equal(1, State<T>.SecondCalls);
                Assert.Equal(7, Second<T>.Value);
            }

            GC.Collect();
        }
    }
}
