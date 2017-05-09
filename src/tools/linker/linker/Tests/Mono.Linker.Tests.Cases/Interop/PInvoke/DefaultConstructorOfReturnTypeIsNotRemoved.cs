using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Interop.PInvoke {
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
		[DllImport ("Unused")]
		private static extern A SomeMethod ();
	}
}