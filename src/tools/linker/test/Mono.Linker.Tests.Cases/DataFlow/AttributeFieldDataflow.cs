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
		[KeepsPublicConstructors (Type = typeof (ClassWithKeptPublicConstructor))]
		[KeepsPublicMethods ("Mono.Linker.Tests.Cases.DataFlow.AttributeFieldDataflow+ClassWithKeptPublicMethods")]
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
	}
}
