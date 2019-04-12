using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.UnreachableBody {
	public class OverrideOfAbstractAndInterfaceMethodWhenInterfaceRemoved2 {
		public static void Main ()
		{
			Foo f = HelperToMarkFooAndRequireBase ();
			f.Method ();
			// Use IBar in another method so that IBar can be removed from Foo
			HelperToMarkIBar ();
		}

		[Kept]
		static Foo HelperToMarkFooAndRequireBase ()
		{
			return null;
		}

		[Kept]
		static IBar GetAnIBar ()
		{
			return null;
		}
		
		[Kept]
		static void HelperToMarkIBar()
		{
			GetAnIBar().Method();
		}

		[Kept]
		abstract class Base {
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

		[Kept]
		interface IBar {
			[Kept]
			void Method();
		}
	}
}