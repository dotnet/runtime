using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Interop.InternalCalls
{
	class UnusedFieldsOfTypesAreNotRemoved
	{
		public static void Main ()
		{
			var a = new A ();
			SomeMethod (a);
		}

		[KeptMember (".ctor()")]
		class A
		{
			[Kept] private int field1;

			[Kept] private int field2;
		}

		[Kept]
		[MethodImpl (MethodImplOptions.InternalCall)]
		static extern void SomeMethod (A a);
	}
}