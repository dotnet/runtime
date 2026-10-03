using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Interop.PInvoke.Com
{
    class InterfacesOfReturnTypeAreKept
    {
        public static void Main()
        {
            ((I)SomeMethod()).Method();
        }

        [Kept]
        interface I
        {
            [Kept]
            void Method();
        }

        [Kept]
        [KeptInterface(typeof(I))]
        [KeptAttributeAttribute(typeof(GuidAttribute))]
        [ComImport]
        [Guid("D7BB1889-3AB7-4681-A115-60CA9158FECA")]
        class A : I
        {
            [Kept]
            [MethodImpl(MethodImplOptions.InternalCall)]
            public extern void Method();
        }

        [Kept]
        [MethodImpl(MethodImplOptions.InternalCall)]
        static extern A SomeMethod();
    }
}
