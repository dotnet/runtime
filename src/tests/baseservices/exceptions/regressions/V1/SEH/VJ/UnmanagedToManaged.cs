// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime.InteropServices;

public class UnmanagedToManaged {

    ///** @dll.import("Unmanaged.dll")*/
        [System.Runtime.InteropServices.DllImport("unmanaged.dll")]
	public static extern void UnmanagedCode( int i) ;

	public static int Main(String []args){
		String s = "Done";
		int retVal = 0;
		try {
			Console.WriteLine("Calling unmanaged code...");
			UnmanagedCode(0);
			Console.WriteLine("...Returned from unmanaged code");
		}
		catch (DivideByZeroException )
		{
			Console.WriteLine("Caught a div-by-zero exception.");
			retVal = 100;
		}
		catch (Exception )
		{
			Console.WriteLine("Caught a general exception");
		}
		Console.WriteLine(s);
		return retVal;
	}

}
