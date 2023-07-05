// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.VirtualMethods
{
	public class OverrideOfAbstractInUnmarkedClassIsRemoved
	{
		[Kept]
		public static void Main ()
		{
			MarkedBase x = new MarkedDerived ();
			x.Method ();

			UsedSecondLevelTypeWithAbstractBase y = new ();
			y.Method ();

			UsedSecondLevelType z = new ();
			z.Method ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		abstract class MarkedBase
		{
			[Kept]
			public abstract int Method ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (MarkedBase))]
		class MarkedDerived : MarkedBase
		{
			[Kept]
			public override int Method () => 1;
		}

		class UnmarkedDerived : MarkedBase
		{
			public override int Method () => 1;
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (MarkedBase))]
		class UnusedIntermediateType : MarkedBase
		{
			[Kept]
			public override int Method () => 1;
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (UnusedIntermediateType))]
		class UsedSecondLevelType : UnusedIntermediateType
		{
			[Kept]
			public override int Method () => 1;
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (MarkedBase))]
		abstract class UnusedIntermediateTypeWithAbstractOverride : MarkedBase
		{
			[Kept]
			public abstract override int Method ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (UnusedIntermediateTypeWithAbstractOverride))]
		class UsedSecondLevelTypeWithAbstractBase : UnusedIntermediateTypeWithAbstractOverride
		{
			[Kept]
			public override int Method () => 1;
		}

		class UnusedSecondLevelTypeWithAbstractBase : UnusedIntermediateTypeWithAbstractOverride
		{
			public override int Method () => 1;
		}

		class UnusedSecondLevelType : UnusedIntermediateType
		{
			public override int Method () => 1;
		}
	}
}
