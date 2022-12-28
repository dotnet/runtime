using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoKeptCtorButInterfaceNeeded
{
	public class InterfaceOnMultipleBases
	{
		public static void Main ()
		{
			Foo f = null;
			Helper (f);
		}

		[Kept]
		static void Helper (IFoo f)
		{
		}

		[Kept]
		[KeptInterface (typeof (IFoo))]
		class Base : IFoo
		{
		}

		[Kept]
		[KeptBaseType (typeof (Base))]
		[KeptInterface (typeof (IFoo))]
		class Base2 : Base, IFoo
		{
		}

		[Kept]
		[KeptBaseType (typeof (Base2))]
		class Base3 : Base2
		{
		}

		[Kept]
		[KeptBaseType (typeof (Base3))]
		[KeptInterface (typeof (IFoo))]
		class Foo : Base3, IFoo
		{
		}

		[Kept]
		interface IFoo
		{
		}
	}
}