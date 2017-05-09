using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Interop {
	class FieldsOfTypeMarkedSequentialLayoutAreNotRemoved {
		public static void Main ()
		{
			new A ();
		}

		[StructLayout (LayoutKind.Sequential)]
		[KeptMember (".ctor()")]
		class A {
			[Kept] int a;
		}
	}
}