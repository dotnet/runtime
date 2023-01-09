using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[SetupCSharpCompilerToUse ("csc")]
	[ExpectedNoWarnings]
	public class MembersUsedViaReflection
	{
		public static void Main ()
		{
			TestGetMembers ();
			TestWithBindingFlags ();
			TestWithUnknownBindingFlags (BindingFlags.Public);
			TestNullType ();
			TestNoValue ();
			TestDataFlowType ();
			TestDataFlowWithAnnotation (typeof (MyType));
			TestIfElse (true);
		}

		[Kept]
		static void TestGetMembers ()
		{
			var members = typeof (SimpleGetMembers).GetMembers ();
		}

		[Kept]
		static void TestWithBindingFlags ()
		{
			var members = typeof (MembersBindingFlags).GetMembers (BindingFlags.Public);
		}

		[Kept]
		static void TestWithUnknownBindingFlags (BindingFlags bindingFlags)
		{
			// Since the binding flags are not known linker should mark all members on the type
			var members = typeof (UnknownBindingFlags).GetMembers (bindingFlags);
		}

		[Kept]
		static void TestNullType ()
		{
			Type type = null;
			var members = type.GetMembers ();
		}

		[Kept]
		static void TestNoValue ()
		{
			Type t = null;
			Type noValue = Type.GetTypeFromHandle (t.TypeHandle);
			var members = noValue.GetMembers ();
		}

		[Kept]
		static Type FindType ()
		{
			return null;
		}

		[ExpectedWarning ("IL2075", "FindType", "GetMembers")]
		[Kept]
		static void TestDataFlowType ()
		{
			Type type = FindType ();
			var members = type.GetMembers (BindingFlags.Public);
		}

		[Kept]
		private static void TestDataFlowWithAnnotation ([KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors |
										 DynamicallyAccessedMemberTypes.PublicEvents |
										 DynamicallyAccessedMemberTypes.PublicFields |
										 DynamicallyAccessedMemberTypes.PublicMethods |
										 DynamicallyAccessedMemberTypes.PublicProperties |
										 DynamicallyAccessedMemberTypes.PublicNestedTypes)] Type type)
		{
			var properties = type.GetMembers (BindingFlags.Public | BindingFlags.Static);
		}

		[Kept]
		static void TestIfElse (bool decision)
		{
			Type myType;
			if (decision) {
				myType = typeof (IfType);
			} else {
				myType = typeof (ElseType);
			}
			var members = myType.GetMembers (BindingFlags.Public);
		}

		[Kept]
		private class SimpleGetMembers
		{
			[Kept]
			public SimpleGetMembers ()
			{ }

			private SimpleGetMembers (int i)
			{ }

			[Kept]
			public void PublicMethod ()
			{ }

			private void PrivateMethod ()
			{ }

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> PublicEvent;

			private event EventHandler<EventArgs> MarkedDueToPutRefDispPropertyEvent;

			[Kept]
			public static int _publicField;

			private int _privateField;

			[Kept]
			public int PublicProperty {
				[Kept]
				get { return _publicField; }
				[Kept]
				set { _publicField = value; }
			}

			private int PrivateProperty {
				get { return _privateField; }
				set { _privateField = value; }
			}

			[Kept]
			public static class PublicNestedType
			{
				[Kept]
				public static int _nestedPublicField;
				[Kept]
				public static void NestedPublicMethod ()
				{ }
			}

			private static class PrivateNestedType { }
		}

		[Kept]
		private class MembersBindingFlags
		{
			[Kept]
			public MembersBindingFlags ()
			{ }

			private MembersBindingFlags (int i)
			{ }

			[Kept]
			public void PublicMethod ()
			{ }

			private void PrivateMethod ()
			{ }

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> PublicEvent;

			private event EventHandler<EventArgs> MarkedDueToPutRefDispPropertyEvent;

			[Kept]
			public static int _publicField;

			private int _privateField;

			[Kept]
			public int PublicProperty {
				[Kept]
				get { return _publicField; }
				[Kept]
				set { _publicField = value; }
			}

			private int PrivateProperty {
				get { return _privateField; }
				set { _privateField = value; }
			}

			[Kept]
			public static class PublicNestedType
			{
				[Kept]
				public static int _nestedPublicField;
				[Kept]
				public static void NestedPublicMethod ()
				{ }
			}

			private static class PrivateNestedType { }
		}

		[Kept]
		private class UnknownBindingFlags
		{
			[Kept]
			public UnknownBindingFlags ()
			{ }

			[Kept]
			private UnknownBindingFlags (int i)
			{ }

			[Kept]
			public void PublicMethod ()
			{ }

			[Kept]
			private void PrivateMethod ()
			{ }

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

			[Kept]
			public static int _publicField;

			[Kept]
			private int _privateField;

			[Kept]
			public int PublicProperty {
				[Kept]
				get { return _publicField; }
				[Kept]
				set { _publicField = value; }
			}

			[Kept]
			private int PrivateProperty {
				[Kept]
				get { return _privateField; }
				[Kept]
				set { _privateField = value; }
			}

			[Kept]
			public static class PublicNestedType
			{
				[Kept]
				public static int _nestedPublicField;
				[Kept]
				public static void NestedPublicMethod ()
				{ }
			}

			[Kept]
			private static class PrivateNestedType { }
		}
		[Kept]
		private class MyType
		{
			[Kept]
			public MyType ()
			{ }

			private MyType (int i)
			{ }

			[Kept]
			public void PublicMethod ()
			{ }

			private void PrivateMethod ()
			{ }

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> PublicEvent;

			private event EventHandler<EventArgs> MarkedDueToPutRefDispPropertyEvent;

			[Kept]
			public static int _publicField;

			private int _privateField;

			[Kept]
			public int PublicProperty {
				[Kept]
				get { return _publicField; }
				[Kept]
				set { _publicField = value; }
			}

			private int PrivateProperty {
				get { return _privateField; }
				set { _privateField = value; }
			}

			[Kept]
			public static class PublicNestedType
			{
				[Kept]
				public static int _nestedPublicField;
				[Kept]
				public static void NestedPublicMethod ()
				{ }
			}

			private static class PrivateNestedType { }
		}

		[Kept]
		private class IfType
		{
			[Kept]
			public IfType ()
			{ }

			private IfType (int i)
			{ }

			[Kept]
			public void PublicMethod ()
			{ }

			private void PrivateMethod ()
			{ }

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> PublicEvent;

			private event EventHandler<EventArgs> MarkedDueToPutRefDispPropertyEvent;

			[Kept]
			public static int _publicField;

			private int _privateField;

			[Kept]
			public int PublicProperty {
				[Kept]
				get { return _publicField; }
				[Kept]
				set { _publicField = value; }
			}

			private int PrivateProperty {
				get { return _privateField; }
				set { _privateField = value; }
			}

			[Kept]
			public static class PublicNestedType
			{
				[Kept]
				public static int _nestedPublicField;
				[Kept]
				public static void NestedPublicMethod ()
				{ }
			}

			private static class PrivateNestedType { }
		}

		[Kept]
		private class ElseType
		{
			[Kept]
			public ElseType ()
			{ }

			private ElseType (int i)
			{ }

			[Kept]
			public void PublicMethod ()
			{ }

			private void PrivateMethod ()
			{ }

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> PublicEvent;

			private event EventHandler<EventArgs> MarkedDueToPutRefDispPropertyEvent;

			[Kept]
			public static int _publicField;

			private int _privateField;

			[Kept]
			public int PublicProperty {
				[Kept]
				get { return _publicField; }
				[Kept]
				set { _publicField = value; }
			}

			private int PrivateProperty {
				get { return _privateField; }
				set { _privateField = value; }
			}

			[Kept]
			public static class PublicNestedType
			{
				[Kept]
				public static int _nestedPublicField;
				[Kept]
				public static void NestedPublicMethod ()
				{ }
			}

			private static class PrivateNestedType { }
		}
	}
}