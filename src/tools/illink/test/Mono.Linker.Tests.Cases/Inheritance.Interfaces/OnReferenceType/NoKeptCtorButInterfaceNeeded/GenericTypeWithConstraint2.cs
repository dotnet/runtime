using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoKeptCtorButInterfaceNeeded
{
	public class GenericTypeWithConstraint2
	{
		public static void Main ()
		{
			Foo f = null;
			Bar<Foo>.Helper (f);
		}

		[Kept]
		static class Bar<T> where T : IFoo
		{
			[Kept]
			public static void Helper (T arg)
			{
			}
		}

		[Kept]
		[KeptInterface (typeof (IFoo))]
		class Foo : IFoo
		{
		}

		[Kept]
		interface IFoo
		{
		}
	}
}