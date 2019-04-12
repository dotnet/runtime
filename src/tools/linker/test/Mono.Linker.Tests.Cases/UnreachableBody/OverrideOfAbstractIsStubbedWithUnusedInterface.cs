using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.UnreachableBody {
	public class OverrideOfAbstractIsStubbedWithUnusedInterface {
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
		class Foo : Base, IBar {
			[Kept]
			[ExpectBodyModified]
			public override void Method ()
			{
				UsedByOverride ();
			}

			void UsedByOverride ()
			{
			}
		}

		interface IBar
		{
			void Method();
		}
	}
}