using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.OverrideRemoval
{
	public class OverrideThatAlsoFulfilsInterface
	{
		public static void Main ()
		{
			Base b = HelperToMarkFooAndRequireBase ();
			b.Method ();

			MethodToUseTheInterface ();
		}

		[Kept]
		static void MethodToUseTheInterface ()
		{
			// Now use the interface method so that it is kept
			IFoo f = new Bar ();
			f.Method ();
		}

		[Kept]
		static Foo HelperToMarkFooAndRequireBase ()
		{
			return null;
		}

		[Kept]
		class Base
		{
			[Kept]
			public virtual void Method ()
			{
			}
		}

		[Kept]
		interface IFoo
		{
			[Kept]
			void Method ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptInterface (typeof (IFoo))]
		class Bar : IFoo
		{
			[Kept]
			public void Method ()
			{
			}
		}

		[Kept]
		[KeptBaseType (typeof (Base))]
		class Foo : Base, IFoo
		{
			public override void Method ()
			{
			}
		}
	}
}