using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoKeptCtorButInterfaceNeeded
{
	public class LocalArrayPassedAsParameter
	{
		public static void Main ()
		{
			Foo[] arr = null;
			Helper (arr);
		}

		[Kept]
		static void Helper (IFoo[] f)
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