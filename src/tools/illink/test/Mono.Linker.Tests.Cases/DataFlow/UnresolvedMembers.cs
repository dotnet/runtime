// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[IgnoreTestCase ("Ignore in NativeAOT, see https://github.com/dotnet/runtime/issues/82447", IgnoredBy = Tool.NativeAot)]
	[KeptAttributeAttribute (typeof (IgnoreTestCaseAttribute), By = Tool.Trimmer)]
	// NativeAOT will not compile a method with unresolved types in it - it will instead replace it with a throwing method body
	// So it doesn't produce any of these warnings - which is also correct, because the code at runtime would never get there
	// it would fail to JIT/run anyway.

	[SkipILVerify]
	[SetupLinkerArgument ("--skip-unresolved", "true")]
	[SetupCompileBefore ("UnresolvedLibrary.dll", new[] { "Dependencies/UnresolvedLibrary.cs" }, removeFromLinkerInput: true)]
	[ExpectedNoWarnings]
	class UnresolvedMembers
	{
		public static void Main ()
		{
			UnresolvedGenericArgument ();
			UnresolvedAttributeArgument ();
			UnresolvedAttributePropertyValue ();
			UnresolvedAttributeFieldValue ();
			UnresolvedObjectGetType ();
			UnresolvedMethodParameter ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		class TypeWithUnresolvedGenericArgument<
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T>
		{
		}

		[Kept]
		static void MethodWithUnresolvedGenericArgument<
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
		{ }

		[Kept]
		[ExpectedWarning ("IL2066", "TypeWithUnresolvedGenericArgument", Tool.Trimmer | Tool.Analyzer, "")] // Local variable type
		[ExpectedWarning ("IL2066", "TypeWithUnresolvedGenericArgument", Tool.Trimmer | Tool.Analyzer, "")] // Called method declaring type
		[ExpectedWarning ("IL2066", nameof (MethodWithUnresolvedGenericArgument), Tool.Trimmer | Tool.Analyzer, "")]
		static void UnresolvedGenericArgument ()
		{
			var a = new TypeWithUnresolvedGenericArgument<Dependencies.UnresolvedType> ();
			MethodWithUnresolvedGenericArgument<Dependencies.UnresolvedType> ();
		}

		[Kept]
		[KeptBaseType (typeof (Attribute))]
		class AttributeWithRequirements : Attribute
		{
			[Kept]
			public AttributeWithRequirements (
				[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
				[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
			{ }

			[Kept]
			[KeptBackingField]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public Type PropertyWithRequirements { get; [Kept] set; }

			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public Type FieldWithRequirements;
		}

		[Kept]
		[ExpectedWarning ("IL2062", nameof (AttributeWithRequirements), Tool.Trimmer | Tool.Analyzer, "")]
		[KeptAttributeAttribute (typeof (AttributeWithRequirements))]
		[AttributeWithRequirements (typeof (Dependencies.UnresolvedType))]
		static void UnresolvedAttributeArgument ()
		{
		}

		[Kept]
		[ExpectedWarning ("IL2062", nameof (AttributeWithRequirements.PropertyWithRequirements), Tool.Trimmer | Tool.Analyzer, "")]
		[KeptAttributeAttribute (typeof (AttributeWithRequirements))]
		[AttributeWithRequirements (typeof (EmptyType), PropertyWithRequirements = typeof (Dependencies.UnresolvedType))]
		static void UnresolvedAttributePropertyValue ()
		{
		}

		[Kept]
		[ExpectedWarning ("IL2064", nameof (AttributeWithRequirements.FieldWithRequirements), Tool.Trimmer | Tool.Analyzer, "")]
		[KeptAttributeAttribute (typeof (AttributeWithRequirements))]
		[AttributeWithRequirements (typeof (EmptyType), FieldWithRequirements = typeof (Dependencies.UnresolvedType))]
		static void UnresolvedAttributeFieldValue ()
		{
		}

		[Kept]
		static Dependencies.UnresolvedType _unresolvedField;

		[Kept]
		[ExpectedWarning ("IL2072", nameof (Object.GetType), Tool.Trimmer | Tool.Analyzer, "")]
		static void UnresolvedObjectGetType ()
		{
			RequirePublicMethods (_unresolvedField.GetType ());
		}

		[Kept]
		[ExpectedWarning ("IL2072", nameof (Object.GetType), Tool.Trimmer | Tool.Analyzer, "")]
		static void UnresolvedMethodParameter ()
		{
			RequirePublicMethods (typeof (Dependencies.UnresolvedType));
		}

		[Kept]
		class EmptyType
		{
		}

		[Kept]
		static void RequirePublicMethods (
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			Type t)
		{
		}
	}
}
