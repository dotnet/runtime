using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.OverrideRemoval
{
	public class OverrideOfVirtualCanBeRemoved
	{
		public static void Main ()
		{
			Base b = HelperToMarkFooAndRequireBase ();
			b.Method ();
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
		[KeptBaseType (typeof (Base))]
		class Foo : Base
		{
			public override void Method ()
			{
			}
		}
	}
}