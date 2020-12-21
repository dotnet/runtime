// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Reflection;

[assembly: System.Reflection.AssemblyVersion("0.0.0.0")]

public class server2 
{
  public int trivial()
  {
	Console.WriteLine ("server2.trivial");
	Console.WriteLine ("strongly named");
	return 2;
  }
}
