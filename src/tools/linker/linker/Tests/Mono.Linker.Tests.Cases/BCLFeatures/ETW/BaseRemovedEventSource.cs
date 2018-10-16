using System;
using System.Diagnostics.Tracing;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.BCLFeatures.ETW {
	[SetupLinkerArgument ("--exclude-feature", "etw")]
	public class BaseRemovedEventSource {
		public static void Main ()
		{
			var b = CustomCtorEventSource.Log.IsEnabled ();
			if (b)
				CustomCtorEventSource.Log.SomeMethod ();
		}
	}

	[Kept]
	[KeptBaseType (typeof (EventSource))]
	[KeptMember (".ctor()")]
	[KeptMember (".cctor()")]
	[EventSource (Name = "MyCompany")]
	class CustomCtorEventSource : EventSource {
		public class Keywords {
			public const EventKeywords Page = (EventKeywords)1;

			public int Unused;
		}

		[Kept]
		public static CustomCtorEventSource Log = new MyEventSourceBasedOnCustomCtorEventSource (1);

		[Kept]
		public CustomCtorEventSource (int value)
		{
			Removed ();
		}

		[Kept]
		protected override void OnEventCommand (EventCommandEventArgs command)
		{
		}

		[Kept]
		[Event (8)]
		public void SomeMethod ()
		{
			Removed ();
		}

		public void Removed ()
		{
		}
	}

	[Kept]
	[KeptBaseType (typeof (CustomCtorEventSource))]
	class MyEventSourceBasedOnCustomCtorEventSource : CustomCtorEventSource {
		[Kept]
		public MyEventSourceBasedOnCustomCtorEventSource (int value) : base (value)
		{
		}
	}
}
