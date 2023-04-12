using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoKeptCtor
{
	public class LocalDowncastDoesNotCuaseOtherTypesToKeepInterface
	{
		public static void Main ()
		{
			Foo f = null;
			IFoo i = f;
			i.Method ();
			DoOtherStuff ();
		}

		[Kept]
		static void DoOtherStuff ()
		{
			HelperToUseBar (null);
		}

		[Kept]
		static void HelperToUseBar (Bar arg)
		{
		}

		[Kept]
		[KeptInterface (typeof (IFoo))]
		class Foo : IFoo
		{
			[Kept] // TODO : It should be safe to stub this.  It can't actually be called because no instance of Foo ever exists
			public void Method ()
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
		class Bar : IFoo
		{
			public void Method ()
			{
			}
		}
	}
}