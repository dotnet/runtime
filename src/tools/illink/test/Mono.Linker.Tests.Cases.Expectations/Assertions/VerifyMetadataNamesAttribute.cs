using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public class VerifyMetadataNamesAttribute : BaseExpectedLinkedBehaviorAttribute
	{
		public VerifyMetadataNamesAttribute ()
		{
		}
	}
}
