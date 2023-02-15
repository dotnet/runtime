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
			// Normally calls to GetMember use prefix lookup to match multiple values, we took a conservative approach
			// and preserve not based on the string passed but on the binding flags requirements
			TestWithName ();
			TestWithNullName ();
			TestWithEmptyName ();
			TestWithNoValueName ();
			TestWithPrefixLookup ();
			TestWithBindingFlags ();
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
			var members = typeof (SimpleType).GetMember ("memberKept");
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
		static void TestWithUnknownBindingFlags (BindingFlags bindingFlags)
		{
			// Since the binding flags are not known linker should mark all members on the type
			var members = typeof (UnknownBindingFlags).GetMember ("PrefixLookup*", bindingFlags);
		}

		[Kept]
		static void TestWithMemberTypes ()
		{
			// Here we took the same conservative approach, instead of understanding MemberTypes we only use
			// the information in the binding flags requirements and keep all the MemberTypes
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

		[Kept]
		private class SimpleType
		{
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
			public SimpleType ()
			{ }

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
		private class BindingFlagsType
		{
			[Kept]
			public BindingFlagsType ()
			{ }

			[Kept]
			private BindingFlagsType (int i)
			{ }

			[Kept]
			public static int PrefixLookup_field;

			[Kept]
			private static int PrefixLookup_privatefield;

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

			[Kept]
			public void PrefixLookupMethod () { }

			[Kept]
			private void PrefixLookupPrivateMethod () { }

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

			[Kept]
			public static class PrefixLookupNestedType { }

			[Kept]
			private static class PrefixLookupPrivateNestedType { }
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
			public static int PrefixLookup_field;

			[Kept]
			private static int PrefixLookup_privatefield;

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

			[Kept]
			public void PrefixLookupMethod () { }

			[Kept]
			private void PrefixLookupPrivateMethod () { }

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

			[Kept]
			public static class PrefixLookupNestedType { }

			[Kept]
			private static class PrefixLookupPrivateNestedType { }
		}

		[Kept]
		private class TestMemberTypes
		{
			[Kept]
			public TestMemberTypes ()
			{ }

			private TestMemberTypes (int i)
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
	}
}