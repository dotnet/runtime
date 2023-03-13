using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.TestFramework
{
	public class CanCheckInitializersByIndex
	{
		public static void Main ()
		{
			Method1 ();
			Method2 ();
		}

		[Kept]
		[KeptInitializerData (0)]
		[KeptInitializerData (1)]
		[KeptInitializerData (2)]
		static void Method1 ()
		{
			Helper (new[] { 1, 2, 3 });
			Helper (new[] { 1, 2, 3, 4 });
			Helper (new[] { 3, 4, 5 });
		}

		[Kept]
		[KeptInitializerData (0)]
		static void Method2 ()
		{
			Helper (new[] { 10, 11, 12 });
		}

		[Kept]
		static void Helper<T> (T[] arr)
		{
		}
	}
}