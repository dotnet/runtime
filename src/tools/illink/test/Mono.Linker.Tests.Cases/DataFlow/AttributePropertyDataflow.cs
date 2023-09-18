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
		[KeptAttributeAttribute (typeof (KeepsPublicConstructorsAttribute))]
		[KeptAttributeAttribute (typeof (KeepsPublicMethodsAttribute))]
		[KeptAttributeAttribute (typeof (KeepsPublicFieldsAttribute))]
		[KeptAttributeAttribute (typeof (TypeArrayAttribute))]
		[KeepsPublicConstructors (Type = typeof (ClassWithKeptPublicConstructor))]
		[KeepsPublicMethods (TypeName = "Mono.Linker.Tests.Cases.DataFlow.AttributePropertyDataflow+ClassWithKeptPublicMethods")]
		[KeepsPublicFields (Type = null, TypeName = null)]
		[TypeArray (Types = new Type[] { typeof (AttributePropertyDataflow) })]
		// Trimmer only for now - https://github.com/dotnet/linker/issues/2273
		[ExpectedWarning ("IL2026", "--ClassWithKeptPublicMethods--", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		public static void Main ()
		{
			typeof (AttributePropertyDataflow).GetMethod (nameof (Main)).GetCustomAttribute (typeof (KeepsPublicConstructorsAttribute));
			typeof (AttributePropertyDataflow).GetMethod (nameof (Main)).GetCustomAttribute (typeof (KeepsPublicMethodsAttribute));
			RecursivePropertyDataFlow.Test ();
			RecursiveEventDataFlow.Test ();
			RecursiveFieldDataFlow.Test ();
			RecursiveMethodDataFlow.Test ();
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
