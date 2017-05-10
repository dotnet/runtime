using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Interop.InternalCalls.Com {
	class DefaultConstructorOfReturnTypeIsRemoved {
		public static void Main ()
		{
			var a = SomeMethod ();
		}

		[Kept]
		[ComImport]
		[Guid ("D7BB1889-3AB7-4681-A115-60CA9158FECA")]
		class A {
		}

		[Kept]
		[MethodImpl (MethodImplOptions.InternalCall)]
		static extern A SomeMethod ();
	}
}
