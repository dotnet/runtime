using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Interop.PInvoke
{
    [SetupCompileArgument("/unsafe")]
    [KeptModuleReference("Unused")]
    class UnusedFieldsOfTypesAreNotRemoved
    {
        public static unsafe void Main()
        {
            var a = new A();
            SomeMethod(a);
            SomePointerMethod(null);
            SomeArrayMethod(null);
        }

        [KeptMember(".ctor()")]
        class A
        {
            [Kept] private int field1;

            [Kept] private int field2;
        }

        [Kept]
        struct PointedTo
        {
            [Kept] private int field1;

            [Kept] private int field2;
        }

        [Kept]
        class ArrayElement
        {
            [Kept] private int field1;

            [Kept] private int field2;
        }

        [Kept]
        [DllImport("Unused")]
        private static extern void SomeMethod(A a);

        [Kept]
        [DllImport("Unused")]
        private static extern unsafe void SomePointerMethod(PointedTo* a);

        [Kept]
        [DllImport("Unused")]
        private static extern void SomeArrayMethod(ArrayElement[] a);
    }
}
