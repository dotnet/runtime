using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection {
	public class EventUsedViaReflection {
		public static void Main ()
		{
			new Foo (); // Needed to avoid lazy body marking stubbing

			TestByName ();
			TestNullName ();
			TestEmptyName ();
			TestNonExistingName ();
			TestNullType ();
			TestDataFlowType ();
		}

		[Kept]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetEvent), new Type [] { typeof (string) },
			typeof (Foo), nameof (Foo.Event), (Type[]) null)]
		static void TestByName ()
		{
			var eventInfo = typeof (Foo).GetEvent ("Event");
			eventInfo.GetAddMethod (false);
		}

		[Kept]
		[UnrecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetEvent), new Type [] { typeof (string) })]
		static void TestNullName ()
		{
			var eventInfo = typeof (EventUsedViaReflection).GetEvent (null);
		}

		[Kept]
		[UnrecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetEvent), new Type [] { typeof (string) })]
		static void TestEmptyName ()
		{
			var eventInfo = typeof (EventUsedViaReflection).GetEvent (string.Empty);
		}

		[Kept]
		[UnrecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetEvent), new Type [] { typeof (string) })]
		static void TestNonExistingName ()
		{
			var eventInfo = typeof (EventUsedViaReflection).GetEvent ("NonExisting");
		}

		[Kept]
		[UnrecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetEvent), new Type [] { typeof (string) })]
		static void TestNullType ()
		{
			Type type = null;
			var eventInfo = type.GetEvent ("Event");
		}

		[Kept]
		static Type FindType ()
		{
			return typeof (Foo);
		}

		[Kept]
		[UnrecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetEvent), new Type [] { typeof (string) })]
		static void TestDataFlowType ()
		{
			Type type = FindType ();
			var eventInfo = type.GetEvent ("Event");
		}

		[KeptMember (".ctor()")]
		class Foo {
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			internal event EventHandler<EventArgs> Event;
		}
	}
}
