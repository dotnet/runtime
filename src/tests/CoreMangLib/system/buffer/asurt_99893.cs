// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ASURT 99893 - These Buffer members were correctly checking that the array was a
// primitive type, but the check for primitive type did not include the assembly,
// and thus one could define a new System.Int32 (as below) and use that instead.

using System;
using Xunit;

namespace System
{
    public class ASURT_99893
    {
	[Fact]
	public static int TestEntryPoint()
	{
	    Boolean pass=true;
#pragma warning disable 0436
	    Int32 a = new Int32();
	    a.Init("asdas");
	    TestLibrary.Logging.WriteLine(a);
	    Int32[] foo = new Int32 [] {a};
#pragma warning restore 0436
	    byte b = 0;

	    // GetByte
	    try
	    {
		b = Buffer.GetByte(foo, 0);
		pass=false;
		TestLibrary.Logging.WriteLine ("GetByte: No exception thrown!  Got 0x{0:x}", b);
	    }
	    catch (ArgumentException ex)
	    {
		TestLibrary.Logging.WriteLine("GetByte: Got expected exception: {0}: {1}", ex.GetType(), ex.Message);
	    }
	    catch (Exception ex)
	    {
		pass=false;
		TestLibrary.Logging.WriteLine("GetByte: Unexpected exception thrown: " + ex);
	    }
	    
	    // SetByte
	    try
	    {
		Buffer.SetByte(foo, 0, (Byte)2);
		pass=false;
		TestLibrary.Logging.WriteLine ("SetByte: No exception thrown!  Got 0x{0:x}", b);
	    }
	    catch (ArgumentException ex)
	    {
		TestLibrary.Logging.WriteLine("SetByte: Got expected exception: {0}: {1}", ex.GetType(), ex.Message);
	    }
	    catch (Exception ex)
	    {
		pass=false;
		TestLibrary.Logging.WriteLine("SetByte: Unexpected exception thrown: " + ex);
	    }

	    // BlockCopy
	    try
	    {
		Object[] arrObjects = new Object[3];
		Buffer.BlockCopy(foo, 0, arrObjects, 0, 4);
		pass=false;
		TestLibrary.Logging.WriteLine ("BlockCopy: No exception thrown!  Got 0x{0:x}", b);
	    }
	    catch (ArgumentException ex)
	    {
		TestLibrary.Logging.WriteLine("BlockCopy: Got expected exception: {0}: {1}", ex.GetType(), ex.Message);
	    }
	    catch (Exception ex)
	    {
		pass=false;
		TestLibrary.Logging.WriteLine("BlockCopy: Unexpected exception thrown: " + ex);
	    }
	    
	    if (pass)
	    {
		TestLibrary.Logging.WriteLine("Test passed.");
		return 100;
	    }
	    else
	    {
		TestLibrary.Logging.WriteLine("Test failed.");
		return 1;
	    }
	}
    }
	
    public struct Int32 
    {
	object value;
	public void Init (object o) 
	{
	    value = o;
	}
		
	override public string ToString () 
	{
	    string s = "MyInt32";
	    if (value == null) s += "<null>";
	    else s += value.ToString();
	    return s;
	}
    }
}
