using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Statics
{
	public class MixedStaticFieldInitializerAndCtor
	{
		public static void Main ()
		{
			C.Foo ();
		}

		static class C
		{
			[Kept]
			public static object o = new object ();

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
