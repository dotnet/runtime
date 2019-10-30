// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

class ReflectObj
{
	public static int Main( String [] str )
	{
		Random rand = null;
		try
		{
			rand.Next();
		}
		catch (NullReferenceException)
		{
			Console.WriteLine("Got expected NullReferenceException");
			Console.WriteLine("PASS");
			return 100;
		}
		catch (Exception e)
		{
			Console.WriteLine("Got unexpected exception: {0}", e);
		}

		Console.WriteLine("FAIL");
		return 0;
	}
}

