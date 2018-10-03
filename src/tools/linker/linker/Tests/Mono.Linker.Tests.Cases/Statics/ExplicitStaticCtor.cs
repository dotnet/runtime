using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Statics
{
	public class ExplicitStaticCtor
	{
		public static void Main ()
		{
			C.Foo ();
		}

		static class C
		{
			[Kept]
			static C ()
			{
			}

			[Kept]
			public static void Foo ()
			{
			}
		}
	}
}
