// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// this is a regression test for VSWhidbey 359519
// a struct Root, has a static field that appears earlier in the metadata than a valuetype instance field.

using System; 
using System.Runtime.InteropServices; 
using Xunit;

public class MainClass 

{ 
    //Variable 
    [StructLayout(LayoutKind.Explicit, Size=1, Pack=1, CharSet=CharSet.Unicode)]
    public struct Variable 
    { 
        [FieldOffset(0), MarshalAs(UnmanagedType.I1)] 
        public bool boolean; // A boolean field marshalled as 1 byte) 
    } 

    [StructLayout(LayoutKind.Explicit, Size=2, Pack=1, CharSet=CharSet.Unicode)] 
    public struct Root 
    { 
 	 public static byte byte1; 
        [FieldOffset(8)] 
        public Variable var1; 
    } 

    [Fact]
    public static int TestEntryPoint() 
    { 
    	try
    	{
		Root r = new Root();

		// to remove compiler warning
		// warning CS0219: The variable 'r' is assigned but its value is never used

		r.ToString();
		Console.WriteLine("PASS");
		return 100;
    	}
	catch (Exception e)
	{
		Console.WriteLine("FAIL: Caught unexpected exception - " + e);
		return 101;
	}
    } 
}
