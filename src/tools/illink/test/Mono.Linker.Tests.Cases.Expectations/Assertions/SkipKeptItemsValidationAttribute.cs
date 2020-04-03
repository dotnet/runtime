using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = false)]
	public class SkipKeptItemsValidationAttribute : BaseExpectedLinkedBehaviorAttribute
	{
		public SkipKeptItemsValidationAttribute () { }
	}
}
