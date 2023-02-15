using System;
using System.Diagnostics.Tracing;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.BCLFeatures.ETW
{
	[TestCaseRequirements (TestRunCharacteristics.TargetingNetFramework, "--exclude-feature is not supported on .NET Core")]
	[SetupLinkerArgument ("--exclude-feature", "etw")]
	// Used to avoid different compilers generating different IL which can mess up the instruction asserts
	[SetupCompileArgument ("/optimize+")]
	public class NonEventWithLog
	{
		public static void Main ()
		{
			var n = new NonEventWithLogSource ();
			n.Test1 ();
		}
	}

	[Kept]
	[KeptBaseType (typeof (EventSource))]
	[KeptMember (".ctor()")]

	[EventSource (Name = "MyCompany")]
	class NonEventWithLogSource : EventSource
	{

		[NonEvent]
		[Kept]
		internal void Test1 ()
		{
			if (IsEnabled ())
				Test2 ();
		}

		[Kept]
		[ExpectedInstructionSequence (new[]
		{
			"ret"
		})]
		private void Test2 ()
		{
			Console.WriteLine ();
		}

		[NonEvent]
		private void Test3 ()
		{
			Console.WriteLine ();
		}
	}
}
