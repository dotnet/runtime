using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection
{
#pragma warning disable 67 // The event {event} is not used
	[ExpectedNoWarnings]
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
		[ExpectedWarning ("IL2072", nameof (RuntimeReflectionExtensions) + "." + nameof (RuntimeReflectionExtensions.GetRuntimeEvent))]
		public static void TestGetRuntimeEvent ()
		{
			typeof (ClassWithKeptMembers).GetRuntimeEvent ("PublicEvent");
			typeof (ClassWithUnkeptMembers).GetRuntimeEvent ("PrivateEvent");
			typeof (ClassWithUnkeptMembers).GetRuntimeEvent ("ProtectedEvent");
			GetClassWithEvent ().GetRuntimeEvent ("This string will not be reached");
			typeof (Derived).GetRuntimeEvent ("Event");
			GetUnknownType ().GetRuntimeEvent (GetUnknownString ()); // IL2072

			Type t = null;
			t.GetRuntimeEvent ("This string will not be reached");
			Type noValue = Type.GetTypeFromHandle (t.TypeHandle);
			noValue.GetRuntimeEvent ("This string  will not be reached");

			typeof (ClassWithKeptMembers).GetRuntimeEvent (null);
			typeof (ClassWithKeptMembers).GetRuntimeEvent (string.Empty);
			string noValueString = t.AssemblyQualifiedName;
			typeof (ClassWithKeptMembers).GetRuntimeEvent (noValueString);
		}
		#endregion

		#region GetRuntimeField
		[Kept]
		[ExpectedWarning ("IL2072", nameof (RuntimeReflectionExtensions) + "." + nameof (RuntimeReflectionExtensions.GetRuntimeField))]
		public static void TestGetRuntimeField ()
		{
			typeof (ClassWithKeptMembers).GetRuntimeField ("PublicField");
			typeof (ClassWithUnkeptMembers).GetRuntimeField ("PrivateField");
			typeof (ClassWithUnkeptMembers).GetRuntimeField ("ProtectedField");
			GetClassWithField ().GetRuntimeField ("This string will not be reached");
			typeof (Derived).GetRuntimeField ("Field");
			GetUnknownType ().GetRuntimeField (GetUnknownString ()); // IL2072

			Type t = null;
			t.GetRuntimeField ("This string will not be reached");
			Type noValue = Type.GetTypeFromHandle (t.TypeHandle);
			noValue.GetRuntimeField ("This string  will not be reached");

			typeof (ClassWithKeptMembers).GetRuntimeField (null);
			typeof (ClassWithKeptMembers).GetRuntimeField (string.Empty);
			string noValueString = t.AssemblyQualifiedName;
			typeof (ClassWithKeptMembers).GetRuntimeField (noValueString);
		}
		#endregion

		#region GetRuntimeMethod
		[Kept]
		[ExpectedWarning ("IL2072", nameof (RuntimeReflectionExtensions) + "." + nameof (RuntimeReflectionExtensions.GetRuntimeMethod))]
		public static void TestGetRuntimeMethod ()
		{
			typeof (ClassWithKeptMembers).GetRuntimeMethod ("PublicMethod", Type.EmptyTypes);
			typeof (ClassWithUnkeptMembers).GetRuntimeMethod ("PrivateMethod", Type.EmptyTypes);
			typeof (ClassWithUnkeptMembers).GetRuntimeMethod ("ProtectedMethod", Type.EmptyTypes);
			GetClassWithMethod ().GetRuntimeMethod ("This string will not be reached", Type.EmptyTypes);
			typeof (Derived).GetRuntimeMethod ("Method", Type.EmptyTypes);
			GetUnknownType ().GetRuntimeMethod (GetUnknownString (), Type.EmptyTypes); // IL2072

			Type t = null;
			t.GetRuntimeMethod ("This string will not be reached", Type.EmptyTypes);
			Type noValue = Type.GetTypeFromHandle (t.TypeHandle);
			noValue.GetRuntimeMethod ("This string  will not be reached", Type.EmptyTypes);

			typeof (ClassWithKeptMembers).GetRuntimeMethod (null, Type.EmptyTypes);
			typeof (ClassWithKeptMembers).GetRuntimeMethod (string.Empty, Type.EmptyTypes);
			string noValueString = t.AssemblyQualifiedName;
			typeof (ClassWithKeptMembers).GetRuntimeMethod (noValueString, Type.EmptyTypes);
		}
		#endregion

		#region GetRuntimeProperty
		[Kept]
		[ExpectedWarning ("IL2072", nameof (RuntimeReflectionExtensions) + "." + nameof (RuntimeReflectionExtensions.GetRuntimeProperty))]
		public static void TestGetRuntimeProperty ()
		{
			typeof (ClassWithKeptMembers).GetRuntimeProperty ("PublicProperty");
			typeof (ClassWithUnkeptMembers).GetRuntimeProperty ("PrivateProperty");
			typeof (ClassWithUnkeptMembers).GetRuntimeProperty ("ProtectedProperty");
			GetClassWithProperty ().GetRuntimeProperty ("This string will not be reached");
			typeof (Derived).GetRuntimeProperty ("Property");
			GetUnknownType ().GetRuntimeProperty (GetUnknownString ()); // IL2072

			Type t = null;
			t.GetRuntimeProperty ("This string will not be reached");
			Type noValue = Type.GetTypeFromHandle (t.TypeHandle);
			noValue.GetRuntimeProperty ("This string  will not be reached");

			typeof (ClassWithKeptMembers).GetRuntimeProperty (null);
			typeof (ClassWithKeptMembers).GetRuntimeProperty (string.Empty);
			string noValueString = t.AssemblyQualifiedName;
			typeof (ClassWithKeptMembers).GetRuntimeProperty (noValueString);
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
