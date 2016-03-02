// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Reflection;

//[assembly: AssemblyKeyFile("..\\..\\compatkey.dat")]


public class server1// : MarshalByRefObject 
{
  public int trivial()
  {
	Console.WriteLine ("server1.trivial");
	Console.WriteLine ("simple named");
	return 1;
  }
}