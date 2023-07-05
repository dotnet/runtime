using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.OverrideRemoval
{
	public class OverrideOfAbstractIsKept
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
		abstract class Base
		{
			[Kept]
			public abstract void Method ();
		}

		[Kept]
		[KeptBaseType (typeof (Base))]
		abstract class Base2 : Base
		{
		}

		[Kept]
		[KeptBaseType (typeof (Base2))]
		abstract class Base3 : Base2
		{
		}

		[Kept]
		[KeptBaseType (typeof (Base3))]
		class Foo : Base3
		{
			[Kept]
			public override void Method ()
			{
			}
		}
	}
}