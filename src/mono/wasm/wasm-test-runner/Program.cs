// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.IO;

public static class Program
{
    public static int Main(string[] args)
    {
	try
        {
		string assemblyName = args[0];

		Console.WriteLine("Load");
		var assembly = Assembly.Load(assemblyName);

		Console.WriteLine("EntryPoint");
		MethodInfo mainMethod = assembly.EntryPoint;


		Console.WriteLine("Invoke");
	
		if (mainMethod != null)
		{	
			ParameterInfo[] mainMethodParameters = mainMethod.GetParameters();

			if ( mainMethodParameters.Length > 0)
		        {
				return (int)mainMethod.Invoke (null, new object [] { args } );
			}
		        else
			{
				return (int)mainMethod.Invoke (null, null);
			}
		}
		else
		{
			return 72345;
		}

	}
	catch(Exception e)
	{
		Console.WriteLine(e);
		throw;
	}
    }
}
