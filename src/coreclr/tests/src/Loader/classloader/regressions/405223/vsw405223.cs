// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Regression test for VSW 405223
// We shouldn't be able to cast from short[] to char[]  or from char[] to short[]
// since that is the behavior in Everett and we should be consistent in Whidbey.

using System;

class Class1
{
	public static int Main() 
	{
	        object o1 = new short[3];
	        object o2 = new char[3];
			
	        if(o1 is char[] || o2 is short[])
	        {
			Console.WriteLine("FAIL: Was able to cast short[] to char[] or char[] to short[]");
			return 101;
	        }
		else
		{
	       	Console.WriteLine("PASS");
			return 100;
		}                       
	}
}
