// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Reflection;

[assembly: System.Reflection.AssemblyVersion("0.0.0.0")]


#if DESKTOP
public class server2 : MarshalByRefObject
#else
       public class server2 
#endif
{
  public int trivial()
  {
	Console.WriteLine ("server2.trivial");
	Console.WriteLine ("strongly named");
	return 2;
  }
}