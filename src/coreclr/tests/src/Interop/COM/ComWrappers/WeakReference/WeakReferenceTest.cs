// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace ComWrappersTests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using TestLibrary;

    static class WeakReferenceNative
    {
        [DllImport(nameof(WeakReferenceNative))]
        public static extern IntPtr CreateWeakReferencableObject();
    }

    public struct VtblPtr
    {
        public IntPtr Vtbl;
    }

    public class WeakReferencableWrapper
    {
        private struct Vtbl
        {
            public IntPtr QueryInterface;
            public _AddRef AddRef;
            public _Release Release;
        }

        private delegate int _AddRef(IntPtr This);
        private delegate int _Release(IntPtr This);

        private readonly IntPtr instance;
        private readonly Vtbl vtable;

        public WeakReferencableWrapper(IntPtr instance)
        {
            var inst = Marshal.PtrToStructure<VtblPtr>(instance);
            this.vtable = Marshal.PtrToStructure<Vtbl>(inst.Vtbl);
            this.instance = instance;
        }

        ~WeakReferencableWrapper()
        {
            if (this.instance != IntPtr.Zero)
            {
                this.vtable.Release(this.instance);
            }
        }
    }

    class Program
    {
        class TestComWrappers : ComWrappers
        {
            protected unsafe override ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
            {
                count = 0;
                return null;
            }

            protected override object CreateObject(IntPtr externalComObject, CreateObjectFlags flag)
            {
                return new WeakReferencableWrapper(externalComObject);
            }

            protected override void ReleaseObjects(IEnumerable objects)
            {
            }

            public static readonly ComWrappers Instance = new TestComWrappers();
        }

        static void ValidateNativeWeakReference()
        {
            Console.WriteLine($"Running {nameof(ValidateNativeWeakReference)}...");

            static (WeakReference<WeakReferencableWrapper>, IntPtr) GetWeakReference()
            {
                var cw = new TestComWrappers();

                IntPtr objRaw = WeakReferenceNative.CreateWeakReferencableObject();

                var obj = (WeakReferencableWrapper)cw.GetOrCreateObjectForComInstance(objRaw, CreateObjectFlags.None);

                // The returned WeakReferencableWrapper from ComWrappers takes ownership
                // of the ref returned from CreateWeakReferencableObject.
                // Call Marshal.AddRef to ensure that objRaw owns a reference.
                Marshal.AddRef(objRaw);

                return (new WeakReference<WeakReferencableWrapper>(obj), objRaw);
            }

            static bool CheckIfWeakReferenceIsAlive(WeakReference<WeakReferencableWrapper> wr)
            {
                return wr.TryGetTarget(out _);
            }

            var (weakRef, nativeRef) = GetWeakReference();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            // A weak reference to an RCW wrapping an IWeakReference should stay alive even after the RCW dies
            Assert.IsTrue(CheckIfWeakReferenceIsAlive(weakRef));

            // Release the last native reference.
            Marshal.Release(nativeRef);

            GC.Collect();
            GC.WaitForPendingFinalizers();

            // After all native references die and the RCW is collected, the weak reference should be dead and stay dead.
            Assert.IsFalse(CheckIfWeakReferenceIsAlive(weakRef));

        }

        static int Main(string[] doNotUse)
        {
            try
            {
                ComWrappers.RegisterForTrackerSupport(TestComWrappers.Instance);
                ValidateNativeWeakReference();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Test Failure: {e}");
                return 101;
            }

            return 100;
        }
    }
}

