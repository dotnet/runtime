using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
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
		}

		[Kept]
		[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
		[RequiresUnreferencedCode ("-- DynamicallyAccessedMembersEnabled --")]
		[RecognizedReflectionAccessPattern]
		static void TestRequiresUnreferencedCodeAttributeWithDynamicallyAccessedMembersEnabled ()
		{
			RequiresPublicFields (typeof (TypeWithPublicFieldsAccessed));
		}

		[Kept]
		static void RequiresPublicFields (
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
			Type type)
		{
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
	}
}