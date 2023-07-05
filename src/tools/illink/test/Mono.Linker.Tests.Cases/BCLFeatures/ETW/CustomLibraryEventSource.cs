using System.Diagnostics.Tracing;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.BCLFeatures.ETW
{
	[Reference ("System.Diagnostics.Tracing.dll")]
	[SetupLinkerArgument ("-a", "test.exe", "library")]
	[KeptMember (".ctor()")]
	public class CustomLibraryEventSource
	{
		public static void Main ()
		{
			// Reference to a derived EventSource but does not trigger Object.GetType()
			var b = CustomEventSourceInLibraryMode.Log.IsEnabled ();
		}
	}

	[Kept]
	[KeptBaseType (typeof (EventSource))]
	[KeptAttributeAttribute (typeof (EventSourceAttribute))]
	[KeptMember (".ctor()")]
	[KeptMember (".cctor()")]

	[EventSource (Name = "MyLibraryCompany")]
	class CustomEventSourceInLibraryMode : EventSource
	{
		// In library mode, we special case nested types
		[Kept]
		public class Keywords
		{
			[Kept]
			public const EventKeywords Page = (EventKeywords) 1;

			public int Unused;
		}

		[Kept]
		public class Tasks
		{
			[Kept]
			public const EventTask Page = (EventTask) 1;

			public int Unused;
		}

		class NotMatching
		{
		}

		[Kept]
		public static CustomEventSourceInLibraryMode Log = new CustomEventSourceInLibraryMode ();

		int private_member;

		void PrivateMethod () { }
	}
}
