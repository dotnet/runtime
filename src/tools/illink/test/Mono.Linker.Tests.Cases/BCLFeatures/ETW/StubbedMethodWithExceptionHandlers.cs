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
	public class StubbedMethodWithExceptionHandlers
	{
		public static void Main ()
		{
			var b = StubbedMethodWithExceptionHandlers_RemovedEventSource.Log.IsEnabled ();
			if (b)
				StubbedMethodWithExceptionHandlers_RemovedEventSource.Log.SomeMethod ();
		}
	}

	[Kept]
	[KeptBaseType (typeof (EventSource))]
	[KeptMember (".ctor()")]
	[KeptMember (".cctor()")]
	[EventSource (Name = "MyCompany")]
	class StubbedMethodWithExceptionHandlers_RemovedEventSource : EventSource
	{
		public class Keywords
		{
			public const EventKeywords Page = (EventKeywords) 1;

			public int Unused;
		}

		[Kept]
		public static StubbedMethodWithExceptionHandlers_RemovedEventSource Log = new StubbedMethodWithExceptionHandlers_RemovedEventSource ();

		[Kept]
		[ExpectedInstructionSequence (new[]
		{
			"ret"
		})]
		protected override void OnEventCommand (EventCommandEventArgs command)
		{
			try {
				Removed ();
			} catch {
				try {
					Removed ();
				} catch {
					Removed ();
					throw;
				}
				throw;
			}
		}

		[Kept]
		[ExpectedInstructionSequence (new[]
		{
			"ret"
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