// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

internal class Program
{
	private static int Main()
	{
		int iErrorCount = 0;
		String tstr = null;
		try{
			S<int> i = new S<int>();
			tstr = i.ToString();
			System.Console.WriteLine(tstr);
			if( tstr != "Program+S`1[System.Int32]" ) 
			{
				Console.WriteLine( "Err_01: Expected type: Program+S`1[System.Int32]" );
				Console.WriteLine( "Err_01: Actualy get type: " + tstr );
				iErrorCount++;
			}
			S<object> o = new S<object>();
			tstr = o.ToString();		
			System.Console.WriteLine(tstr);
			if( tstr != "Program+S`1[System.Object]" ) 
			{
				Console.WriteLine( "Err_02: Expected type:  Program+S`1[System.Object]" );
				Console.WriteLine( "Err_02: Actualy get type: " + tstr );
				iErrorCount++;
			}
			
			S<string> s = new S<string>();
			tstr = s.ToString();
			System.Console.WriteLine(tstr);
			if( tstr != "Program+S`1[System.String]" ) 
			{
				Console.WriteLine( "Err_01: Expected type:  Program+S`1[System.String]" );
				Console.WriteLine( "Err_01: Actualy get type: " + tstr );
				iErrorCount++;
			}			
			S<Program> p = new S<Program>();
			tstr = p.ToString();
			System.Console.WriteLine(tstr);
			if( tstr != "Program+S`1[Program]" ) 
			{
				Console.WriteLine( "Err_01: Expected type:  Program+S`1[Program]" );
				Console.WriteLine( "Err_01: Actualy get type: " + tstr );
				iErrorCount++;
			}						
		}catch( Exception e)
		{
			Console.WriteLine( "Unexpected: " + e );
			iErrorCount++;	
		}
		if( iErrorCount > 0 )
		{
			Console.WriteLine( "Test Failed" );
			return 101;
		}
		else {
			Console.WriteLine( "Test passed" );
			return 100;
		}
		
	}

	public struct S<T>
	{
#pragma warning disable 0414
		public static S<T> Foo = new S<T>();
#pragma warning restore 0414

	}
}
