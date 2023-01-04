using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Generics
{
	class GenericInstanceInterfaceMethodImplementedWithDifferentGenericArgumentNameDoesNotGetStripped
	{
		public static void Main ()
		{
			ISomething it = new Concrete ();
			it.ShouldNotGetStripped<int> ();
		}

		[Kept]
		public class GenericType<T>
		{
		}

		public interface ISomething
		{
			[Kept]
			GenericType<TInInterface> ShouldNotGetStripped<TInInterface> ();
		}

		[KeptMember (".ctor()")]
		[KeptInterface (typeof (ISomething))]
		public class Concrete : ISomething
		{
			[Kept]
			public GenericType<TInConcrete> ShouldNotGetStripped<TInConcrete> ()
			{
				throw new System.Exception ();
			}
		}
	}
}