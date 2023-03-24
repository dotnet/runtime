using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoKeptCtorButInterfaceNeeded
{
	public class LocalPassedAsParameterToGenericWithConstraint2
	{
		public static void Main ()
		{
			Foo f = null;
			Foo2 f2 = null;
			Helper (f, f2);
		}

		[Kept]
		static void Helper<T> (T f, Foo2 arg2) where T : IFoo
		{
		}

		[Kept]
		[KeptInterface (typeof (IFoo))]
		class Foo : IFoo
		{
		}

		[Kept]
		[KeptInterface (typeof (IFoo))] // technically this can be removed, but it would require more complex knowledge of the stack to do so
		class Foo2 : IFoo
		{
		}


		[Kept]
		interface IFoo
		{
		}
	}
}