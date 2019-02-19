using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Interop.InternalCalls {
	class DefaultConstructorOfReturnTypeIsNotRemoved {
		public static void Main ()
		{
			var a = SomeMethod ();
		}

		class A {
			[Kept]
			public A ()
			{
			}
		}

		[Kept]
		[MethodImpl (MethodImplOptions.InternalCall)]
		static extern A SomeMethod ();
	}
}