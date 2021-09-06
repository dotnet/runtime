// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

    public enum WrapperRegistration
    {
        Local,
        TrackerSupport,
        Marshalling,
    }

    public class WeakReferenceableWrapper
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

        public WrapperRegistration Registration { get; }

        public WeakReferenceableWrapper(IntPtr instance, WrapperRegistration reg)
        {
            var inst = Marshal.PtrToStructure<VtblPtr>(instance);
            this.vtable = Marshal.PtrToStructure<Vtbl>(inst.Vtbl);
            this.instance = instance;
            Registration = reg;
        }

        ~WeakReferenceableWrapper()
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
            public WrapperRegistration Registration { get; }

            public TestComWrappers(WrapperRegistration reg = WrapperRegistration.Local)
            {
                Registration = reg;
            }

            protected unsafe override ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
            {
                count = 0;
                return null;
            }

            protected override object CreateObject(IntPtr externalComObject, CreateObjectFlags flag)
            {
                Marshal.AddRef(externalComObject);
                return new WeakReferenceableWrapper(externalComObject, Registration);
            }

            protected override void ReleaseObjects(IEnumerable objects)
            {
            }

            public static readonly TestComWrappers TrackerSupportInstance = new TestComWrappers(WrapperRegistration.TrackerSupport);
            public static readonly TestComWrappers MarshallingInstance = new TestComWrappers(WrapperRegistration.Marshalling);
        }

        private static void ValidateWeakReferenceState(WeakReference<WeakReferenceableWrapper> wr, bool expectedIsAlive, TestComWrappers sourceWrappers = null)
        {
            WeakReferenceableWrapper target;
            bool isAlive = wr.TryGetTarget(out target);
            Assert.AreEqual(expectedIsAlive, isAlive);

            if (isAlive && sourceWrappers != null)
                Assert.AreEqual(sourceWrappers.Registration, target.Registration);
        }

        private static (WeakReference<WeakReferenceableWrapper>, IntPtr) GetWeakReference(TestComWrappers cw)
        {
            IntPtr objRaw = WeakReferenceNative.CreateWeakReferencableObject();
            var obj = (WeakReferenceableWrapper)cw.GetOrCreateObjectForComInstance(objRaw, CreateObjectFlags.None);
            var wr = new WeakReference<WeakReferenceableWrapper>(obj);
            ValidateWeakReferenceState(wr, expectedIsAlive: true, cw);
            return (wr, objRaw);
        }

        private static IntPtr SetWeakReferenceTarget(WeakReference<WeakReferenceableWrapper> wr, TestComWrappers cw)
        {
            IntPtr objRaw = WeakReferenceNative.CreateWeakReferencableObject();
            var obj = (WeakReferenceableWrapper)cw.GetOrCreateObjectForComInstance(objRaw, CreateObjectFlags.None);
            wr.SetTarget(obj);
            ValidateWeakReferenceState(wr, expectedIsAlive: true, cw);
            return objRaw;
        }

        private static void ValidateNativeWeakReference(TestComWrappers cw)
        {
            Console.WriteLine($"  -- Validate weak reference creation");
            var (weakRef, nativeRef) = GetWeakReference(cw);

            // Make sure RCW is collected
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // Non-globally registered ComWrappers instances do not support rehydration.
            // A weak reference to an RCW wrapping an IWeakReference can stay alive if the RCW was created through
            // a global ComWrappers instance. If the RCW was created throug a local ComWrappers instance, the weak
            // reference should be dead and stay dead once the RCW is collected.
            bool supportsRehydration = cw.Registration != WrapperRegistration.Local;
            
            Console.WriteLine($"    -- Validate RCW recreation");
            ValidateWeakReferenceState(weakRef, expectedIsAlive: supportsRehydration, cw);

            // Release the last native reference.
            Marshal.Release(nativeRef);
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // After all native references die and the RCW is collected, the weak reference should be dead and stay dead.
            Console.WriteLine($"    -- Validate release");
            ValidateWeakReferenceState(weakRef, expectedIsAlive: false);

            // Reset the weak reference target
            Console.WriteLine($"  -- Validate target reset");
            nativeRef = SetWeakReferenceTarget(weakRef, cw);

            // Make sure RCW is collected
            GC.Collect();
            GC.WaitForPendingFinalizers();

            Console.WriteLine($"    -- Validate RCW recreation");
            ValidateWeakReferenceState(weakRef, expectedIsAlive: supportsRehydration, cw);

            // Release the last native reference.
            Marshal.Release(nativeRef);
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // After all native references die and the RCW is collected, the weak reference should be dead and stay dead.
            Console.WriteLine($"    -- Validate release");
            ValidateWeakReferenceState(weakRef, expectedIsAlive: false);
        }

        static void ValidateGlobalInstanceTrackerSupport()
        {
            Console.WriteLine($"Running {nameof(ValidateGlobalInstanceTrackerSupport)}...");
            ValidateNativeWeakReference(TestComWrappers.TrackerSupportInstance);
        }

        static void ValidateGlobalInstanceMarshalling()
        {
            Console.WriteLine($"Running {nameof(ValidateGlobalInstanceMarshalling)}...");
            ValidateNativeWeakReference(TestComWrappers.MarshallingInstance);
        }

        static void ValidateLocalInstance()
        {
            Console.WriteLine($"Running {nameof(ValidateLocalInstance)}...");
            ValidateNativeWeakReference(new TestComWrappers());
        }

        static void ValidateNonComWrappers()
        {
            Console.WriteLine($"Running {nameof(ValidateNonComWrappers)}...");

            (WeakReference, IntPtr) GetWeakReference()
            {
                IntPtr objRaw = WeakReferenceNative.CreateWeakReferencableObject();
                var obj = Marshal.GetObjectForIUnknown(objRaw);
                return (new WeakReference(obj), objRaw);
            }

            bool HasTarget(WeakReference wr)
            {
                return wr.Target != null;
            }

            var (weakRef, nativeRef) = GetWeakReference();
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // A weak reference to an RCW wrapping an IWeakReference created throguh the built-in system
            // should stay alive even after the RCW dies
            Assert.IsFalse(weakRef.IsAlive);
            Assert.IsTrue(HasTarget(weakRef));

            // Release the last native reference.
            Marshal.Release(nativeRef);
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // After all native references die and the RCW is collected, the weak reference should be dead and stay dead.
            Assert.IsNull(weakRef.Target);
        }

        static int Main(string[] doNotUse)
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    ValidateNonComWrappers();

                    ComWrappers.RegisterForMarshalling(TestComWrappers.MarshallingInstance);
                    ValidateGlobalInstanceMarshalling();
                }

                ComWrappers.RegisterForTrackerSupport(TestComWrappers.TrackerSupportInstance);
                ValidateGlobalInstanceTrackerSupport();

                ValidateLocalInstance();
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

