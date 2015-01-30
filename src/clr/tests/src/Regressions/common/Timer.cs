// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading;

public class TimerTest
{
	public static void Target(object p)
	{
		TestLibrary.TestFramework.LogInformation("Timer called");
	}

	public static int Main()
	{
		TestLibrary.TestFramework.BeginTestCase("Timer.Dispose()");

		TestLibrary.TestFramework.BeginScenario("Timer.Dispose() hang test");
		TestLibrary.TestFramework.LogInformation("Creating timercallback");
		TimerCallback tcb = new TimerCallback(Target);

		TestLibrary.TestFramework.LogInformation("Creating timer");
		Timer timer = new Timer(tcb,null,0,0);
	
		TestLibrary.TestFramework.LogInformation("Calling timer.Dispose");
		timer.Dispose();

		TestLibrary.TestFramework.EndTestCase();
		TestLibrary.TestFramework.LogInformation("PASS");

		return 100;
	}
}
