// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.IO;
using System.Reflection;

/*
 * This is a code coverage test meant to address some low coverage wrt to random Assembly attributes
 * AssemblyCompanyAttribute
 * AssemblyCopyrightAttribute
 * AssemblyCultureAttribute
 * AssemblyFileVersionAttribute
 * AssemblyInformationalVersion
 * AssemblyProductAttribute
 * AssemblyTrademarkAttribute
 * AssemblyVersionAttribute
 */

public class AssemblyName
{
	public static int Main()
	{
		//random Assembly Attributes
		Console.WriteLine((new AssemblyCompanyAttribute("my company")).Company);
		Console.WriteLine((new AssemblyCopyrightAttribute("my copyright")).Copyright);
		Console.WriteLine((new AssemblyCultureAttribute("my culture")).Culture);
		Console.WriteLine((new AssemblyFileVersionAttribute("my version")).ToString());
		try
		{
			new AssemblyFileVersionAttribute(null);
		}
		catch(ArgumentNullException)
		{}
		Console.WriteLine((new AssemblyInformationalVersionAttribute("my informational")).InformationalVersion);
		Console.WriteLine((new AssemblyProductAttribute("my product")).Product);
		Console.WriteLine((new AssemblyTrademarkAttribute("my trademark")).Trademark);
		Console.WriteLine((new AssemblyVersionAttribute("my version")).Version);
		return 100;
	}
}
