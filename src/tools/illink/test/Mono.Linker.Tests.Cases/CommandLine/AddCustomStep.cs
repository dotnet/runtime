using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.CommandLine
{
	[SetupCompileBefore ("CustomStepDummy.dll", new[] { "Dependencies/CustomStepDummy.cs" }, new[] { "illink.dll" })]
	[SetupLinkerArgument ("--custom-step", "CustomStep.CustomStepDummy,CustomStepDummy.dll")]
	[SetupLinkerArgument ("--custom-step", "-CleanStep:CustomStep.CustomStepDummy,CustomStepDummy.dll")]
	[SetupLinkerArgument ("--custom-step", "+CleanStep:CustomStep.CustomStepDummy,CustomStepDummy.dll")]
	[SetupLinkerArgument ("--verbose")]
	[LogContains ("Custom step added")]
	public class AddCustomStep
	{
		public static void Main ()
		{
		}
	}
}
