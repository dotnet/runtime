using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Generics {
	public class UsedOverloadedGenericMethodWithNoParametersIsNotStripped {
		public static void Main ()
		{
			B.Call<string, int> ();
		}

		public class B {
			public static void Method<T> ()
			{
			}

			[Kept]
			public static void Method<TKey, TValue> ()
			{
			}

			[Kept]
			public static void Call<TKey, TValue> ()
			{
				B.Method<TKey, TValue> ();
			}
		}
	}
}