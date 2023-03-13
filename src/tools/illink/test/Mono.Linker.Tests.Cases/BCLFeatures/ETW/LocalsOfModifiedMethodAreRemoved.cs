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
	public class LocalsOfModifiedMethodAreRemoved
	{
		public static void Main ()
		{
			var b = LocalsAreCleared_RemovedEventSource.Log.IsEnabled ();
			if (b)
				LocalsAreCleared_RemovedEventSource.Log.SomeMethod ();
		}

		public class ClassForLocal
		{
		}

		public static void CallsToForceALocal (int arg1, int arg2, int arg3)
		{
		}
	}

	[Kept]
	[KeptBaseType (typeof (EventSource))]
	[KeptMember (".ctor()")]
	[KeptMember (".cctor()")]
	[EventSource (Name = "MyCompany")]
	class LocalsAreCleared_RemovedEventSource : EventSource
	{
		public class Keywords
		{
			public const EventKeywords Page = (EventKeywords) 1;

			public int Unused;
		}

		[Kept]
		public static LocalsAreCleared_RemovedEventSource Log = new LocalsAreCleared_RemovedEventSource ();

		[Kept]
		[ExpectBodyModified]
		[ExpectedLocalsSequence (new string[0])]
		protected override void OnEventCommand (EventCommandEventArgs command)
		{
			// Do some extra stuff to be extra certain the compiler introduced a local instead of using `dup`
			var tmp = new LocalsOfModifiedMethodAreRemoved.ClassForLocal ();
			LocalsOfModifiedMethodAreRemoved.CallsToForceALocal (1, 3, 4);
			LocalsOfModifiedMethodAreRemoved.CallsToForceALocal (1, 4, 4);
			var hashcode = tmp.GetHashCode ();
			LocalsOfModifiedMethodAreRemoved.CallsToForceALocal (1, hashcode, 3);
		}

		[Kept]
		[Event (8)]
		[ExpectBodyModified]
		public void SomeMethod ()
		{
			Removed ();
		}

		public void Removed ()
		{
		}
	}
}