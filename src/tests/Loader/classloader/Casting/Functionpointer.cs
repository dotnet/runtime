using System;
using System.Runtime.InteropServices;
using Xunit;

namespace TestFunctionPointer
{
    unsafe class TestThings
    {
        public static delegate* managed<int>[][] Functions = {
            new delegate* managed<int>[]
            {
                &Function,
            },
        };

        public static int Function() => 100;

        public static delegate* unmanaged<int>[][] Functions1 = {
            new delegate* unmanaged<int>[]
            {
                &Function1,
            },
        };

        [UnmanagedCallersOnly]
        public static int Function1() => 100;

        public static delegate* managed<int, int>[][] Functions2 = {
            new delegate* managed<int, int>[]
            {
                &Function2,
            },
        };

        public static int Function2(int a) {
            return a;
        }
    }

    public unsafe class Program
    {
        [Fact]
        public static int TestEntryPoint()
        {
            return TestThings.Functions2[0][0](TestThings.Functions[0][0]());
        }
    }
}