using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoKeptCtorButInterfaceNeeded
{
	public class LocalPassedAsParameterToGenericWithConstraint
	{
		public static void Main ()
		{
			Foo f = null;
			Helper (f);
		}

		[Kept]
		static void Helper<T> (T f) where T : IFoo
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