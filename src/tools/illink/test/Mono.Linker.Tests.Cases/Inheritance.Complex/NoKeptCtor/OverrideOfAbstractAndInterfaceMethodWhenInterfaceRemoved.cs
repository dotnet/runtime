using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Complex.NoKeptCtor
{
	public class OverrideOfAbstractAndInterfaceMethodWhenInterfaceRemoved
	{
		public static void Main ()
		{
			Foo b = HelperToMarkFooAndRequireBase ();
			// Use IBar in another method so that IBar can be removed from Foo
			HelperToMarkIBar ();
		}

		[Kept]
		static Foo HelperToMarkFooAndRequireBase ()
		{
			return null;
		}

		[Kept]
		static void HelperToMarkIBar ()
		{
			GetAnIBar ().Method ();
		}

		[Kept]
		static IBar GetAnIBar ()
		{
			return null;
		}

		[Kept]
		abstract class Base
		{
			public abstract void Method ();
		}

		[Kept]
		[KeptBaseType (typeof (Base))]
		class Foo : Base, IBar
		{
			public override void Method ()
			{
				UsedByOverride ();
			}

			void UsedByOverride ()
			{
			}
		}

		[Kept]
		interface IBar
		{
			[Kept]
			void Method ();
		}
	}
}