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
	string assemblyName = args[0];

	var assembly = Assembly.Load(assemblyName);
	var mainMethod = assembly.EntryPoint;
	mainMethod.Invoke (null, new object [] { args } );

	return 0;
    }
}
