// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipPeVerify]
	[SetupLinkerArgument ("--skip-unresolved", "true")]
	[SetupCompileBefore ("UnresolvedLibrary.dll", new[] { "Dependencies/UnresolvedLibrary.cs" }, removeFromLinkerInput: true)]
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
		[ExpectedWarning ("IL2066", "TypeWithUnresolvedGenericArgument")]
		[ExpectedWarning ("IL2066", nameof (MethodWithUnresolvedGenericArgument))]
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
		[ExpectedWarning ("IL2062", nameof (AttributeWithRequirements))]
		[KeptAttributeAttribute (typeof (AttributeWithRequirements))]
		[AttributeWithRequirements (typeof (Dependencies.UnresolvedType))]
		static void UnresolvedAttributeArgument ()
		{
		}

		[Kept]
		[ExpectedWarning ("IL2062", nameof (AttributeWithRequirements.PropertyWithRequirements))]
		[KeptAttributeAttribute (typeof (AttributeWithRequirements))]
		[AttributeWithRequirements (typeof (EmptyType), PropertyWithRequirements = typeof (Dependencies.UnresolvedType))]
		static void UnresolvedAttributePropertyValue ()
		{
		}

		[Kept]
		[ExpectedWarning ("IL2064", nameof (AttributeWithRequirements.FieldWithRequirements))]
		[KeptAttributeAttribute (typeof (AttributeWithRequirements))]
		[AttributeWithRequirements (typeof (EmptyType), FieldWithRequirements = typeof (Dependencies.UnresolvedType))]
		static void UnresolvedAttributeFieldValue ()
		{
		}

		[Kept]
		static Dependencies.UnresolvedType _unresolvedField;

		[Kept]
		[ExpectedWarning ("IL2072", nameof (Object.GetType))]
		static void UnresolvedObjectGetType ()
		{
			RequirePublicMethods (_unresolvedField.GetType ());
		}

		[Kept]
		[ExpectedWarning ("IL2072", nameof (Object.GetType))]
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
