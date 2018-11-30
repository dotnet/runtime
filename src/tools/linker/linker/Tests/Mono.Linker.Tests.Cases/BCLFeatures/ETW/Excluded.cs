using System;
using System.Diagnostics.Tracing;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.BCLFeatures.ETW {
	[SetupLinkerArgument ("--exclude-feature", "etw")]
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
	class RemovedEventSource : EventSource {
		public class Keywords {
			public const EventKeywords Page = (EventKeywords)1;

			public int Unused;
		}

		[Kept]
		public static RemovedEventSource Log = new RemovedEventSource ();

		[Kept]
		[ExpectedInstructionSequence (new []
		{
			"ldstr",
			"newobj",
			"throw",
		})]
		protected override void OnEventCommand (EventCommandEventArgs command)
		{
		}

		[Kept]
		[ExpectedInstructionSequence (new []
		{
			"ldstr",
			"newobj",
			"throw",
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
