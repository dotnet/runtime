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
			AttributeWithConditionalExpression.Test ();
			RecursivePropertyDataFlow.Test ();
			RecursiveMethodDataFlow.Test ();
			RecursiveEventDataFlow.Test ();
			RecursiveFieldDataFlow.Test ();
		}

		class AttributesOnMethod
		{
			[Kept]
			public static void Test () {
				TestKeepsPublicConstructors ();
				TestKeepsPublicMethods ();
				TestKeepsPublicMethodsByName ();
				TestKeepsPublicFields ();
				TestTypeArray ();
			}

			[Kept]
			[KeptAttributeAttribute (typeof (KeepsPublicConstructorsAttribute))]
			[KeepsPublicConstructors (Type = typeof (ClassWithKeptPublicConstructor))]
			public static void TestKeepsPublicConstructors ()
			{
				typeof (AttributesOnMethod).GetMethod (nameof (TestKeepsPublicConstructors)).GetCustomAttribute (typeof (KeepsPublicConstructorsAttribute));
			}

			[Kept]
			[KeptAttributeAttribute (typeof (KeepsPublicMethodsAttribute))]
			[ExpectedWarning ("IL2026", "--ClassWithKeptPublicMethods--")]
			[KeepsPublicMethods (Type = typeof (ClassWithKeptPublicMethods))]
			public static void TestKeepsPublicMethods ()
			{
				typeof (AttributesOnMethod).GetMethod (nameof (TestKeepsPublicMethods)).GetCustomAttribute (typeof (KeepsPublicMethodsAttribute));
			}

			[Kept]
			[KeptAttributeAttribute (typeof (KeepsPublicMethodsAttribute))]
            // Trimmer/NativeAot only for now - https://github.com/dotnet/runtime/issues/95118
            [ExpectedWarning ("IL2026", "--ClassWithKeptPublicMethodsKeptByName--", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[KeepsPublicMethods (TypeName = "Mono.Linker.Tests.Cases.DataFlow.AttributePropertyDataflow+AttributesOnMethod+ClassWithKeptPublicMethodsKeptByName")]
			public static void TestKeepsPublicMethodsByName ()
			{
				typeof (AttributesOnMethod).GetMethod (nameof (TestKeepsPublicMethodsByName)).GetCustomAttribute (typeof (KeepsPublicMethodsAttribute));
			}

			[Kept]
			[KeptAttributeAttribute (typeof (KeepsPublicFieldsAttribute))]
			[KeepsPublicFields (Type = null, TypeName = null)]
			public static void TestKeepsPublicFields ()
			{
				typeof (AttributesOnMethod).GetMethod (nameof (TestKeepsPublicFields)).GetCustomAttribute (typeof (KeepsPublicFieldsAttribute));
			}

			[Kept]
			[KeptAttributeAttribute (typeof (TypeArrayAttribute))]
			[TypeArray (Types = new Type[] { typeof (AttributePropertyDataflow) })]
			public static void TestTypeArray ()
			{
				typeof (AttributesOnMethod).GetMethod (nameof (TestTypeArray)).GetCustomAttribute (typeof (TypeArrayAttribute));
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
			class ClassWithKeptPublicMethodsKeptByName
			{
				[Kept]
				[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
				[RequiresUnreferencedCode ("--ClassWithKeptPublicMethodsKeptByName--")]
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
			public static bool field;

			[Kept]
			public static void Test ()
			{
				typeof (AttributesOnField).GetField ("field").GetCustomAttribute (typeof (KeepsPublicMethodsAttribute));
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
			public static bool Property { [Kept] get; [Kept] set; }

			[Kept]
			public static void Test ()
			{
				typeof (AttributesOnProperty).GetProperty ("Property").GetCustomAttribute (typeof (KeepsPublicMethodsAttribute));
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
			[KeepsPublicMethods (Type = typeof (ClassWithKeptPublicMethods))]
			public static event EventHandler Event_FieldSyntax;

			[field: Kept]
			[Kept]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			[KeptAttributeAttribute (typeof (KeepsPublicMethodsAttribute))]
			[ExpectedWarning ("IL2026", "--ClassWithKeptPublicMethods--")]
			[KeepsPublicMethods (Type = typeof (ClassWithKeptPublicMethods))]
			public static event EventHandler Event_PropertySyntax {
				add { }
				remove { }
			}

			[Kept]
			public static void Test ()
			{
				typeof (AttributesOnEvent).GetEvent ("Event_FieldSyntax").GetCustomAttribute (typeof (KeepsPublicMethodsAttribute));
				typeof (AttributesOnEvent).GetEvent ("Event_PropertySyntax").GetCustomAttribute (typeof (KeepsPublicMethodsAttribute));
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

		class AttributeWithConditionalExpression
		{
			[Kept]
			[KeptAttributeAttribute (typeof (KeepsPublicMethodsAttribute))]
			// Trimmer/NativeAot only for now - https://github.com/dotnet/linker/issues/2273
			[ExpectedWarning ("IL2026", "--ClassWithKeptPublicMethods--", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[KeepsPublicMethods (TypeName = 1 + 1 == 2 ? "Mono.Linker.Tests.Cases.DataFlow.AttributePropertyDataflow+AttributeWithConditionalExpression+ClassWithKeptPublicMethods" : null)]
			public static void Test ()
			{
				typeof (AttributeWithConditionalExpression).GetMethod ("Test").GetCustomAttribute (typeof (KeepsPublicMethodsAttribute));
				typeof (AttributeWithConditionalExpression).GetField ("field").GetCustomAttribute (typeof (KeepsPublicMethodsAttribute));
			}

			// This testcase is an example where the analyzer may have a branch value while analyzing an attribute,
			// where the owning symbol is not a method.
			[Kept]
			[KeptAttributeAttribute (typeof (KeepsPublicMethodsAttribute))]
			// NativeAot doesn't handle the type name on fields: https://github.com/dotnet/runtime/issues/92259
			[ExpectedWarning ("IL2105", "Mono.Linker.Tests.Cases.DataFlow.AttributePropertyDataflow+AttributeWithConditionalExpression+ClassWithKeptPublicMethods", ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL2026", "--ClassWithKeptPublicMethods--", ProducedBy = Tool.Trimmer)]
			[KeepsPublicMethods (TypeName = 1 + 1 == 2 ? "Mono.Linker.Tests.Cases.DataFlow.AttributePropertyDataflow+AttributeWithConditionalExpression+ClassWithKeptPublicMethods" : null)]
			public static int field;

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
		class KeepsPublicPropertiesAttribute : Attribute
		{
			[Kept]
			public KeepsPublicPropertiesAttribute ()
			{
			}

			[field: Kept]
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)]
			public Type Type { get; [Kept] set; }
		}

		[Kept]
		[KeptBaseType (typeof (Attribute))]
		class KeepsPublicEventsAttribute : Attribute
		{
			[Kept]
			public KeepsPublicEventsAttribute ()
			{
			}

			[field: Kept]
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicEvents)]
			public Type Type { get; [Kept] set; }
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

		[Kept]
		class RecursivePropertyDataFlow
		{
			[field: Kept]
			[Kept]
			[KeptAttributeAttribute (typeof (KeepsPublicPropertiesAttribute))]
			[KeepsPublicProperties (Type = typeof (RecursivePropertyDataFlow))]
			public static int Property { [Kept] get; [Kept] set; }

			[Kept]
			public static void Test ()
			{
				typeof (RecursivePropertyDataFlow).GetProperty (nameof (Property)).GetCustomAttribute (typeof (KeepsPublicPropertiesAttribute));
				Property = 0;
			}
		}

		[Kept]
		class RecursiveEventDataFlow
		{
			[field: Kept]
			[Kept]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			[KeptAttributeAttribute (typeof (KeepsPublicEventsAttribute))]
			[KeepsPublicEvents (Type = typeof (RecursiveEventDataFlow))]
			public static event EventHandler Event;

			[Kept]
			public static void Test ()
			{
				typeof (RecursiveEventDataFlow).GetEvent (nameof (Event)).GetCustomAttribute (typeof (KeepsPublicEventsAttribute));
				Event += (sender, e) => { };
			}
		}

		[Kept]
		class RecursiveFieldDataFlow
		{
			[Kept]
			[KeptAttributeAttribute (typeof (KeepsPublicFieldsAttribute))]
			[KeepsPublicFields (Type = typeof (RecursiveFieldDataFlow))]
			public static int field;

			[Kept]
			public static void Test ()
			{
				typeof (RecursiveMethodDataFlow).GetField (nameof (field)).GetCustomAttribute (typeof (KeepsPublicFieldsAttribute));
				field = 0;
			}
		}

		[Kept]
		class RecursiveMethodDataFlow
		{
			[Kept]
			[KeptAttributeAttribute (typeof (KeepsPublicMethodsAttribute))]
			[KeepsPublicMethods (Type = typeof (RecursiveMethodDataFlow))]
			public static void Method () { }

			[Kept]
			public static void Test ()
			{
				typeof (RecursiveMethodDataFlow).GetMethod (nameof (Method)).GetCustomAttribute (typeof (KeepsPublicMethodsAttribute));
				Method ();
			}
		}
	}
}
