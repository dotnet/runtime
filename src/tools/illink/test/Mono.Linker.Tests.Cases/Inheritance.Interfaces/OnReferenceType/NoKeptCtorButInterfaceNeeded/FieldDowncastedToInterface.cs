using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoKeptCtorButInterfaceNeeded
{
	public class FieldDowncastedToInterface
	{
		[Kept]
		private static Foo f;
		[Kept]
		private static IFoo i;

		public static void Main ()
		{
			f = null;
			i = f;
			i.Method ();
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
	}
}