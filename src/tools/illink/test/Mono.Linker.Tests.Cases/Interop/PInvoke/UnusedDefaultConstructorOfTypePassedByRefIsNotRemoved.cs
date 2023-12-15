using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Interop.PInvoke
{
	[KeptModuleReference ("Unused")]
	class UnusedDefaultConstructorOfTypePassedByRefIsNotRemoved
	{
		public static void Main ()
		{
			var a = new A (1);
			SomeMethod (ref a);
		}

		class A
		{
			[Kept]
			public A ()
			{
			}

			[Kept]
			public A (int other)
			{
			}
		}

		[Kept]
		[DllImport ("Unused")]
		private static extern void SomeMethod (ref A a);
	}
}