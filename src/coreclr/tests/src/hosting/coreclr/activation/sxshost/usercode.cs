// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Reflection;

#if _NONENGLISHCULTURE_
[assembly:AssemblyCultureAttribute("ja-jp")]
#endif

[assembly: AssemblyVersion("5.6.7.8")]
public class EventSink
{
	static public int Click(int x, int y) 
	{
		return (x+y);
	}

	public int Click2(int x, int y) 
	{
		return (x+y);
	}


	public static int Main()
	{
		return 100;
	}
}

