using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Xunit;

namespace DllImportGenerator.IntegrationTests
{
    partial class NativeExportsNE
    {
        public partial class FunctionPointer
        {
            [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "invoke_callback_after_gc")]
            public static unsafe partial void InvokeAfterGC(delegate* <void> cb);

            [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "invoke_callback_after_gc")]
            public static unsafe partial void InvokeAfterGC(delegate* unmanaged<void> cb);

            [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "invoke_callback_after_gc")]
            public static unsafe partial void InvokeAfterGC(delegate* unmanaged[Stdcall]<void> cb);

            [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "invoke_callback_blittable_args")]
            public static unsafe partial int InvokeWithBlittableArgument(delegate* <int, int, int> cb, int a, int b);

            [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "invoke_callback_blittable_args")]
            public static unsafe partial int InvokeWithBlittableArgument(delegate* unmanaged<int, int, int> cb, int a, int b);

            [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "invoke_callback_blittable_args")]
            public static unsafe partial int InvokeWithBlittableArgument(delegate* unmanaged[Stdcall]<int, int, int> cb, int a, int b);
        }
    }

    public class FunctionPointerTests
    {
        private static bool wasCalled;

        [Fact]
        public unsafe void InvokedAfterGC()
        {
            wasCalled = false;
            NativeExportsNE.FunctionPointer.InvokeAfterGC(&Callback);
            Assert.True(wasCalled);

            wasCalled = false;
            NativeExportsNE.FunctionPointer.InvokeAfterGC(&CallbackUnmanaged);
            Assert.True(wasCalled);

            wasCalled = false;
            NativeExportsNE.FunctionPointer.InvokeAfterGC(&CallbackUnmanagedStdcall);
            Assert.True(wasCalled);

            static void Callback()
            {
                wasCalled = true;
            }

            [UnmanagedCallersOnly]
            static void CallbackUnmanaged()
            {
                wasCalled = true;
            }

            [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
            static void CallbackUnmanagedStdcall()
            {
                wasCalled = true;
            }
        }

        [Fact]
        public unsafe void CalledWithArgumentsInOrder()
        {
            const int a = 100;
            const int b = 50;
            int result;

            int expected = Callback(a, b);
            result = NativeExportsNE.FunctionPointer.InvokeWithBlittableArgument(&Callback, a, b);
            Assert.Equal(expected, result);

            result = NativeExportsNE.FunctionPointer.InvokeWithBlittableArgument(&CallbackUnmanaged, a, b);
            Assert.Equal(expected, result);

            result = NativeExportsNE.FunctionPointer.InvokeWithBlittableArgument(&CallbackUnmanagedStdcall, a, b);
            Assert.Equal(expected, result);

            expected = Callback(b, a);
            result = NativeExportsNE.FunctionPointer.InvokeWithBlittableArgument(&Callback, b, a);
            Assert.Equal(expected, result);

            result = NativeExportsNE.FunctionPointer.InvokeWithBlittableArgument(&CallbackUnmanaged, b, a);
            Assert.Equal(expected, result);

            result = NativeExportsNE.FunctionPointer.InvokeWithBlittableArgument(&CallbackUnmanagedStdcall, b, a);
            Assert.Equal(expected, result);

            static int Callback(int a, int b)
            {
                // Use a noncommutative operation to validate passed in order.
                return a - b;
            }

            [UnmanagedCallersOnly]
            static int CallbackUnmanaged(int a, int b)
            {
                return Callback(a, b);
            }

            [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
            static int CallbackUnmanagedStdcall(int a, int b)
            {
                return Callback(a, b);
            }
        }
    }
}
