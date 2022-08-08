// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[Kept]
	[ExpectedNoWarnings]
	class AttributePropertyDataflow
	{
		[KeptAttributeAttribute (typeof (KeepsPublicConstructorsAttribute))]
		[KeptAttributeAttribute (typeof (KeepsPublicMethodsAttribute))]
		[KeptAttributeAttribute (typeof (KeepsPublicFieldsAttribute))]
		[KeptAttributeAttribute (typeof (TypeArrayAttribute))]
		[KeepsPublicConstructors (Type = typeof (ClassWithKeptPublicConstructor))]
		[KeepsPublicMethods (Type = "Mono.Linker.Tests.Cases.DataFlow.AttributePropertyDataflow+ClassWithKeptPublicMethods")]
		[KeepsPublicFields (Type = null, TypeName = null)]
		[TypeArray (Types = new Type[] { typeof (AttributePropertyDataflow) })]
		// Trimmer only for now - https://github.com/dotnet/linker/issues/2273
		[ExpectedWarning ("IL2026", "--ClassWithKeptPublicMethods--", ProducedBy = ProducedBy.Trimmer | ProducedBy.NativeAot)]
		public static void Main ()
		{
			typeof (AttributePropertyDataflow).GetMethod ("Main").GetCustomAttribute (typeof (KeepsPublicConstructorsAttribute));
			typeof (AttributePropertyDataflow).GetMethod ("Main").GetCustomAttribute (typeof (KeepsPublicMethodsAttribute));
		}

		[Kept]
		[KeptBaseType (typeof (Attribute))]
		class KeepsPublicConstructorsAttribute : Attribute
		{
			[Kept]
			public KeepsPublicConstructorsAttribute ()
			{
			}

			[field: Kept]
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
			public Type Type { get; [Kept] set; }
		}

		[Kept]
		[KeptBaseType (typeof (Attribute))]
		class KeepsPublicMethodsAttribute : Attribute
		{
			[Kept]
			public KeepsPublicMethodsAttribute ()
			{
			}

			[field: Kept]
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public string Type { get; [Kept] set; }
		}

		// Used to test null values
		[Kept]
		[KeptBaseType (typeof (Attribute))]
		class KeepsPublicFieldsAttribute : Attribute
		{
			[Kept]
			public KeepsPublicFieldsAttribute ()
			{
			}

			[field: Kept]
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
			public Type Type { get; [Kept] set; }

			[field: Kept]
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
			public string TypeName { get; [Kept] set; }
		}

		[Kept]
		class ClassWithKeptPublicConstructor
		{
			[Kept]
			public ClassWithKeptPublicConstructor (int unused) { }

			private ClassWithKeptPublicConstructor (short unused) { }

			public void Method () { }
		}

		[Kept]
		class ClassWithKeptPublicMethods
		{
			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[RequiresUnreferencedCode ("--ClassWithKeptPublicMethods--")]
			public static void KeptMethod () { }
			static void Method () { }
		}

		[Kept]
		[KeptBaseType (typeof (Attribute))]
		class TypeArrayAttribute : Attribute
		{
			[Kept]
			public TypeArrayAttribute ()
			{
			}

			[field: Kept]
			[Kept]
			public Type[] Types { get; [Kept] set; }
		}
	}
}
