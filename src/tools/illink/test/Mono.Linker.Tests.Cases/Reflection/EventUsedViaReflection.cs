using System;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection {
	[SetupCSharpCompilerToUse ("csc")]
	public class EventUsedViaReflection {
		public static void Main ()
		{
			new Foo (); // Needed to avoid lazy body marking stubbing

			TestByName ();
			TestNameBindingFlags ();
			TestNameWrongBindingFlags ();
			TestNullName ();
			TestEmptyName ();
			TestNonExistingName ();
			TestNullType ();
			TestDataFlowType ();
			TestIfElse (1);
			TestEventInBaseType ();
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
		static void TestNameBindingFlags ()
		{
			var eventInfo = typeof (Bar).GetEvent ("PrivateEvent", BindingFlags.NonPublic);
		}

		[Kept]
		static void TestNameWrongBindingFlags()
		{
			var eventInfo = typeof (Bar).GetEvent ("PublicEvent", BindingFlags.NonPublic);
		}

		[Kept]
		static void TestNullName ()
		{
			var eventInfo = typeof (EventUsedViaReflection).GetEvent (null);
		}

		[Kept]
		static void TestEmptyName ()
		{
			var eventInfo = typeof (EventUsedViaReflection).GetEvent (string.Empty);
		}

		[Kept]
		static void TestNonExistingName ()
		{
			var eventInfo = typeof (EventUsedViaReflection).GetEvent ("NonExisting");
		}

		[Kept]
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

		[Kept]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetEvent), new Type [] { typeof (string) },
			typeof (IfClass), nameof (IfClass.IfEvent), (Type [])null)]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetEvent), new Type [] { typeof (string) },
			typeof (IfClass), nameof (IfClass.ElseEvent), (Type [])null)]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetEvent), new Type [] { typeof (string) },
			typeof (ElseClass), nameof (ElseClass.IfEvent), (Type [])null)]
		static void TestIfElse (int i)
		{
			Type myType;
			if (i == 1) {
				myType = typeof (IfClass);
			} else {
				myType = typeof (ElseClass);
			}
			String myString;
			if(i == 1) {
				myString = "IfEvent";
			} else {
				myString = "ElseEvent";
			}
			var eventInfo = myType.GetEvent (myString);
		}

		[Kept]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetEvent), new Type [] { typeof (string) },
			typeof (BaseClass), nameof (BaseClass.PublicEventOnBase), (Type [])null)]
		static void TestEventInBaseType ()
		{
			typeof (DerivedClass).GetEvent ("ProtectedEventOnBase");
			typeof (DerivedClass).GetEvent ("PublicEventOnBase");
		}

		[KeptMember (".ctor()")]
		class Foo {
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			internal event EventHandler<EventArgs> Event;
		}

		class Bar
		{
			internal event EventHandler<EventArgs> InternalEvent;
			static event EventHandler<EventArgs> Static;
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			private event EventHandler<EventArgs> PrivateEvent;
			public event EventHandler<EventArgs> PublicEvent;
		}
		
		class IfClass
		{
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> IfEvent;
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			internal event EventHandler<EventArgs> ElseEvent;
		}

		class ElseClass
		{
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			private static event EventHandler<EventArgs> ElseEvent;
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> IfEvent;
		}

		[Kept]
		class BaseClass
		{
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			protected static event EventHandler<EventArgs> ProtectedEventOnBase;
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> PublicEventOnBase;
		}
		[KeptBaseType (typeof (BaseClass))]
		class DerivedClass : BaseClass
		{
		}
	}
}
