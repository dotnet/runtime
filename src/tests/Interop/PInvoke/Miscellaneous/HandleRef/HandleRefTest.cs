// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System;
using System.Reflection;
using System.Text;
using Xunit;

class HandleRefTest
{
    [DllImport(@"HandleRefNative", CallingConvention = CallingConvention.Winapi)]
    private static extern int MarshalPointer_In(HandleRef pintValue, int stackGuard);

    [DllImport(@"HandleRefNative", CallingConvention = CallingConvention.Winapi)]
    private static extern int MarshalPointer_InOut(HandleRef pintValue, int stackGuard);

    [DllImport(@"HandleRefNative", CallingConvention = CallingConvention.Winapi)]
    private static extern int MarshalPointer_Out(HandleRef pintValue, int stackGuard);

    [DllImport(@"HandleRefNative", CallingConvention = CallingConvention.Winapi)]
    private static extern int TestNoGC(HandleRef pintValue, Action gcCallback);

    [DllImport(@"HandleRefNative")]
    private static extern HandleRef InvalidMarshalPointer_Return();

    public unsafe static int Main(string[] args)
    {
        try{
            const int intManaged = 1000;
            const int intNative = 2000;
            const int intReturn = 3000;
            const int stackGuard = 5000;

            Console.WriteLine("MarshalPointer_In");
            int int1 = intManaged;
            int* int1Ptr = &int1;
            HandleRef hr1 = new HandleRef(new Object(), (IntPtr)int1Ptr);
            Assert.Equal(intReturn, MarshalPointer_In(hr1, stackGuard));
            Assert.Equal(intManaged, int1);

            Console.WriteLine("MarshalPointer_InOut");
            int int2 = intManaged;
            int* int2Ptr = &int2;
            HandleRef hr2 = new HandleRef(new Object(), (IntPtr)int2Ptr);
            Assert.Equal(intReturn, MarshalPointer_InOut(hr2, stackGuard));
            Assert.Equal(intNative, int2);

            Console.WriteLine("MarshalPointer_Out");
            int int3 = intManaged;
            int* int3Ptr = &int3;
            HandleRef hr3 = new HandleRef(new Object(), (IntPtr)int3Ptr);
            Assert.Equal(intReturn, MarshalPointer_Out(hr3, stackGuard));
            Assert.Equal(intNative, int3);

            // Note that this scenario will always pass in a debug build because all values
            // stay rooted until the end of the method.
            Console.WriteLine("TestNoGC");

            int* int4Ptr = (int*)Marshal.AllocHGlobal(sizeof(int)); // We don't free this memory so we don't have to worry about a GC run between freeing and return (possible in a GCStress mode).
            Console.WriteLine("2");
            *int4Ptr = intManaged;
            CollectableClass collectableClass = new CollectableClass(int4Ptr);
            HandleRef hr4 = new HandleRef(collectableClass, (IntPtr)int4Ptr);
            Action gcCallback = () => { Console.WriteLine("GC callback now"); GC.Collect(2, GCCollectionMode.Forced); GC.WaitForPendingFinalizers(); GC.Collect(2, GCCollectionMode.Forced); };
            Assert.Equal(intReturn, TestNoGC(hr4, gcCallback));
            Console.WriteLine("Native code finished");

            Console.WriteLine("InvalidMarshalPointer_Return");
            Assert.Throws<MarshalDirectiveException>(() => InvalidMarshalPointer_Return());

            return 100;
        } catch (Exception e){
            Console.WriteLine($"Test Failure: {e}");
            return 101;
        }
    }

    /// <summary>
    /// Class that will change a pointer passed to native code when this class gets finalized.
    /// Native code can check whether the pointer changed during a P/Invoke
    /// </summary>
    unsafe class CollectableClass
    {
        int* PtrToChange;
        public CollectableClass(int* ptrToChange)
        {
            PtrToChange = ptrToChange;
        }

        ~CollectableClass()
        {
            Console.WriteLine("CollectableClass collected");
            *PtrToChange = Int32.MaxValue;
        }
    }
}
