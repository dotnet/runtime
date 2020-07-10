// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Reflection;

[assembly:	   AssemblyVersionAttribute("1.0.0.0")]

public class server3 
{
  public int trivial()
  {
	Console.WriteLine ("server3.trivial");
	return 3;
  }
}
