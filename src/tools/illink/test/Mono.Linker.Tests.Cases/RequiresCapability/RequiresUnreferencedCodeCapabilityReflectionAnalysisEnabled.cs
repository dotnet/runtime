using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.RequiresCapability
{
	public class RequiresUnreferencedCodeCapabilityReflectionAnalysisEnabled
	{
		[LogContains ("-- DynamicallyAccessedMembersEnabled --")]
		[LogContains ("-- ReflectionPattern --")]
		[LogContains ("-- DynamicallyAccessedMembersOnGenericsEnabled --")]
		public static void Main ()
		{
			TestRequiresUnreferencedCodeAttributeWithDynamicallyAccessedMembersEnabled ();
			TestRequiresUnreferencedCodeAttributeWithReflectionPattern ();
			TestRequiresUnreferencedCodeAttributeWithDynamicallyAccessedMembersOnGenericsEnabled ();
			TestRequiresUnreferencedCodeAndDynamicallyAccessedMembers.Test ();
		}

		[Kept]
		[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
		[RequiresUnreferencedCode ("-- DynamicallyAccessedMembersEnabled --")]
		[RecognizedReflectionAccessPattern]
		static void TestRequiresUnreferencedCodeAttributeWithDynamicallyAccessedMembersEnabled ()
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
		[RecognizedReflectionAccessPattern]
		static void TestRequiresUnreferencedCodeAttributeWithReflectionPattern ()
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
		[RecognizedReflectionAccessPattern]
		static void TestRequiresUnreferencedCodeAttributeWithDynamicallyAccessedMembersOnGenericsEnabled ()
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
		[ExpectedNoWarnings]
		class TestRequiresUnreferencedCodeAndDynamicallyAccessedMembers
		{
			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[RequiresUnreferencedCode ("--- RequiresUnreferencedCodeAndPublicMethods ---")]
			[RecognizedReflectionAccessPattern]
			static void RequiresUnreferencedCodeAndPublicMethods (
				[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
				[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
				Type type)
			{
				// This should not produce a warning since the method is annotated with RequiresUnreferencedCode
				type.RequiresPublicFields ();

				// This will still "work" in that it will apply the PublicFields requirement onto the specified type
				typeof (TestRequiresUnreferencedCodeAndDynamicallyAccessedMembers).RequiresPublicFields ();
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
			[ExpectedWarning ("IL2026", "--- RequiresUnreferencedCodeAndPublicMethods ---")]
			public static void Test ()
			{
				RequiresUnreferencedCodeAndPublicMethods (typeof (TestRequiresUnreferencedCodeAndDynamicallyAccessedMembers));
			}
		}
	}
}