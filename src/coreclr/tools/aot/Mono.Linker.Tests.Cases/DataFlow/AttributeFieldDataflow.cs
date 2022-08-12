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
	class AttributeFieldDataflow
	{
		[KeptAttributeAttribute (typeof (KeepsPublicConstructorsAttribute))]
		[KeptAttributeAttribute (typeof (KeepsPublicMethodsAttribute))]
		[KeptAttributeAttribute (typeof (KeepsPublicFieldsAttribute))]
		[KeptAttributeAttribute (typeof (TypeArrayAttribute))]
		[KeepsPublicConstructors (Type = typeof (ClassWithKeptPublicConstructor))]
		[KeepsPublicMethods (Type = "Mono.Linker.Tests.Cases.DataFlow.AttributeFieldDataflow+ClassWithKeptPublicMethods")]
		[KeepsPublicFields (Type = null, TypeName = null)]
		[TypeArray (Types = new Type[] { typeof (AttributeFieldDataflow) })]
		// Trimmer only for now - https://github.com/dotnet/linker/issues/2273
		[ExpectedWarning ("IL2026", "--ClassWithKeptPublicMethods--", ProducedBy = ProducedBy.Trimmer | ProducedBy.NativeAot)]
		public static void Main ()
		{
			typeof (AttributeFieldDataflow).GetMethod ("Main").GetCustomAttribute (typeof (KeepsPublicConstructorsAttribute));
			typeof (AttributeFieldDataflow).GetMethod ("Main").GetCustomAttribute (typeof (KeepsPublicMethodsAttribute));
		}

		[Kept]
		[KeptBaseType (typeof (Attribute))]
		class KeepsPublicConstructorsAttribute : Attribute
		{
			[Kept]
			public KeepsPublicConstructorsAttribute ()
			{
			}

			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
			public Type Type;
		}

		[Kept]
		[KeptBaseType (typeof (Attribute))]
		class KeepsPublicMethodsAttribute : Attribute
		{
			[Kept]
			public KeepsPublicMethodsAttribute ()
			{
			}

			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public string Type;
		}

		// Use to test null values
		[Kept]
		[KeptBaseType (typeof (Attribute))]
		class KeepsPublicFieldsAttribute : Attribute
		{
			[Kept]
			public KeepsPublicFieldsAttribute ()
			{
			}

			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
			public Type Type;

			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
			public string TypeName;
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

			[Kept]
			public Type[] Types;
		}
	}
}
