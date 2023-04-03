﻿using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Generics
{
	public class MdArrayVariantCasting
	{
		[Kept]
		interface IFoo { }

		[Kept]
		[KeptInterface (typeof (IFoo))]
		class Foo : IFoo
		{
			// Even though Foo is never allocated (as seen from the removal of the constructor),
			// we need to make sure trimming tools keep its interface list because it's relevant
			// for variant casting.
			public Foo () { }
		}

		[Kept]
		class Base
		{
			// Removed
			public Base () { }
		}

		[Kept]
		[KeptBaseType (typeof (Base))]
		class Derived : Base
		{
			// Even though Derived is never allocated (as seen from the removal of the constructor),
			// we need to make sure trimming tools keep its base types because it's relevant
			// for variant casting.
			public Derived () { }
		}

		public static void Main ()
		{
			// These casts need to succeed after trimming
			var foos = (IFoo[,]) (object) new Foo[0, 0];
			var bases = (Base[,]) (object) new Derived[0, 0];
		}
	}
}