using System;
using System.Diagnostics.Tracing;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.TestFramework
{
	/// <summary>
	/// This test is here to give some coverage to the attribute to ensure it doesn't break.  We need to leverage the ETW feature since it is the only
	/// one that modifies bodies currently
	/// </summary>
	[TestCaseRequirements (TestRunCharacteristics.TargetingNetFramework, "--exclude-feature is not supported on .NET Core")]
	[SetupLinkerArgument ("--exclude-feature", "etw")]
	// Keep framework code that calls EventSource methods like OnEventCommand
	[SetupLinkerTrimMode ("skip")]
	// Used to avoid different compilers generating different IL which can mess up the instruction asserts
	[SetupCompileArgument ("/optimize+")]
	public class VerifyExpectModifiedAttributesWork
	{
		public static void Main ()
		{
			var b = VerifyExpectModifiedAttributesWork_RemovedEventSource.Log.IsEnabled ();
			if (b)
				VerifyExpectModifiedAttributesWork_RemovedEventSource.Log.SomeMethod ();
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
	class VerifyExpectModifiedAttributesWork_RemovedEventSource : EventSource
	{
		public class Keywords
		{
			public const EventKeywords Page = (EventKeywords) 1;

			public int Unused;
		}

		[Kept]
		public static VerifyExpectModifiedAttributesWork_RemovedEventSource Log = new VerifyExpectModifiedAttributesWork_RemovedEventSource ();

		[Kept]
		[ExpectBodyModified]
		[ExpectExceptionHandlersModified]
		[ExpectLocalsModified]
		protected override void OnEventCommand (EventCommandEventArgs command)
		{
			try {
				// Do some extra stuff to be extra certain the compiler introduced a local instead of using `dup`
				var tmp = new VerifyExpectModifiedAttributesWork.ClassForLocal ();
				VerifyExpectModifiedAttributesWork.CallsToForceALocal (1, 3, 4);
				VerifyExpectModifiedAttributesWork.CallsToForceALocal (1, 4, 4);
				var hashcode = tmp.GetHashCode ();
				VerifyExpectModifiedAttributesWork.CallsToForceALocal (1, hashcode, 3);
			} catch {
				try {
					Removed ();
				} catch {
					var tmp = new VerifyExpectModifiedAttributesWork.ClassForLocal ();
					VerifyExpectModifiedAttributesWork.CallsToForceALocal (1, 3, 4);
					VerifyExpectModifiedAttributesWork.CallsToForceALocal (1, 4, 4);
					var hashcode = tmp.GetHashCode ();
					VerifyExpectModifiedAttributesWork.CallsToForceALocal (1, hashcode, 3);
					throw;
				}
				throw;
			}
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