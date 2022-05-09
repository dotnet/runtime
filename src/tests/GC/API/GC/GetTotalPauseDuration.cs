// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Tests GC.GetTotalPauseDuration()

using System;
using System.Diagnostics;

public class Test_Collect
{
	public static int Main()
	{
		Stopwatch sw = Stopwatch.StartNew();
		GC.Collect();
		sw.Stop();
		TimeSpan elapsed = sw.Elapsed;
		TimeSpan totalPauseDuration = GC.GetTotalPauseDuration();
		if (TimeSpan.Zero < totalPauseDuration && totalPauseDuration <= elapsed)
		{
			return 100;
		}
		else
		{
			return 101;
		}		
	}
}
