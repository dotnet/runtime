using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection
{
#pragma warning disable 67 // The event {event} is not used
	public class RuntimeReflectionExtensionsCalls
	{
		public static void Main ()
		{
			TestGetRuntimeEvent ();
			TestGetRuntimeField ();
			TestGetRuntimeMethod ();
			TestGetRuntimeProperty ();

		}

		#region GetRuntimeEvent
		[Kept]
		[UnrecognizedReflectionAccessPattern (typeof (RuntimeReflectionExtensions), nameof (RuntimeReflectionExtensions.GetRuntimeEvent),
			new Type[] { typeof (Type), typeof (string) }, messageCode: "IL2072")]
		public static void TestGetRuntimeEvent ()
		{
			typeof (ClassWithKeptMembers).GetRuntimeEvent ("PublicEvent");
			typeof (ClassWithUnkeptMembers).GetRuntimeEvent ("PrivateEvent");
			typeof (ClassWithUnkeptMembers).GetRuntimeEvent ("ProtectedEvent");
			GetClassWithEvent ().GetRuntimeEvent ("This string will not be reached");
			typeof (Derived).GetRuntimeEvent ("Event");
			GetUnknownType ().GetRuntimeEvent (GetUnknownString ()); // UnrecognizedReflectionAccessPattern
		}
		#endregion

		#region GetRuntimeField
		[Kept]
		[UnrecognizedReflectionAccessPattern (typeof (RuntimeReflectionExtensions), nameof (RuntimeReflectionExtensions.GetRuntimeField),
			new Type[] { typeof (Type), typeof (string) }, messageCode: "IL2072")]
		public static void TestGetRuntimeField ()
		{
			typeof (ClassWithKeptMembers).GetRuntimeField ("PublicField");
			typeof (ClassWithUnkeptMembers).GetRuntimeField ("PrivateField");
			typeof (ClassWithUnkeptMembers).GetRuntimeField ("ProtectedField");
			GetClassWithField ().GetRuntimeField ("This string will not be reached");
			typeof (Derived).GetRuntimeField ("Field");
			GetUnknownType ().GetRuntimeField (GetUnknownString ()); // UnrecognizedReflectionAccessPattern
		}
		#endregion

		#region GetRuntimeMethod
		[Kept]
		[UnrecognizedReflectionAccessPattern (typeof (RuntimeReflectionExtensions), nameof (RuntimeReflectionExtensions.GetRuntimeMethod),
			new Type[] { typeof (Type), typeof (string), typeof (Type[]) }, messageCode: "IL2072")]
		public static void TestGetRuntimeMethod ()
		{
			typeof (ClassWithKeptMembers).GetRuntimeMethod ("PublicMethod", Type.EmptyTypes);
			typeof (ClassWithUnkeptMembers).GetRuntimeMethod ("PrivateMethod", Type.EmptyTypes);
			typeof (ClassWithUnkeptMembers).GetRuntimeMethod ("ProtectedMethod", Type.EmptyTypes);
			GetClassWithMethod ().GetRuntimeMethod ("This string will not be reached", Type.EmptyTypes);
			typeof (Derived).GetRuntimeMethod ("Method", Type.EmptyTypes);
			GetUnknownType ().GetRuntimeMethod (GetUnknownString (), Type.EmptyTypes); // UnrecognizedReflectionAccessPattern
		}
		#endregion

		#region GetRuntimeProperty
		[Kept]
		[UnrecognizedReflectionAccessPattern (typeof (RuntimeReflectionExtensions), nameof (RuntimeReflectionExtensions.GetRuntimeProperty),
			new Type[] { typeof (Type), typeof (string) }, messageCode: "IL2072")]
		public static void TestGetRuntimeProperty ()
		{
			typeof (ClassWithKeptMembers).GetRuntimeProperty ("PublicProperty");
			typeof (ClassWithUnkeptMembers).GetRuntimeProperty ("PrivateProperty");
			typeof (ClassWithUnkeptMembers).GetRuntimeProperty ("ProtectedProperty");
			GetClassWithProperty ().GetRuntimeProperty ("This string will not be reached");
			typeof (Derived).GetRuntimeProperty ("Property");
			GetUnknownType ().GetRuntimeProperty (GetUnknownString ()); // UnrecognizedReflectionAccessPattern
		}
		#endregion

		#region Helpers
		class ClassWithKeptMembers
		{
			[Kept]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			[method: ExpectBodyModified]
			public event EventHandler<EventArgs> PublicEvent;

			[Kept]
			public int PublicField;

			[Kept]
			public void PublicMethod (int arg)
			{
			}

			[Kept]
			public long PublicProperty { [Kept][ExpectBodyModified] get; [Kept][ExpectBodyModified] set; }
		}

		[Kept]
		class ClassWithUnkeptMembers
		{
			private event EventHandler<EventArgs> PrivateEvent;

			private int PrivateField;

			private void PrivateMethod (int arg)
			{
			}

			private long PrivateProperty { get; set; }

			protected event EventHandler<EventArgs> ProtectedEvent;

			protected int ProtectedField;

			protected void ProtectedMethod (int arg)
			{
			}

			protected long ProtectedProperty { get; set; }
		}

		class ClassWithEvent
		{
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public static event EventHandler<EventArgs> Event;
		}

		class ClassWithField
		{
			[Kept]
			public static int Field;
		}

		class ClassWithMethod
		{
			[Kept]
			public static void Method (int arg)
			{
			}
		}

		class ClassWithProperty
		{
			[Kept]
			[KeptBackingField]
			public static long Property { [Kept] get; [Kept] set; }
		}

		[Kept]
		class Base
		{
			[Kept]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			[method: ExpectBodyModified]
			public event EventHandler<EventArgs> Event;

			[Kept]
			public int Field;

			[Kept]
			public void Method (int arg)
			{
			}

			[Kept]
			public long Property { [Kept][ExpectBodyModified] get; [Kept][ExpectBodyModified] set; }
		}

		[Kept]
		[KeptBaseType (typeof (Base))]
		class Derived : Base
		{
		}

		[Kept]
		private static Type GetUnknownType ()
		{
			return null;
		}

		[Kept]
		private static string GetUnknownString ()
		{
			return null;
		}

		[Kept]
		[return: KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicEvents)]
		private static Type GetClassWithEvent ()
		{
			return typeof (ClassWithEvent);
		}

		[Kept]
		[return: KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
		private static Type GetClassWithField ()
		{
			return typeof (ClassWithField);
		}

		[Kept]
		[return: KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		private static Type GetClassWithMethod ()
		{
			return typeof (ClassWithMethod);
		}

		[Kept]
		[return: KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)]
		private static Type GetClassWithProperty ()
		{
			return typeof (ClassWithProperty);
		}
		#endregion
	}
}
