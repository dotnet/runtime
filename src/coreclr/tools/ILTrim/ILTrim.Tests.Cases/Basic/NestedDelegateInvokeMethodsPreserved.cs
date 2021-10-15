using Mono.Linker.Tests.Cases.Expectations.Assertions;

#pragma warning disable 649

namespace Mono.Linker.Tests.Cases.Basic
{
    [IgnoreTestCase("Support for delegates is not implemented yet")]
    class NestedDelegateInvokeMethodsPreserved
    {
        [Kept]
        static B.Delegate @delegate;

        static void Main()
        {
            System.GC.KeepAlive(@delegate);
        }

        [Kept]
        public class B
        {
            [Kept]
            [KeptMember("Invoke()")]
            [KeptMember(".ctor(System.Object,System.IntPtr)")]
            [KeptBaseType(typeof(System.MulticastDelegate))]
            public delegate void Delegate();
        }
    }
}
