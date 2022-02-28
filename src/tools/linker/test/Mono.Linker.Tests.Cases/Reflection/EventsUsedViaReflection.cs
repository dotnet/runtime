using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[SetupCSharpCompilerToUse ("csc")]
	[ExpectedNoWarnings]
	[SetupLinkerArgument ("--disable-opt", "unreachablebodies")]
	public class EventsUsedViaReflection
	{
		public static void Main ()
		{
			new Foo (); // Needed to avoid lazy body marking stubbing

			TestGetEvents ();
			TestInternal ();
			TestBindingFlags ();
			TestUnknownBindingFlags (BindingFlags.Public);
			TestNullType ();
			TestNoValue ();
			TestDataFlowType ();
			TestDataFlowWithAnnotation (typeof (MyType));
			TestIfElse (1);
			TestIgnoreCaseBindingFlags ();
			TestUnsupportedBindingFlags ();
		}

		[Kept]
		static void TestGetEvents ()
		{
			var events = typeof (Foo).GetEvents ();
		}

		[Kept]
		// The event will not be kept as it's internal and the behavior of Type.GetEvents() is to only return public events
		// But we don't mark it as unrecognized access pattern - we did recognize it fully, just didn't find the event being asked for
		// The behavior of the code will not change by linking it:
		//   - Without linking the GetEvents will return null
		//   - After linking the GetEvents will still return null
		static void TestInternal ()
		{
			var events = typeof (InternalEventType).GetEvents ();
		}

		[Kept]
		static void TestBindingFlags ()
		{
			var events = typeof (Bar).GetEvents (BindingFlags.NonPublic);
		}

		[Kept]
		static void TestUnknownBindingFlags (BindingFlags bindingFlags)
		{
			// Since the binding flags are not known linker should mark all events on the type
			var events = typeof (UnknownBindingFlags).GetEvents (bindingFlags);
		}

		[Kept]
		static void TestNullType ()
		{
			Type type = null;
			var events = type.GetEvents ();
		}

		[Kept]
		static void TestNoValue ()
		{
			Type t = null;
			Type noValue = Type.GetTypeFromHandle (t.TypeHandle);
			var methods = noValue.GetEvents ();
		}

		[Kept]
		static Type FindType ()
		{
			return typeof (Foo);
		}

		[Kept]
		[ExpectedWarning ("IL2075", "FindType", "GetEvents")]
		static void TestDataFlowType ()
		{
			Type type = FindType ();
			var events = type.GetEvents (BindingFlags.Public | BindingFlags.Static);
		}

		[Kept]
		static void TestDataFlowWithAnnotation ([KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))][DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicEvents)] Type type)
		{
			var events = type.GetEvents (BindingFlags.Public | BindingFlags.Static);
		}

		[Kept]
		static void TestIfElse (int i)
		{
			Type myType;
			if (i == 1) {
				myType = typeof (IfClass);
			} else {
				myType = typeof (ElseClass);
			}
			var events = myType.GetEvents (BindingFlags.Public);
		}

		[Kept]
		static void TestIgnoreCaseBindingFlags ()
		{
			var events = typeof (IgnoreCaseBindingFlagsClass).GetEvents (BindingFlags.IgnoreCase | BindingFlags.Public);
		}

		[Kept]
		static void TestUnsupportedBindingFlags ()
		{
			var events = typeof (PutRefDispPropertyBindingFlagsClass).GetEvents (BindingFlags.PutRefDispProperty);
		}

		[KeptMember (".ctor()")]
		class Foo
		{
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> Event;
		}

		[Kept]
		class InternalEventType
		{
			internal event EventHandler<EventArgs> Event;
		}

		class Bar
		{
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			internal event EventHandler<EventArgs> InternalEvent;
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			static event EventHandler<EventArgs> Static;
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			private event EventHandler<EventArgs> PrivateEvent;
			public event EventHandler<EventArgs> PublicEvent;
		}

		class UnknownBindingFlags
		{
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			internal event EventHandler<EventArgs> InternalEvent;
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			static event EventHandler<EventArgs> Static;
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			private event EventHandler<EventArgs> PrivateEvent;
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> PublicEvent;
		}

		[Kept]
		class MyType
		{
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> PublicEvent;
			private event EventHandler<EventArgs> PrivateEvent;
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
			public event EventHandler<EventArgs> ElseEvent;
		}

		class ElseClass
		{
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public static event EventHandler<EventArgs> ElseEvent;
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> IfEvent;
		}

		class IgnoreCaseBindingFlagsClass
		{
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> PublicEvent;

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			private event EventHandler<EventArgs> MarkedDueToIgnoreCaseEvent;
		}

		[Kept]
		class PutRefDispPropertyBindingFlagsClass
		{
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> PublicEvent;

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			private event EventHandler<EventArgs> MarkedDueToPutRefDispPropertyEvent;
		}
	}
}
