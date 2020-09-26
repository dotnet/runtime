using System;
using System.Runtime.InteropServices;

namespace NativeExports
{
    public unsafe class ScalarOps
    {
        [UnmanagedCallersOnly(EntryPoint = "sumi")]
        public static int Sum(int a, int b)
        {
            return a + b;
        }

        [UnmanagedCallersOnly(EntryPoint = "sumouti")]
        public static void SumOut(int a, int b, int* c)
        {
            *c = a + b;
        }

        [UnmanagedCallersOnly(EntryPoint = "sumrefi")]
        public static void SumRef(int a, int* b)
        {
            *b += a;
        }
    }
}
