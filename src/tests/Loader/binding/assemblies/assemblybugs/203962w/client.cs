// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using System.Reflection;
using Xunit;

public class client
{

	[Fact]
	public static int TestEntryPoint()
	{

		try
		{

			Console.WriteLine ("expect a FileLoadException");
			AssemblyName assemblyName = new AssemblyName("System, PublicKeyToken=00000000000000000400000000000000");
			Console.WriteLine(assemblyName.FullName);
		}
		catch (FileLoadException e)
		{
			Console.WriteLine (e);
			Console.WriteLine ("expected exception!");
			Console.WriteLine ("test pass");
			return 100;
		}
		catch (Exception e)
		{
			Console.WriteLine (e);
			Console.WriteLine ("unexpected exception");
			Console.WriteLine ("test fails");
			return 1;
		}
		Console.WriteLine ("no exception");
		Console.WriteLine ("test fails");
		return 1;
	}

}
