// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;
using TestLibrary;

// test for classes
public class A<T> {}
public class B : A<C> {}
public class C : B {} 


// test for interfaces
public interface IA<T> {}
public interface IB : IA<D> {}
public class D : IB {} 
  
                                       

public class Test_test137310
{

	public static void LoadC()
	{
		A<C> c = new C();		
	}

	public static void LoadD()
	{
		IA<D> d = new D();		
	}

 [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
	[Fact]
	public static int TestEntryPoint()
    	{
    		bool pass = true;
    		try
    		{
    			LoadC();
    		}
		catch (Exception e)
		{
			Console.WriteLine("FAIL to load C: Caught unexpected exception: " + e);
			pass = false;
		}

	    	try
    		{
    			LoadD();
    		}
		catch (Exception e)
		{
			Console.WriteLine("FAIL to load D: Caught unexpected exception: " + e);
			pass = false;
		}


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
