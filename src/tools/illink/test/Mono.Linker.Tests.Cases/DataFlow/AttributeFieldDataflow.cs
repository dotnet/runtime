// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[Kept]
	[ExpectedNoWarnings]
	class AttributeFieldDataflow
	{
		public static void Main ()
		{
			TestKeepsPublicConstructors ();
			TestKeepsPublicMethods ();
			TestKeepsPublicMethodsString ();
			TestKeepsPublicFields ();
			TestTypeArray ();
		}

		[Kept]
		[KeptAttributeAttribute (typeof (KeepsPublicConstructorsAttribute))]
		[KeepsPublicConstructors (Type = typeof (ClassWithKeptPublicConstructor))]
		public static void TestKeepsPublicConstructors ()
		{
			typeof (AttributeFieldDataflow).GetMethod (nameof (TestKeepsPublicConstructors)).GetCustomAttribute (typeof (KeepsPublicConstructorsAttribute));
		}

		[Kept]
		[KeptAttributeAttribute (typeof (KeepsPublicMethodsAttribute))]
		[ExpectedWarning ("IL2026", "--ClassWithKeptPublicMethods--")]
		[KeepsPublicMethods (Type = typeof (ClassWithKeptPublicMethods))]
		public static void TestKeepsPublicMethods ()
		{
			typeof (AttributeFieldDataflow).GetMethod (nameof (TestKeepsPublicMethods)).GetCustomAttribute (typeof (KeepsPublicMethodsAttribute));
		}

		[Kept]
		[KeptAttributeAttribute (typeof (KeepsPublicMethodsAttribute))]
		// Trimmer/NativeAot only for now - https://github.com/dotnet/linker/issues/2273
		[ExpectedWarning ("IL2026", "--ClassWithKeptPublicMethods--", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		[KeepsPublicMethods (TypeName = "Mono.Linker.Tests.Cases.DataFlow.AttributeFieldDataflow+ClassWithKeptPublicMethods")]
		public static void TestKeepsPublicMethodsString ()
		{
			typeof (AttributeFieldDataflow).GetMethod (nameof (TestKeepsPublicMethodsString)).GetCustomAttribute (typeof (KeepsPublicMethodsAttribute));
		}

		[Kept]
		[KeptAttributeAttribute (typeof (KeepsPublicFieldsAttribute))]
		[KeepsPublicFields (Type = null, TypeName = null)]
		public static void TestKeepsPublicFields ()
		{
			typeof (AttributeFieldDataflow).GetMethod (nameof (TestKeepsPublicFields)).GetCustomAttribute (typeof (KeepsPublicFieldsAttribute));
		}

		[Kept]
		[KeptAttributeAttribute (typeof (TypeArrayAttribute))]
		[TypeArray (Types = new Type[] { typeof (AttributeFieldDataflow) })]
		public static void TestTypeArray ()
		{
			typeof (AttributeFieldDataflow).GetMethod (nameof (TestTypeArray)).GetCustomAttribute (typeof (TypeArrayAttribute));
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
			public string TypeName;

			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public Type Type;
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
