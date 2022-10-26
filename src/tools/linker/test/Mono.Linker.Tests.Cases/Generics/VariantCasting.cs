using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Generics
{
	public class VariantCasting
	{
		[Kept]
		interface IVariant<out T> { }

		[Kept]
		interface IFoo { }

		[Kept]
		[KeptInterface (typeof (IFoo))]
		class Foo : IFoo
		{
			// Even though Foo is never allocated (as seen from the removal of the constructor),
			// we need to make sure linker keeps its interface list because it's relevant
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
			// we need to make sure linker keeps its base types because it's relevant
			// for variant casting.
			public Derived () { }
		}

		[Kept]
		[KeptInterface (typeof (IVariant<>))]
		class Interesting<T> : IVariant<T>
		{
			[Kept]
			public Interesting () { }
		}

		[Kept]
		[KeptBaseType (typeof (Interesting<>), "T")]
		class Boring<T> : Interesting<T>
		{
			[Kept]
			public Boring () { }
		}

		public static void Main ()
		{
			// These casts need to succeed after trimming
			IVariant<IFoo> foos = (IVariant<IFoo>) (object) new Boring<Foo> ();
			IVariant<Base> bases = (IVariant<Base>) (object) new Boring<Derived> ();
		}
	}
}