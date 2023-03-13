using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoKeptCtorButInterfaceNeeded
{
	public class LocalPassedAsParameterToGeneric
	{
		public static void Main ()
		{
			Foo f = null;
			Helper<IFoo> (f);
		}

		[Kept]
		static void Helper<T> (T f)
		{
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