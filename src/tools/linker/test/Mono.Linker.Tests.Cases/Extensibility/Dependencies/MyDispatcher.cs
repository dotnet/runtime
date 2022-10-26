using System;
using System.Collections.Generic;
using Mono.Linker.Steps;

public class MyDispatcher : SubStepsDispatcher
{
	public MyDispatcher ()
		: base (GetSubSteps ())
	{
	}

	static IEnumerable<ISubStep> GetSubSteps ()
	{
		yield return new CustomSubStep ();
	}
}