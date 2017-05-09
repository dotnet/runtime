using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Interop.PInvoke {
	class UnusedFieldsOfTypesPassedByRefAreNotRemoved {
		public static void Main ()
		{
			var a = new A ();
			SomeMethod (ref a);
		}

		[KeptMember (".ctor()")]
		class A {
			[Kept] private int field1;

			[Kept] private int field2;
		}

		[Kept]
		[DllImport ("Unused")]
		private static extern void SomeMethod (ref A a);
	}
}