// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Mono.Linker.Tests.Cases.DataFlow;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

[assembly: KeptAttributeAttribute (typeof (AttributeConstructorDataflow.KeepsPublicPropertiesAttribute))]
[assembly: ExpectedWarning ("IL2026", "--ClassWithKeptPublicProperties--", Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/linker/issues/2273")]
[assembly: AttributeConstructorDataflow.KeepsPublicProperties (typeof (AttributeConstructorDataflow.ClassWithKeptPublicProperties))]

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[Kept]
	[ExpectedNoWarnings]
	class AttributeConstructorDataflow
	{
		[KeptAttributeAttribute (typeof (KeepsPublicConstructorAttribute))]
		[KeptAttributeAttribute (typeof (KeepsPublicMethodsAttribute))]
		[KeptAttributeAttribute (typeof (KeepsPublicFieldsAttribute))]
		[KeptAttributeAttribute (typeof (TypeArrayAttribute))]
		[KeepsPublicConstructor (typeof (ClassWithKeptPublicConstructor))]
		[KeepsPublicMethods ("Mono.Linker.Tests.Cases.DataFlow.AttributeConstructorDataflow+ClassWithKeptPublicMethods")]
		[KeepsPublicFields (null, null)]
		[TypeArray (new Type[] { typeof (AttributeConstructorDataflow) })]
		[ExpectedWarning ("IL2026", "--ClassWithKeptPublicMethods--", Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/linker/issues/2273")]
		public static void Main ()
		{
			typeof (AttributeConstructorDataflow).GetMethod ("Main").GetCustomAttribute (typeof (KeepsPublicConstructorAttribute));
			typeof (AttributeConstructorDataflow).GetMethod ("Main").GetCustomAttribute (typeof (KeepsPublicMethodsAttribute));
			AllOnSelf.Test ();
		}

		[Kept]
		[KeptBaseType (typeof (Attribute))]
		class KeepsPublicConstructorAttribute : Attribute
		{
			[Kept]
			public KeepsPublicConstructorAttribute (
				[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
				[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
				Type type)
			{
			}
		}

		[Kept]
		[KeptBaseType (typeof (Attribute))]
		class KeepsPublicMethodsAttribute : Attribute
		{
			[Kept]
			public KeepsPublicMethodsAttribute (
				[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				string type)
			{
			}
		}

		// Used to test null parameter values
		[Kept]
		[KeptBaseType (typeof (Attribute))]
		class KeepsPublicFieldsAttribute : Attribute
		{
			[Kept]
			public KeepsPublicFieldsAttribute (
				[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
				[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
				Type type,
				[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
				[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
				string typeName)
			{
			}
		}

		// Used to test assembly-level attribute
		[Kept]
		[KeptBaseType (typeof (Attribute))]
		public class KeepsPublicPropertiesAttribute : Attribute
		{
			[Kept]
			public KeepsPublicPropertiesAttribute (
				[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)]
				Type type)
			{
			}
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
		public class ClassWithKeptPublicProperties
		{
			[Kept]
			public static int KeptProperty {
				[Kept]
				[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
				[RequiresUnreferencedCode ("--ClassWithKeptPublicProperties--")]
				get => 0;
			}
			static int Property { get; }
		}

		[Kept]
		class AllOnSelf
		{
			[Kept]
			public static void Test ()
			{
				var t = typeof (KeepAllOnSelf);
			}

			[Kept]
			[KeptBaseType (typeof (Attribute))]
			class KeepsAllAttribute : Attribute
			{
				[Kept]
				public KeepsAllAttribute (
					[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
					Type type)
				{
				}
			}

			[KeepsAll (typeof (KeepAllOnSelf))]
			[Kept]
			[KeptAttributeAttribute (typeof (KeepsAllAttribute))]
			[KeptMember (".ctor()")]
			class KeepAllOnSelf
			{
				[Kept]
				public void Method () { }

				[Kept]
				public int Field;
			}
		}

		[Kept]
		[KeptBaseType (typeof (Attribute))]
		class TypeArrayAttribute : Attribute
		{
			[Kept]
			public TypeArrayAttribute (Type[] types) // This should not trigger data flow analysis of the parameter
			{
			}
		}
	}
}
