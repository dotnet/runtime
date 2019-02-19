using System;
using System.Diagnostics.Tracing;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.BCLFeatures.ETW {
	public class CustomEventSource {
		public static void Main ()
		{
			var b = MyCompanyEventSource.Log.IsEnabled ();
		}
	}

	[Kept]
	[KeptBaseType (typeof (EventSource))]
	[KeptAttributeAttribute (typeof (EventSourceAttribute))]
	[KeptMember (".ctor()")]
	[KeptMember (".cctor()")]

	[EventSource (Name = "MyCompany")]
	class MyCompanyEventSource : EventSource
	{
		[Kept]
		public class Keywords 
		{
			[Kept]
			public const EventKeywords Page = (EventKeywords)1;

			public int Unused;
		}

		[Kept]
		public class Tasks 
		{
			[Kept]
			public const EventTask Page = (EventTask)1;

			public int Unused;
		}

		class NotMatching
		{
		}

		[Kept]
		public static MyCompanyEventSource Log = new MyCompanyEventSource ();
	}
}
