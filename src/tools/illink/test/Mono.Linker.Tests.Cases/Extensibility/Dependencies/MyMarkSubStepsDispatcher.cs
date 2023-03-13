using System;
using System.Collections.Generic;
using Mono.Linker;
using Mono.Linker.Steps;

public class MyMarkSubStepsDispatcher : MarkSubStepsDispatcher
{
	public MyMarkSubStepsDispatcher ()
		: base (GetSubSteps ())
	{
	}

	public override void Initialize (LinkContext context, MarkContext markContext)
	{
		base.Initialize (context, markContext);
	}

	static IEnumerable<ISubStep> GetSubSteps ()
	{
		yield return new CustomSubStep ();
	}
}