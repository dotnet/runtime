// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using Xunit;

delegate int Int32_VoidDelegate();
public class TestClass
{
	[Fact]
	public static int TestEntryPoint() 
	{
		int iTestCount= 0;
		int iErrorCount= 0;
		String LocStr = "loc_000";
		int iExitCode = 101; //101: fail; 100: pass
		try
		{
			//The return value of Delegate.GetHashcode must not be persisted for two reasons. 
			//First, the hash function of a class might be altered to generate a better 
			//distribution, rendering any values from the old hash function useless. 
			//Second, the default implementation of this class does not guarantee that 
			//the same value will be returned by different instances.


			{
				LocStr = "loc_001";
				iTestCount++;
				Console.WriteLine( "test1: GetHashCode of delegate pointing to static method" );
				Int32_VoidDelegate sdg1 = new Int32_VoidDelegate( staticMethInt32_Void );

				int ihc1 = sdg1.GetHashCode();
				int ihc2 = sdg1.GetHashCode();
				if( ihc2 != ihc1 )
				{
					Console.WriteLine( "Err_001 : should be equal, but one is :" +  ihc1 + " other one is " + ihc2);
					iErrorCount++;
				}
			}

			{
				LocStr = "loc_002";
				iTestCount++;
				Console.WriteLine( "test1: GetHashCode of delegate pointing to instance method " );
				TestClass obj = new TestClass();
				Int32_VoidDelegate sdg2 = new Int32_VoidDelegate( obj.instanceMethInt32_Void );
			
				int ihc1 = sdg2.GetHashCode();
				int ihc2 = sdg2.GetHashCode();
				if( ihc2 != ihc1 )
				{
					Console.WriteLine( "Err_002 : should be equal, but one is :" +  ihc1 + " other one is " + ihc2);
					iErrorCount++;
				}
			}
		}
		catch( Exception e )
		{
			Console.WriteLine( LocStr + " : unexpected " + e.ToString() );
			iErrorCount++;
		}

		if( iErrorCount > 0)
		{
			Console.WriteLine( "Total tests count" +  iTestCount + " . Failed tests count" + iErrorCount);
			iExitCode = 101;
		}
		else
		{
			Console.WriteLine( "Total tests count" +  iTestCount + " . All passed" );
			iExitCode = 100;
		}
		return iExitCode;
	}

	public static int staticMethInt32_Void()
	{
		Console.WriteLine( "Invoked staticMethVoid_Void method");
		return 77;
	}

	public int instanceMethInt32_Void()
	{
		Console.WriteLine( "Invoked staticMethVoid_Void1 method");
		return 66;
	}
}

