using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoKeptCtorButInterfaceNeeded
{
	public class GenericTypeWithConstraint
	{
		public static void Main ()
		{
			object o = new Bar<Foo> ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		class Bar<T> where T : IFoo
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