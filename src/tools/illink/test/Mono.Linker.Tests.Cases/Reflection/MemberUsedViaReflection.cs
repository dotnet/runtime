using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[SetupCSharpCompilerToUse ("csc")]
	[ExpectedNoWarnings]
	public class MemberUsedViaReflection
	{
		public static void Main ()
		{
			TestWithName ();
			TestWithNullName ();
			TestWithEmptyName ();
			TestWithNoValueName ();
			TestWithPrefixLookup ();
			TestWithBindingFlags ();
			TestConstructorPrefix ();
			TestConstructorName ();
			TestMemberTypeNamed ();
			TestWithUnknownBindingFlags (BindingFlags.Public);
			TestWithMemberTypes ();
			TestNullType ();
			TestNoValue ();
			TestDataFlowType ();
			TestDataFlowWithAnnotation (typeof (MyType));
			TestIfElse (true);
		}

		[Kept]
		static void TestWithName ()
		{
			var members = typeof (SpecificName).GetMember ("memberKept");
		}

		[Kept]
		public static void TestWithNullName ()
		{
			var members = typeof (SimpleType).GetMember (null);
		}

		[Kept]
		static void TestWithEmptyName ()
		{
			var members = typeof (SimpleType).GetMember (string.Empty);
		}

		[Kept]
		static void TestWithNoValueName ()
		{
			Type t = null;
			string noValue = t.AssemblyQualifiedName;
			var members = typeof (SimpleType).GetMember (noValue);
		}

		[Kept]
		static void TestWithPrefixLookup ()
		{
			var members = typeof (PrefixLookupType).GetMember ("PrefixLookup*");
		}

		[Kept]
		static void TestWithBindingFlags ()
		{
			var members = typeof (BindingFlagsType).GetMember ("PrefixLookup*", BindingFlags.Public | BindingFlags.NonPublic);
		}

		[Kept]
		static void TestConstructorPrefix ()
		{
			var members = typeof (ConstructorPrefixClass).GetMember (".ct*");
		}

		[Kept]
		static void TestConstructorName ()
		{
			var members = typeof (ConstructorNameClass).GetMember (".ctor");
		}

		[Kept]
		static void TestMemberTypeNamed ()
		{
			var members1 = typeof (MemberTypeNamedClass1).GetMember ("FieldName", MemberTypes.Field, BindingFlags.Public | BindingFlags.Instance);
			var members2 = typeof (MemberTypeNamedClass2).GetMember ("FieldName", MemberTypes.Field, BindingFlags.Public | BindingFlags.Instance);
		}

		[Kept]
		static void TestWithUnknownBindingFlags (BindingFlags bindingFlags)
		{
			// The binding flags are not known trimming tools should mark all members on the type with that prefix
			var members = typeof (UnknownBindingFlags).GetMember ("PrefixLookup*", bindingFlags);
		}

		[Kept]
		static void TestWithMemberTypes ()
		{
			var members = typeof (TestMemberTypes).GetMember ("PrefixLookup*", MemberTypes.Method, BindingFlags.Public);
		}

		[Kept]
		static void TestNullType ()
		{
			Type type = null;
			var constructor = type.GetMember ("PrefixLookup*");
		}

		[Kept]
		static void TestNoValue ()
		{
			Type t = null;
			Type noValue = Type.GetTypeFromHandle (t.TypeHandle);
			var members = noValue.GetMember ("PrefixLookup*");
		}

		[Kept]
		static Type FindType ()
		{
			return null;
		}

		[ExpectedWarning ("IL2075", "FindType", "GetMember")]
		[Kept]
		static void TestDataFlowType ()
		{
			Type type = FindType ();
			var members = type.GetMember ("PrefixLookup*");
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
			var members = type.GetMember ("PrefixLookup*", BindingFlags.Public | BindingFlags.Static);
		}

		[Kept]
		static void TestIfElse (bool decision)
		{
			Type myType;
			if (decision) {
				myType = typeof (IfMember);
			} else {
				myType = typeof (ElseMember);
			}
			var members = myType.GetMember ("PrefixLookup*", BindingFlags.Public);
		}

		private class SpecificName
		{
			[Kept]
			public SpecificName()
			{ }

			[Kept]
			public static int field;

			public void otherMember() { }
		}

		[Kept]
		private class SimpleType
		{
			[Kept]
			public SimpleType ()
			{ }

			[Kept]
			public static int field;

			[Kept]
			public int memberKept {
				[Kept]
				get { return field; }
				[Kept]
				set { field = value; }
			}

			[Kept]
			public void someMethod () { }
		}

		[Kept]
		private class PrefixLookupType
		{
			[Kept]
			public PrefixLookupType ()
			{ }

			private PrefixLookupType (int i)
			{ }

			[Kept]
			public static int PrefixLookup_field;

			private static int PrefixLookup_privatefield;

			public static int IncorrectPrefix_field;

			[Kept]
			public int PrefixLookupProperty {
				[Kept]
				get { return PrefixLookup_field; }
				[Kept]
				set { PrefixLookup_field = value; }
			}

			private int PrefixLookupPrivateProperty {
				get { return PrefixLookup_privatefield; }
				set { PrefixLookup_privatefield = value; }
			}

			public int IncorrectPrefixProperty {
				get { return IncorrectPrefix_field; }
				set { IncorrectPrefix_field = value; }
			}

			[Kept]
			public void PrefixLookupMethod () { }

			private void PrefixLookupPrivateMethod () { }

			public void IncorrectPrefixMethod() { }

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> PrefixLookupEvent;

			private event EventHandler<EventArgs> PrefixLookupPrivateEvent;

			public event EventHandler<EventArgs> IncorrectPrefixEvent;

			[Kept]
			public static class PrefixLookupNestedType { }

			private static class PrefixLookupPrivateNestedType { }

			public static class IncorrectPrefixNestedType { }
		}

		[Kept]
		private class BindingFlagsType
		{
			[Kept]
			public BindingFlagsType ()
			{ }

			private BindingFlagsType (int i)
			{ }

			[Kept]
			public static int PrefixLookup_field;

			[Kept]
			private static int PrefixLookup_privatefield;

			public static int IncorrectPrefix_field;

			[Kept]
			public int PrefixLookupProperty {
				[Kept]
				get { return PrefixLookup_field; }
				[Kept]
				set { PrefixLookup_field = value; }
			}

			[Kept]
			private int PrefixLookupPrivateProperty {
				[Kept]
				get { return PrefixLookup_privatefield; }
				[Kept]
				set { PrefixLookup_privatefield = value; }
			}

			public int IncorrectPrefixProperty {
				get { return IncorrectPrefix_field; }
				set { IncorrectPrefix_field = value; }
			}

			[Kept]
			public void PrefixLookupMethod () { }

			[Kept]
			private void PrefixLookupPrivateMethod () { }

			public void IncorrectPrefixMethod() { }

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> PrefixLookupEvent;

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			private event EventHandler<EventArgs> PrefixLookupPrivateEvent;

			public event EventHandler<EventArgs> IncorrectPrefixEvent;

			[Kept]
			public static class PrefixLookupNestedType { }

			[Kept]
			private static class PrefixLookupPrivateNestedType { }

			public static class IncorrectPrefixNestedType { }
		}

		[Kept]
		private class ConstructorPrefixClass
		{
			[Kept]
			public ConstructorPrefixClass()
			{ }

			[Kept]
			public ConstructorPrefixClass(int a)
			{ }

			private ConstructorPrefixClass(string a)
			{ }
		}

		[Kept]
		private class ConstructorNameClass
		{
			[Kept]
			public ConstructorNameClass()
			{ }

			[Kept]
			public ConstructorNameClass(int a)
			{ }

			private ConstructorNameClass(string a)
			{ }
		}

		[Kept]
		private class MemberTypeNamedClass1
		{
			[Kept]
			public MemberTypeNamedClass1()
			{ }

			[Kept]
			public int FieldName;

			public int IncorrectFieldName;
		}

		private class MemberTypeNamedClass2
		{
			public MemberTypeNamedClass2()
			{ }

			public void FieldName() { }
		}

		[Kept]
		private class UnknownBindingFlags
		{
			[Kept]
			public UnknownBindingFlags ()
			{ }

			private UnknownBindingFlags (int i)
			{ }

			[Kept]
			public static int PrefixLookup_field;

			[Kept]
			private static int PrefixLookup_privatefield;

			public static int IncorrectPrefix_field;

			[Kept]
			public int PrefixLookupProperty {
				[Kept]
				get { return PrefixLookup_field; }
				[Kept]
				set { PrefixLookup_field = value; }
			}

			[Kept]
			private int PrefixLookupPrivateProperty {
				[Kept]
				get { return PrefixLookup_privatefield; }
				[Kept]
				set { PrefixLookup_privatefield = value; }
			}

			public int IncorrectPrefixProperty {
				get { return IncorrectPrefix_field; }
				set { IncorrectPrefix_field = value; }
			}

			[Kept]
			public void PrefixLookupMethod () { }

			[Kept]
			private void PrefixLookupPrivateMethod () { }

			public void IncorrectPrefixMethod() { }

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> PrefixLookupEvent;

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			private event EventHandler<EventArgs> PrefixLookupPrivateEvent;

			public event EventHandler<EventArgs> IncorrectPrefixEvent;

			[Kept]
			public static class PrefixLookupNestedType { }

			[Kept]
			private static class PrefixLookupPrivateNestedType { }

			public static class IncorrectPrefixNestedType { }
		}

		[Kept]
		private class TestMemberTypes
		{
			[Kept]
			public TestMemberTypes ()
			{ }

			private TestMemberTypes (int i)
			{ }

			public static int PrefixLookup_field;

			private static int PrefixLookup_privatefield;

			public int PrefixLookupProperty {
				get { return PrefixLookup_field; }
				set { PrefixLookup_field = value; }
			}

			private int PrefixLookupPrivateProperty {
				get { return PrefixLookup_privatefield; }
				set { PrefixLookup_privatefield = value; }
			}

			[Kept]
			public void PrefixLookupMethod () { }

			private void PrefixLookupPrivateMethod () { }

			public event EventHandler<EventArgs> PrefixLookupEvent;

			private event EventHandler<EventArgs> PrefixLookupPrivateEvent;

			public static class PrefixLookupNestedType { }

			private static class PrefixLookupPrivateNestedType { }
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
			public static int PrefixLookup_field;

			private static int PrefixLookup_privatefield;

			[Kept]
			public int PrefixLookupProperty {
				[Kept]
				get { return PrefixLookup_field; }
				[Kept]
				set { PrefixLookup_field = value; }
			}

			private int PrefixLookupPrivateProperty {
				get { return PrefixLookup_privatefield; }
				set { PrefixLookup_privatefield = value; }
			}

			[Kept]
			public void PrefixLookupMethod () { }

			private void PrefixLookupPrivateMethod () { }

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> PrefixLookupEvent;

			private event EventHandler<EventArgs> PrefixLookupPrivateEvent;

			[Kept]
			public static class PrefixLookupNestedType { }

			private static class PrefixLookupPrivateNestedType { }
		}

		[Kept]
		private class IfMember
		{
			[Kept]
			public IfMember ()
			{ }

			private IfMember (int i)
			{ }

			[Kept]
			public static int PrefixLookup_field;

			private static int PrefixLookup_privatefield;

			public static int IncorrectPrefix_field;

			[Kept]
			public int PrefixLookupProperty {
				[Kept]
				get { return PrefixLookup_field; }
				[Kept]
				set { PrefixLookup_field = value; }
			}

			private int PrefixLookupPrivateProperty {
				get { return PrefixLookup_privatefield; }
				set { PrefixLookup_privatefield = value; }
			}

			public int IncorrectPrefixProperty {
				get { return IncorrectPrefix_field; }
				set { IncorrectPrefix_field = value; }
			}

			[Kept]
			public void PrefixLookupMethod () { }

			private void PrefixLookupPrivateMethod () { }

			public void IncorrectPrefixMethod () { }

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> PrefixLookupEvent;

			private event EventHandler<EventArgs> PrefixLookupPrivateEvent;

			public event EventHandler<EventArgs> IncorrectPrefixEvent;

			[Kept]
			public static class PrefixLookupNestedType { }

			private static class PrefixLookupPrivateNestedType { }

			public static class IncorrectPrefixNestedType { }
		}

		[Kept]
		private class ElseMember
		{
			[Kept]
			public ElseMember ()
			{ }

			private ElseMember (int i)
			{ }

			[Kept]
			public static int PrefixLookup_field;

			private static int PrefixLookup_privatefield;

			public static int IncorrectPrefix_field;

			[Kept]
			public int PrefixLookupProperty {
				[Kept]
				get { return PrefixLookup_field; }
				[Kept]
				set { PrefixLookup_field = value; }
			}

			private int PrefixLookupPrivateProperty {
				get { return PrefixLookup_privatefield; }
				set { PrefixLookup_privatefield = value; }
			}

			public int IncorrectPrefixProperty {
				get { return IncorrectPrefix_field; }
				set { IncorrectPrefix_field = value; }
			}

			[Kept]
			public void PrefixLookupMethod () { }

			private void PrefixLookupPrivateMethod () { }

			public void IncorrectPrefixMethod () { }

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> PrefixLookupEvent;

			private event EventHandler<EventArgs> PrefixLookupPrivateEvent;

			public event EventHandler<EventArgs> IncorrectPrefixEvent;

			[Kept]
			public static class PrefixLookupNestedType { }

			private static class PrefixLookupPrivateNestedType { }

			public static class IncorrectPrefixNestedType { }
		}
	}
}