// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Explicit)]
// non-generic base class
public class Base
{
}

// ... AND subclass is explicit
[StructLayout(LayoutKind.Explicit)]
public class Sub : Base
{	
  // and field is at offset 8
  [FieldOffset(8)]public object Fld1;
}

public class Test
{
	public static int Main()
	{
		try
		{
      			new Sub();
		       Console.WriteLine("PASS");
			   
			return 100;
		}
		catch (TypeLoadException e)
		{
			Console.WriteLine("FAIL: Caught TypeLoadException: " + e.Message);
			return 101;
		}
		catch (Exception e)
		{
			Console.WriteLine("FAIL: Caught unexpected exception: " + e.Message);
			return 101;
		}
	}
}
