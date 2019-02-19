using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic {
	class NestedDelegateInvokeMethodsPreserved {
		[Kept]
		static B.Delegate @delegate;

		static void Main ()
		{
			System.GC.KeepAlive (@delegate);
		}

		[Kept]
		public class B {
			[Kept]
			[KeptMember ("Invoke()")]
			[KeptMember ("BeginInvoke(System.AsyncCallback,System.Object)")]
			[KeptMember ("EndInvoke(System.IAsyncResult)")]
			[KeptMember (".ctor(System.Object,System.IntPtr)")]
			[KeptBaseType (typeof (System.MulticastDelegate))]
			public delegate void Delegate ();
		}
	}
}