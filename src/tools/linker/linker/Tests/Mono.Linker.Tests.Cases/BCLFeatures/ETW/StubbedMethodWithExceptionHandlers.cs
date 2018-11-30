using System;
using System.Diagnostics.Tracing;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.BCLFeatures.ETW {
	[SetupLinkerArgument ("--exclude-feature", "etw")]
	// Used to avoid different compilers generating different IL which can mess up the instruction asserts
	[SetupCompileArgument ("/optimize+")]
	public class StubbedMethodWithExceptionHandlers {
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
	class StubbedMethodWithExceptionHandlers_RemovedEventSource : EventSource {
		public class Keywords {
			public const EventKeywords Page = (EventKeywords)1;

			public int Unused;
		}

		[Kept]
		public static StubbedMethodWithExceptionHandlers_RemovedEventSource Log = new StubbedMethodWithExceptionHandlers_RemovedEventSource ();

		[Kept]
		[ExpectedInstructionSequence (new []
		{
			"ldstr",
			"newobj",
			"throw",
		})]
		[ExpectedExceptionHandlerSequence (new string[0])]
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