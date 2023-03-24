using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoKeptCtorButInterfaceNeeded
{
	public class LocalAndNestedInterfaces
	{
		public static void Main ()
		{
			Foo f = null;
			IFoo i = f;
			IBase2 b = i;

			Helper (b);
		}

		[Kept]
		static void Helper (IBase2 f)
		{
		}

		[Kept]
		[KeptInterface (typeof (IFoo))]
		[KeptInterface (typeof (IBase2))]
		class Foo : IFoo
		{
		}

		interface IBase
		{
		}

		[Kept]
		interface IBase2 : IBase
		{
		}

		[Kept]
		[KeptInterface (typeof (IBase2))]
		interface IFoo : IBase2
		{
		}
	}
}