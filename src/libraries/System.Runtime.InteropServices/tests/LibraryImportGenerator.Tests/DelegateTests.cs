// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

using Xunit;

namespace LibraryImportGenerator.IntegrationTests
{
    partial class NativeExportsNE
    {
        public delegate void VoidVoid();

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "invoke_callback_after_gc")]
        public static partial void InvokeAfterGC(VoidVoid cb);

        public delegate int IntIntInt(int a, int b);

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "invoke_callback_blittable_args")]
        public static partial int InvokeWithBlittableArgument(IntIntInt cb, int a, int b);
    }

    public class DelegateTests
    {
        [Fact]
        public void DelegateIsKeptAliveDuringCall()
        {
            bool wasCalled = false;
            NativeExportsNE.InvokeAfterGC(new NativeExportsNE.VoidVoid(Callback));
            Assert.True(wasCalled);

            void Callback()
            {
                wasCalled = true;
            }
        }

        [Fact]
        public void DelegateIsCalledWithArgumentsInOrder()
        {
            const int a = 100;
            const int b = 50;
            int result;

            result = NativeExportsNE.InvokeWithBlittableArgument(new NativeExportsNE.IntIntInt(Callback), a, b);
            Assert.Equal(Callback(a, b), result);

            result = NativeExportsNE.InvokeWithBlittableArgument(new NativeExportsNE.IntIntInt(Callback), b, a);
            Assert.Equal(Callback(b, a), result);

            static int Callback(int a, int b)
            {
                // Use a noncommutative operation to validate passed in order.
                return a - b;
            }
        }
    }
}
