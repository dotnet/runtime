// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*	
Open and write to a file inside static class constructor of a class/struct (eager and beforefieldinit cases). 
Access type's static field (which would trigger the .cctor)
Expected: Should get no exceptions.

*/

using System;
using System.IO;
using Xunit;

public class MyClass
{	
	public static int counter;
	
	static MyClass()
	{
		Console.WriteLine("Inside class cctor");
		
		File.WriteAllText("file.txt", "inside MyClass.cctor");

		counter++;
	}
}

public struct MyStruct
{

	public static int counter;
	
	static MyStruct()
	{
		Console.WriteLine("Inside struct cctor");
		
		File.WriteAllText("file.txt", "inside MyClass.cctor");

		counter++;
	}
}


public class Test_CctorOpenFile
{
	[Fact]
	public static int TestEntryPoint()
	{

		int ret;
		try
		{
			File.WriteAllText("file.txt", "inside Main");
			
			
			if (MyClass.counter == 1 && MyStruct.counter == 1)
			{			
				Console.WriteLine("PASS");
				ret = 100;	
			}
			else
			{
				Console.WriteLine("Fail: One of the .cctors wasn't called");
				ret = 101;
			}

			if (File.Exists("file.txt"))
			{
				File.Delete("file.txt");
			}

			return ret;
		}
		catch (Exception e)
		{
			Console.WriteLine("FAIL: Caught unexpected exception: " + e);
			return 102;
		}
		
	}
}
