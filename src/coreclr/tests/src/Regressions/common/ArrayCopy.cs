// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;

public class Repro
{
	public static Boolean ubyte_short()
	{
		const int size = 50;
		short[] shortArray = new short[size];
		byte[] ubyteArray = new byte[size];
		for(int i=0; i<size; i++)
			ubyteArray[i] = (byte)(size - i);
		
		Array.Copy(ubyteArray, shortArray, size);
		for (int i=0; i<size; i++)
		{
Console.WriteLine("{0}: {1} {2} {3}", i, ubyteArray[i], shortArray[i], (size-i));
			if (shortArray[i] != (size-i))
				throw new Exception("Copying from byte[] to short[] failed!  element "+i+" was incorrect!  got: "+shortArray[i]);
		}
		return true;
	}

	public static int Main()
	{
		if (ubyte_short())
		{
			Console.WriteLine("PASS");
			return 100;
		}
		else
		{
			Console.WriteLine("FAIL");
			return 9;
		}
	}
}

