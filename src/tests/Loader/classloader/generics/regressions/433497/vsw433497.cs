// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;

public class Map<K,D> {}

public class C 
{
	public static int Main()
 	{
    		Type t = Type.GetType("Map`2[System.Int32,System.Int32]");

		Console.WriteLine("PASS");
		return 100;
  	}
}
