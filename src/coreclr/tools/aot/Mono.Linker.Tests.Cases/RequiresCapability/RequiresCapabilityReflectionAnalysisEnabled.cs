using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.RequiresCapability
{
	[ExpectedNoWarnings]
	public class RequiresCapabilityReflectionAnalysisEnabled
	{
		[LogContains ("-- DynamicallyAccessedMembersEnabled --")]
		[LogContains ("-- ReflectionPattern --")]
		[LogContains ("-- DynamicallyAccessedMembersOnGenericsEnabled --")]
		public static void Main ()
		{
			TestRequiresAttributeWithDynamicallyAccessedMembersEnabled ();
			TestRequiresAttributeWithReflectionPattern ();
			TestRequiresAttributeWithDynamicallyAccessedMembersOnGenericsEnabled ();
			TestRequiresAndDynamicallyAccessedMembers.Test ();
		}

		[Kept]
		[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
		[RequiresUnreferencedCode ("-- DynamicallyAccessedMembersEnabled --")]
		static void TestRequiresAttributeWithDynamicallyAccessedMembersEnabled ()
		{
			typeof (TypeWithPublicFieldsAccessed).RequiresPublicFields ();
		}

		[Kept]
		class TypeWithPublicFieldsAccessed
		{
			[Kept]
			public int _publicField;

			private int _privateField;
		}

		[Kept]
		[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
		[RequiresUnreferencedCode ("-- ReflectionPattern --")]
		static void TestRequiresAttributeWithReflectionPattern ()
		{
			typeof (TypeWithMethodAccessed).GetMethod ("PublicMethod");
		}

		[Kept]
		class TypeWithMethodAccessed
		{
			[Kept]
			public void PublicMethod () { }

			public void PublicMethod2 () { }
		}

		[Kept]
		[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
		[RequiresUnreferencedCode ("-- DynamicallyAccessedMembersOnGenericsEnabled --")]
		static void TestRequiresAttributeWithDynamicallyAccessedMembersOnGenericsEnabled ()
		{
			TypeRequiresPublicFields<TypeWithPublicFieldsForGenericType>.Method ();
			MethodRequiresPublicFields<TypeWithPublicFieldsForGenericMethod> ();
		}

		[Kept]
		class TypeRequiresPublicFields<
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
		T>
		{
			[Kept]
			public static void Method () { }
		}

		[Kept]
		class TypeWithPublicFieldsForGenericType
		{
			[Kept]
			public int _publicField;

			private int _privateField;
		}

		[Kept]
		static void MethodRequiresPublicFields<
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
		T> ()
		{
		}

		[Kept]
		class TypeWithPublicFieldsForGenericMethod
		{
			[Kept]
			public int _publicField;

			private int _privateField;
		}

		[Kept]
		class TestRequiresAndDynamicallyAccessedMembers
		{
			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[RequiresUnreferencedCode ("--- RequiresAndPublicMethods ---")]
			static void RequiresAndPublicMethods (
				[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
				[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
				Type type)
			{
				// This should not produce a warning since the method is annotated with Requires
				type.RequiresPublicFields ();

				// This will still "work" in that it will apply the PublicFields requirement onto the specified type
				typeof (TestRequiresAndDynamicallyAccessedMembers).RequiresPublicFields ();
			}

			[Kept]
			public void PublicInstanceMethod () { }

			[Kept]
			public static void PublicStaticMethod () { }

			static void PrivateInstanceMethod () { }

			[Kept]
			public static int PublicStaticField;

			static int PrivateStaticField;

			[Kept]
			[ExpectedWarning ("IL2026", "--- RequiresAndPublicMethods ---")]
			public static void Test ()
			{
				RequiresAndPublicMethods (typeof (TestRequiresAndDynamicallyAccessedMembers));
			}
		}
	}
}