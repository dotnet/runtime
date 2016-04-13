// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This test is a regression test for VSWhidbey 298006
// The struct has an objref and is of odd size.
// The GC requires that all valuetypes containing objrefs be sized to a multiple of sizeof(void*) )== 4).
// Since the size of this struct was 17 we were throwing a TypeLoadException.

using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Explicit)]
public struct S
{
    [FieldOffset(16), MarshalAs(UnmanagedType.VariantBool)] public bool b;
    [FieldOffset(8)] public double d;
    [FieldOffset(0), MarshalAs(UnmanagedType.BStr)] public string st;
}



public class Test
{
        public static void Run()
	{
		S s;
		s.b = true;
	}
        
	public static int Main()
	{
		try
		{
			Run();

			Console.WriteLine("PASS");
			return 100;
		}
		catch (TypeLoadException e)
		{
			Console.WriteLine("FAIL: Caught unexpected TypeLoadException: {0}", e.Message);
			return 101;
		}
		catch (Exception e)
		{
			Console.WriteLine("FAIL: Caught unexpected Exception: {0}", e.Message);
			return 101;
		}
	}

 }
