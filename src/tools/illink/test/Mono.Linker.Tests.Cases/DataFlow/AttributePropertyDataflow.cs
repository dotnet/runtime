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
	class AttributePropertyDataflow
	{
		public static void Main ()
		{
			AttributesOnMethod.Test ();
			AttributesOnProperty.Test ();
			AttributesOnField.Test ();
			AttributesOnEvent.Test ();
			typeof (AttributePropertyDataflow).GetMethod ("Main").GetCustomAttribute (typeof (KeepsPublicConstructorsAttribute));
			typeof (AttributePropertyDataflow).GetMethod ("Main").GetCustomAttribute (typeof (KeepsPublicMethodsAttribute));
		}

		class AttributesOnMethod
		{
			[Kept]
			[KeptAttributeAttribute (typeof (KeepsPublicConstructorsAttribute))]
			[KeptAttributeAttribute (typeof (KeepsPublicMethodsAttribute))]
			[KeptAttributeAttribute (typeof (KeepsPublicFieldsAttribute))]
			[KeptAttributeAttribute (typeof (TypeArrayAttribute))]
			[KeepsPublicConstructors (Type = typeof (ClassWithKeptPublicConstructor))]
			[KeepsPublicMethods (TypeName = "Mono.Linker.Tests.Cases.DataFlow.AttributePropertyDataflow+AttributesOnMethod+ClassWithKeptPublicMethods")]
			[KeepsPublicFields (Type = null, TypeName = null)]
			[TypeArray (Types = new Type[] { typeof (AttributePropertyDataflow) })]
			// Trimmer only for now - https://github.com/dotnet/linker/issues/2273
			[ExpectedWarning ("IL2026", "--ClassWithKeptPublicMethods--", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			public static void Test () {
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
		}

		class AttributesOnField
		{
			[Kept]
			[KeptAttributeAttribute (typeof (KeepsPublicMethodsAttribute))]
			[ExpectedWarning ("IL2026", "--ClassWithKeptPublicMethods--")]
			[KeepsPublicMethods (Type = typeof (ClassWithKeptPublicMethods))]
			static bool field;

			[Kept]
			public static void Test ()
			{
				field = true;
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
		}

		class AttributesOnProperty
		{
			[field: Kept]
			[Kept]
			[KeptAttributeAttribute (typeof (KeepsPublicMethodsAttribute))]
			[ExpectedWarning ("IL2026", "--ClassWithKeptPublicMethods--")]
			[KeepsPublicMethods (Type = typeof (ClassWithKeptPublicMethods))]
			static bool Property { get; [Kept] set; }

			[Kept]
			public static void Test ()
			{
				Property = true;
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
		}

		class AttributesOnEvent
		{
			[field: Kept]
			[Kept]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			[KeptAttributeAttribute (typeof (KeepsPublicMethodsAttribute))]
			[ExpectedWarning ("IL2026", "--ClassWithKeptPublicMethods--")]
			[ExpectedWarning ("IL2026", "--ClassWithKeptPublicMethods--",
				// Trimmer can produce duplicate warnings for events https://github.com/dotnet/runtime/issues/83581
				ProducedBy = Tool.Trimmer)]
			[KeepsPublicMethods (Type = typeof (ClassWithKeptPublicMethods))]
			static event EventHandler Event_FieldSyntax;

			[field: Kept]
			[Kept]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			[KeptAttributeAttribute (typeof (KeepsPublicMethodsAttribute))]
			[ExpectedWarning ("IL2026", "--ClassWithKeptPublicMethods--")]
			[ExpectedWarning ("IL2026", "--ClassWithKeptPublicMethods--",
				// Trimmer can produce duplicate warnings for events
				ProducedBy = Tool.Trimmer)]
			[KeepsPublicMethods (Type = typeof (ClassWithKeptPublicMethods))]
			static event EventHandler Event_PropertySyntax {
				add { }
				remove { }
			}

			[Kept]
			public static void Test ()
			{
				Event_FieldSyntax += (sender, args) => { };
				Event_PropertySyntax += (sender, args) => { };
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
			public Type Type { get; [Kept] set; }

			[field: Kept]
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public string TypeName { get; [Kept] set; }
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
