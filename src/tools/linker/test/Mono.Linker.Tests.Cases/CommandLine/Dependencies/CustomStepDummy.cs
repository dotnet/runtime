using System;
using Mono.Linker;
using Mono.Linker.Steps;

namespace CustomStep
{
	public class CustomStepDummy : IStep
	{
		public void Process(LinkContext context)
		{
			context.LogMessage ("Custom step added.");
		}
	}
}
