using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.CommandLine
{
	[SetupCompileBefore ("CustomStepUser.dll", new[] { "Dependencies/CustomStepUser.cs" }, new[] { "illink.dll" })]
	[SetupLinkerArgument ("--custom-step", "CustomStep.CustomStepUser,CustomStepUser.dll")]
	[SetupLinkerArgument ("--custom-data", "NewKey=UserValue")]
	[SetupLinkerArgument ("--verbose")]
	[LogContains ("Custom step added with custom data of UserValue")]
	public class CustomStepData
	{
		public static void Main ()
		{
		}
	}
}
