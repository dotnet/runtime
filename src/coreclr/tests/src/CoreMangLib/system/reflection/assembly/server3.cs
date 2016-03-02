// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Reflection;

//[assembly: AssemblyKeyFile("..\\..\\compatkey.dat")]


public class server3
{
  public int trivial()
  {
	TestLibrary.Logging.WriteLine ("server.trivial");
	return 3;
  }
}
