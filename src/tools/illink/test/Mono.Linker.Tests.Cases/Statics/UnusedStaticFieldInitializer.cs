using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Statics
{
	public class UnusedStaticFieldInitializer
	{
		public static void Main ()
		{
			C.Foo ();

			new C2 (123);
		}

		static class C
		{
			public static object o = new object ();

			[Kept]
			public static void Foo ()
			{
			}
		}

		class C2
		{
			public static object o = new object ();

			[Kept]
			public int Field;

			[Kept]
			public C2 (int val) => Field = val;
		}
	}
}
