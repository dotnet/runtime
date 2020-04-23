using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.OverrideRemoval
{
	public class OverrideOfVirtualCanBeRemoved3
	{
		public static void Main ()
		{
			HelperToMarkFoo2AndRequireBase ();
			Base b = HelperToMarkFooAndRequireBase ();
			b.Method ();
		}

		[Kept]
		static Foo HelperToMarkFooAndRequireBase ()
		{
			return null;
		}

		[Kept]
		static Foo2 HelperToMarkFoo2AndRequireBase ()
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
		[KeptBaseType (typeof (Base))]
		class Foo : Base
		{
			public override void Method ()
			{
			}
		}

		[Kept]
		[KeptBaseType (typeof (Foo))]
		class Foo2 : Foo
		{
		}
	}
}