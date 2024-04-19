// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

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

public class Test_derivedexplicitclass
{
	[Fact]
	public static int TestEntryPoint()
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
