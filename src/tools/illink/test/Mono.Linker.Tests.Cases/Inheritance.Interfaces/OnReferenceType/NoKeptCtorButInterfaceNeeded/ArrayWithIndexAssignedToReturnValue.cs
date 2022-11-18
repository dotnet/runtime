using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoKeptCtorButInterfaceNeeded
{
	public class ArrayWithIndexAssignedToReturnValue
	{
		public static void Main ()
		{
			IFoo[] arr = new IFoo[5];
			arr[0] = GetAFoo ();
		}

		[Kept]
		static Foo GetAFoo ()
		{
			return null;
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