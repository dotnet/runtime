using System;
using System.Diagnostics.Tracing;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.BCLFeatures.ETW {
	[SetupLinkerArgument ("--exclude-feature", "etw")]
	public class NonEventWithLog {
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
	class NonEventWithLogSource : EventSource {

		[NonEvent]
		[Kept]
		internal void Test1 ()
		{
			if (IsEnabled ())
				Test2 ();
		}

		[Kept]
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
