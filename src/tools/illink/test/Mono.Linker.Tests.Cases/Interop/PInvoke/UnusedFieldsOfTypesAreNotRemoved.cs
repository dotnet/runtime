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
            SomePointerToPointerMethod(null);
            SomeArrayOfArraysMethod(null);
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
        struct ArrayElement
        {
            [Kept] private int field1;

            [Kept] private int field2;
        }

        [Kept]
        struct PointedToByPointer
        {
            [Kept] private int field1;

            [Kept] private int field2;
        }

        [Kept]
        struct NestedArrayElement
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

        [Kept]
        [DllImport("Unused")]
        private static extern unsafe void SomePointerToPointerMethod(PointedToByPointer** a);

        [Kept]
        [DllImport("Unused")]
        private static extern void SomeArrayOfArraysMethod(NestedArrayElement[][] a);
    }
}
