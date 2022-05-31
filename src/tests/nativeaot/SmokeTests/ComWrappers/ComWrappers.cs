// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace ComWrappersTests
{
    internal class Program
    {
        static ComWrappers GlobalComWrappers;

        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(IComInterface))]
        public static int Main(string[] args)
        {
            TestComInteropNullPointers();
            TestComInteropRegistrationRequired();
            GlobalComWrappers = new SimpleComWrapper();
            ComWrappers.RegisterForMarshalling(GlobalComWrappers);
            TestComInteropReleaseProcess();
            TestRCWRoundTripRequireUnwrap();
            TestRCWCached();
            TestRCWRoundTrip();

            TestComInteropCCWCreation();
            TestRCWNonRoundTripUnique();
            return 100;
        }

        public static void ThrowIfNotEquals<T>(T expected, T actual, string message)
        {
            if (!expected.Equals(actual))
            {
                message += "\nExpected: " + expected.ToString() + "\n";
                message += "Actual: " + actual.ToString() + "\n";
                throw new Exception(message);
            }
        }

        public static void ThrowIfNotEquals(bool expected, bool actual, string message)
        {
            ThrowIfNotEquals(expected ? 1 : 0, actual ? 1 : 0, message);
        }

        [DllImport("ComWrappersNative", CallingConvention = CallingConvention.StdCall)]
        static extern bool IsNULL(IComInterface foo);

        [DllImport("ComWrappersNative", CallingConvention = CallingConvention.StdCall)]
        static extern int CaptureComPointer(IComInterface foo);

        [DllImport("ComWrappersNative", CallingConvention = CallingConvention.StdCall)]
        static extern int RetrieveCapturedComPointer(out IComInterface foo);

        [DllImport("ComWrappersNative", EntryPoint="RetrieveCapturedComPointer", CallingConvention = CallingConvention.StdCall)]
        static extern int RetrieveCapturedComPointerRaw(out IntPtr foo);

        [DllImport("ComWrappersNative", CallingConvention = CallingConvention.StdCall)]
        static extern void ReleaseComPointer();

        [DllImport("ComWrappersNative", CallingConvention = CallingConvention.StdCall)]
        static extern int BuildComPointer(out IComInterface foo);

        [DllImport("ComWrappersNative", CallingConvention = CallingConvention.StdCall, PreserveSig = false, EntryPoint="BuildComPointer")]
        static extern IComInterface BuildComPointerNoPreserveSig();

        public static void TestComInteropNullPointers()
        {
            Console.WriteLine("Testing Marshal APIs for COM interfaces");
            IComInterface comPointer = null;
            var result = IsNULL(comPointer);
            ThrowIfNotEquals(true, IsNULL(comPointer), "COM interface marshalling null check failed");
        }

        public static void TestComInteropRegistrationRequired()
        {
            Console.WriteLine("Testing COM Interop registration process");
            ComObject target = new ComObject();
            try
            {
                CaptureComPointer(target);
                throw new Exception("Cannot work without ComWrappers.RegisterForMarshalling called");
            }
            catch (InvalidOperationException)
            {
            }
        }

        public static void TestComInteropReleaseProcess()
        {
            Console.WriteLine("Testing RCW release process");
            WeakReference comPointerHolder = CreateComReference();

            GC.Collect();
            ThrowIfNotEquals(true, comPointerHolder.IsAlive, ".NET object should be alive");

            ReleaseComPointer();

            GC.Collect();
            ThrowIfNotEquals(false, comPointerHolder.IsAlive, ".NET object should be disposed by then");
        }

        public static void TestRCWRoundTripRequireUnwrap()
        {
            Console.WriteLine("Testing RCW round-trip process");
            var target = new ComObject();
            int result = CaptureComPointer(target);
            ThrowIfNotEquals(0, result, "Seems to be COM marshalling behave strange.");
            result = RetrieveCapturedComPointerRaw(out var comPtr);
            var roundTripObject = GlobalComWrappers.GetOrCreateObjectForComInstance(comPtr, CreateObjectFlags.Unwrap);
            ThrowIfNotEquals(0, result, "Seems to be COM marshalling behave strange.");
            if (roundTripObject != target)
            {
                throw new Exception("RCW should round-trip");
            }
        }

        public static void TestRCWRoundTrip()
        {
            var target = new ComObject();
            int result = CaptureComPointer(target);
            ThrowIfNotEquals(0, result, "Seems to be COM marshalling behave strange.");
            result = RetrieveCapturedComPointer(out var capturedObject);
            ThrowIfNotEquals(0, result, "Seems to be COM marshalling behave strange.");
            if (capturedObject != target)
            {
                throw new Exception("Should round-trip");
            }
        }

        public static void TestRCWCached()
        {
            Console.WriteLine("Testing RCW cache process");
            ComWrappers wrapper = new SimpleComWrapper();
            var target = new ComObject();
            var comPtr = wrapper.GetOrCreateComInterfaceForObject(target, CreateComInterfaceFlags.None);
            var comPtr2 = wrapper.GetOrCreateComInterfaceForObject(target, CreateComInterfaceFlags.None);
            if (comPtr != comPtr2)
            {
                throw new Exception("RCW should round-trip");
            }
        }

        public static void TestRCWNonRoundTripUnique()
        {
            Console.WriteLine("Testing CCW uniqueness process");
            ComWrappers wrapper = new SimpleComWrapper();
            var target = new ComObject();
            var comPtr = wrapper.GetOrCreateComInterfaceForObject(target, CreateComInterfaceFlags.None);
            var ifPtr = wrapper.GetOrCreateObjectForComInstance(comPtr, CreateObjectFlags.UniqueInstance);
            if (ifPtr == target)
            {
                throw new Exception("RCW should not round-trip for unique instances");
            }
        }

        public static void TestComInteropCCWCreation()
        {
            Console.WriteLine("Testing CCW release process");
            int result = BuildComPointer(out var comPointer);
            ThrowIfNotEquals(0, result, "Seems to be COM marshalling behave strange.");
            comPointer.DoWork(11);

            comPointer = BuildComPointerNoPreserveSig();
            comPointer.DoWork(22);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference CreateComReference()
        {
            ComObject target = new ComObject();
            WeakReference comPointerHolder = new WeakReference(target);

            int result = CaptureComPointer(target);
            ThrowIfNotEquals(0, result, "Seems to be COM marshalling behave strange.");
            ThrowIfNotEquals(11, target.TestResult, "Call to method should work");

            return comPointerHolder;
        }
    }

    [ComImport]
    [ComVisible(true)]
    [Guid("111e91ef-1887-4afd-81e3-70cf08e715d8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IComInterface
    {
        int DoWork(int param);
    }

    public class ComObject : IComInterface
    {
        public int TestResult;
        public int DoWork(int param)
        {
            this.TestResult += param;
            return 0;
        }
    }

    class NativeComObjectWrapper: IComInterface
    {
        private IntPtr externalComObject;

        public NativeComObjectWrapper(IntPtr externalComObject) => this.externalComObject = externalComObject;

        public unsafe int DoWork(int param)
        {
            IntPtr* comDispatch = (IntPtr*)externalComObject;
            IntPtr* vtbl = (IntPtr*)comDispatch[0];
            return ((delegate* unmanaged<IntPtr, int, int>)vtbl[3])(externalComObject, param);
        }
    }

    internal unsafe class SimpleComWrapper : ComWrappers
    {
        static ComInterfaceEntry* wrapperEntry;

        static SimpleComWrapper()
        {
            IntPtr* vtbl = (IntPtr*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(IComInterface), 4 * sizeof(IntPtr));
            GetIUnknownImpl(out vtbl[0], out vtbl[1], out vtbl[2]);
            vtbl[3] = (IntPtr)(delegate* unmanaged<IntPtr, int, int>)&IComInterfaceProxy.DoWork;

            var comInterfaceEntryMemory = RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(IComInterface), sizeof(ComInterfaceEntry));
            wrapperEntry = (ComInterfaceEntry*)comInterfaceEntryMemory;
            wrapperEntry->IID = new Guid("111e91ef-1887-4afd-81e3-70cf08e715d8");
            wrapperEntry->Vtable = (IntPtr)vtbl;
        }

        protected override unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
        {
            if (obj is not IComInterface)
                throw new Exception();
            count = 1;
            return wrapperEntry;
        }

        protected override object CreateObject(IntPtr externalComObject, CreateObjectFlags flags)
        {
            return new NativeComObjectWrapper(externalComObject);
        }

        protected override void ReleaseObjects(System.Collections.IEnumerable objects)
        {
        }
    }

    internal unsafe class IComInterfaceProxy
    {
        [UnmanagedCallersOnly]
        public static int DoWork(IntPtr thisPtr, int param)
        {
            var inst = ComWrappers.ComInterfaceDispatch.GetInstance<IComInterface>((ComWrappers.ComInterfaceDispatch*)thisPtr);
            return inst.DoWork(param);
        }
    }
}
