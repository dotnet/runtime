
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection
{
	class IsAssignableFrom
	{
		[Kept]
		interface IFoo { }

		[Kept]
		[KeptInterface (typeof (IFoo))]
		class Foo : IFoo { }

		static void Main ()
		{
			typeof (IFoo).IsAssignableFrom (typeof (Foo));
		}

	}
}
