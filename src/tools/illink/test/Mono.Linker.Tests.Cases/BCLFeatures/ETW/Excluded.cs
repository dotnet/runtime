using System;
using System.Diagnostics.Tracing;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.BCLFeatures.ETW
{
	[TestCaseRequirements (TestRunCharacteristics.TargetingNetFramework, "--exclude-feature is not supported on .NET Core")]
	[SetupLinkerArgument ("--exclude-feature", "etw")]
	// Keep framework code that calls EventSource methods like OnEventCommand
	[SetupLinkerTrimMode ("skip")]
	// Used to avoid different compilers generating different IL which can mess up the instruction asserts
	[SetupCompileArgument ("/optimize+")]
	public class Excluded
	{
		public static void Main ()
		{
			var b = RemovedEventSource.Log.IsEnabled ();
			if (b)
				RemovedEventSource.Log.SomeMethod ();
		}
	}

	[Kept]
	[KeptBaseType (typeof (EventSource))]
	[KeptMember (".ctor()")]
	[KeptMember (".cctor()")]
	[EventSource (Name = "MyCompany")]
	class RemovedEventSource : EventSource
	{
		public class Keywords
		{
			public const EventKeywords Page = (EventKeywords) 1;

			public int Unused;
		}

		[Kept]
		public static RemovedEventSource Log = new RemovedEventSource ();

		[Kept]
		[ExpectedInstructionSequence (new[]
		{
			"ret",
		})]
		protected override void OnEventCommand (EventCommandEventArgs command)
		{
			Removed2 ();
		}

		static void Removed2 ()
		{
		}

		[Kept]
		[ExpectedInstructionSequence (new[]
		{
			"ret",
		})]
		[Event (8)]
		public void SomeMethod ()
		{
			Removed ();
		}

		public void Removed ()
		{
		}
	}
}
