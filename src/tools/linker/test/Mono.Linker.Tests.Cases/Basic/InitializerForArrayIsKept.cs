using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
	public class InitializerForArrayIsKept
	{
		public static void Main ()
		{
			Method1 ();
			Method2 ();
			Method3 ();
		}

		[Kept]
		[KeptInitializerData]
		static void Method1 ()
		{
			Helper (new[] { 1, 2, 3 });
			Helper (new[] { 1, 2, 3, 4 });
			Helper (new[] { 3, 4, 5 });
		}

		[Kept]
		[KeptInitializerData]
		static void Method2 ()
		{
			Helper (new[] { 10, 11, 12 });
		}

		[Kept]
		[KeptInitializerData]
		static void Method3 ()
		{
			Helper (new[] { 10, 11, 12 });
		}

		[Kept]
		static void Helper<T> (T[] arr)
		{
		}
	}
}