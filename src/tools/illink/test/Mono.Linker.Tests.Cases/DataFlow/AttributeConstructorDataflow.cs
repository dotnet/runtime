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
	class AttributeConstructorDataflow
	{
		[KeptAttributeAttribute (typeof (KeepsPublicConstructorAttribute))]
		[KeptAttributeAttribute (typeof (KeepsPublicMethodsAttribute))]
		[KeepsPublicConstructor (typeof (ClassWithKeptPublicConstructor))]
		[KeepsPublicMethods ("Mono.Linker.Tests.Cases.DataFlow.AttributeConstructorDataflow+ClassWithKeptPublicMethods")]
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
			public static void KeptMethod () { }
			static void Method () { }
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
	}
}
