using System;
using Mono.Linker;
using Mono.Linker.Steps;

namespace CustomStep
{
	public class CustomStepUser : IStep
	{
		public void Process (LinkContext context)
		{
			if (context.TryGetCustomData ("NewKey", out var value))
				context.LogMessage (MessageContainer.CreateInfoMessage ("Custom step added with custom data of " + value));
		}
	}
}
