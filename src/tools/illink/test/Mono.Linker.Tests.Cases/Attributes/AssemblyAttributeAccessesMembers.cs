// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Attributes;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

[assembly: KeptAttributeAttribute(typeof(AccessesMembersAttribute))]
[assembly: AccessesMembers(typeof(AssemblyAttributeAccessesMembers.TypeWithMembers))]
namespace Mono.Linker.Tests.Cases.Attributes
{
	[Kept]
	class AssemblyAttributeAccessesMembers
	{
		[Kept]
		public static void Main()
		{
			typeof (AssemblyAttributeAccessesMembers).Assembly.GetCustomAttributes (false);
		}

		[Kept]
		public class TypeWithMembers
		{
			[Kept]
			public TypeWithMembers () { }

			[Kept]
			public void Method () { }

			[Kept]
			public int Field;

			[Kept]
			[KeptBackingField]
			public int Property { [Kept]get; [Kept]set; }

			[Kept]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			[KeptBackingField]
			public event EventHandler Event;
		}
	}

	[Kept]
	[KeptBaseType(typeof(Attribute))]
	public class AccessesMembersAttribute: Attribute
	{
		[Kept]
		public AccessesMembersAttribute([KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))][DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]Type type)
		{
		}
	}
}
