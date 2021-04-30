// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ObjectiveCMarshalAPI
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Runtime.InteropServices.ObjectiveC;

    using TestLibrary;

    class NativeObjCMarshalTests
    {
        [DllImport(nameof(NativeObjCMarshalTests))]
        public static extern unsafe void GetExports(
            out delegate* unmanaged<void> beginEndCallback,
            out delegate* unmanaged<IntPtr, int> isReferencedCallback,
            out delegate* unmanaged<IntPtr, void> trackedObjectEnteredFinalization);

        [DllImport(nameof(NativeObjCMarshalTests))]
        public static extern int CallAndCatch(IntPtr fptr, int a);

        [DllImport(nameof(NativeObjCMarshalTests))]
        public static extern IntPtr GetThrowInt();

        [DllImport(nameof(NativeObjCMarshalTests))]
        public static extern IntPtr GetThrowException();
    }

    unsafe class Program
    {
        static void Validate_ReferenceTrackingAPIs_InvalidArgs()
        {
            Console.WriteLine($"Running {nameof(Validate_ReferenceTrackingAPIs_InvalidArgs)}...");

            delegate* unmanaged<void> beginEndCallback;
            delegate* unmanaged<IntPtr, int> isReferencedCallback;
            delegate* unmanaged<IntPtr, void> trackedObjectEnteredFinalization;
            NativeObjCMarshalTests.GetExports(out beginEndCallback, out isReferencedCallback, out trackedObjectEnteredFinalization);

            Assert.Throws<ArgumentNullException>(
                () =>
                {
                    ObjectiveCMarshal.Initialize(null, isReferencedCallback, trackedObjectEnteredFinalization, OnUnhandledExceptionPropagationHandler);
                });
            Assert.Throws<ArgumentNullException>(
                () =>
                {
                    ObjectiveCMarshal.Initialize(beginEndCallback, null, trackedObjectEnteredFinalization, OnUnhandledExceptionPropagationHandler);
                });
            Assert.Throws<ArgumentNullException>(
                () =>
                {
                    ObjectiveCMarshal.Initialize(beginEndCallback, isReferencedCallback, null, OnUnhandledExceptionPropagationHandler);
                });
            Assert.Throws<ArgumentNullException>(
                () =>
                {
                    ObjectiveCMarshal.Initialize(beginEndCallback, isReferencedCallback, trackedObjectEnteredFinalization, null);
                });
            Assert.Throws<ArgumentNullException>(
                () =>
                {
                    ObjectiveCMarshal.CreateReferenceTrackingHandle(null , out _);
                });
        }

        // The expectation here is during reference tracking handle creation
        // the RefCountDown will be set to some non-negative number and RefCountUp
        // will remain zero. The values will be incremented and decremented respectively
        // during the "is referenced" callback. When the object enters the finalizer queue
        // the RefCountDown will then be set to nuint.MaxValue. In the object's finalizer
        // the RefCountUp can be checked to ensure the count down value was respected.
        struct Contract
        {
            public nuint RefCountDown;
            public nuint RefCountUp;
        };

        [ObjectiveCTrackedTypeAttribute]
        class Base
        {
            public static int AllocCount = 0;
            public static int FinalizeCount = 0;

            private nuint _expectedCount = 0;
            private Contract* _contract;

            public Base()
            {
                AllocCount++;
            }

            ~Base()
            {
                if (_contract != null)
                {
                    Assert.AreEqual(nuint.MaxValue, _contract->RefCountDown);  // Validate finalizer queue callback
                    Assert.AreEqual(_expectedCount, _contract->RefCountUp);    // Validate "is referenced" callback
                }

                FinalizeCount++;
            }

            public IntPtr Contract { get => (IntPtr)_contract; }

            public void SetContractMemory(IntPtr mem, uint count)
            {
                _contract = (Contract*)mem;

                // Contract should be 0 initialized when supplied.
                Assert.AreEqual((nuint)0, _contract->RefCountDown);
                Assert.AreEqual((nuint)0, _contract->RefCountUp);

                _expectedCount = (nuint)count;
                _contract->RefCountDown = _expectedCount;
            }
        }

        class Derived : Base { }

        class DerivedWithFinalizer : Base
        {
            ~DerivedWithFinalizer() { }
        }

        [ObjectiveCTrackedTypeAttribute]
        class AttributedNoFinalizer { }

        static void InitializeObjectiveCMarshal()
        {
            delegate* unmanaged<void> beginEndCallback;
            delegate* unmanaged<IntPtr, int> isReferencedCallback;
            delegate* unmanaged<IntPtr, void> trackedObjectEnteredFinalization;
            NativeObjCMarshalTests.GetExports(out beginEndCallback, out isReferencedCallback, out trackedObjectEnteredFinalization);

            ObjectiveCMarshal.Initialize(beginEndCallback, isReferencedCallback, trackedObjectEnteredFinalization, OnUnhandledExceptionPropagationHandler);
        }

        static GCHandle AllocAndTrackObject<T>() where T : Base, new()
        {
            var obj = new T();
            GCHandle h = ObjectiveCMarshal.CreateReferenceTrackingHandle(obj, out Span<IntPtr> s);

            // Validate contract length for tagged memory.
            Assert.AreEqual(2, s.Length);

            // Make the "is referenced" callback run a few times.
            fixed (void* p = s)
                obj.SetContractMemory((IntPtr)p, count: 3);
            return h;
        }

        static void Validate_AllocAndFreeAnotherHandle<T>(GCHandle handle) where T : Base, new()
        {
            var obj = (T)handle.Target;
            GCHandle h = ObjectiveCMarshal.CreateReferenceTrackingHandle(obj, out Span<IntPtr> s);

            // Validate the memory is the same but the GCHandles are distinct.
            fixed (void* p = s)
                Assert.AreEqual(obj.Contract, new IntPtr(p));

            Assert.AreNotEqual(handle, h);
            h.Free();
        }

        static unsafe void Validate_ReferenceTracking_Scenario()
        {
            Console.WriteLine($"Running {nameof(Validate_ReferenceTracking_Scenario)}...");

            var handles = new List<GCHandle>();

            // Attempting to create handle prior to initialization.
            Assert.Throws<InvalidOperationException>(
                () =>
                {
                    ObjectiveCMarshal.CreateReferenceTrackingHandle(new Base(), out _);
                });

            InitializeObjectiveCMarshal();

            // Type attributed but no finalizer.
            Assert.Throws<InvalidOperationException>(
                () =>
                {
                    ObjectiveCMarshal.CreateReferenceTrackingHandle(new AttributedNoFinalizer(), out _);
                });

            {
                GCHandle h = AllocAndTrackObject<Base>();
                handles.Add(h);
                Validate_AllocAndFreeAnotherHandle<Base>(h);
            }
            {
                GCHandle h = AllocAndTrackObject<Derived>();
                handles.Add(h);
                Validate_AllocAndFreeAnotherHandle<Derived>(h);
            }
            {
                GCHandle h = AllocAndTrackObject<DerivedWithFinalizer>();
                handles.Add(h);
                Validate_AllocAndFreeAnotherHandle<DerivedWithFinalizer>(h);
            }

            // Trigger the GC
            for (int i = 0; i < 5; ++i)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            // Validate we finalized all the objects we allocated.
            // It is important to validate the count prior to freeing
            // the handles to verify they are not keeping objects alive.
            Assert.AreEqual(Base.FinalizeCount, Base.AllocCount);

            // Clean up all allocated handles that are no longer needed.
            foreach (var h in handles)
            {
                h.Free();
            }

            // Validate the exception propagation logic.
            _Validate_ExceptionPropagation();
        }

        private class IntException : Exception
        {
            public int Value { get; }
            public IntException(int value) { this.Value = value; }
        }

        private class ExceptionException : Exception
        {
            public ExceptionException() {}
        }

        [UnmanagedCallersOnly]
        static void UCO_ThrowIntException(int a) => throw new IntException(a);
        [UnmanagedCallersOnly]
        static void UCO_ThrowExceptionException(int _) => throw new ExceptionException();

        delegate void ThrowExceptionDelegate(int a);
        static void DEL_ThrowIntException(int a) => throw new IntException(a);
        static void DEL_ThrowExceptionException(int _) => throw new ExceptionException();

        static unsafe delegate* unmanaged<IntPtr, void> OnUnhandledExceptionPropagationHandler(
            Exception e,
            RuntimeMethodHandle lastMethodHandle,
            out IntPtr context)
        {
            var lastMethod = (MethodInfo)MethodBase.GetMethodFromHandle(lastMethodHandle);
            Assert.IsTrue(lastMethod != null);

            context = IntPtr.Zero;
            if (e is IntException ie)
            {
                context = new IntPtr(ie.Value);
                return (delegate* unmanaged<IntPtr, void>)NativeObjCMarshalTests.GetThrowInt();
            }
            else if (e is ExceptionException)
            {
                return (delegate* unmanaged<IntPtr, void>)NativeObjCMarshalTests.GetThrowException();
            }

            Assert.Fail("Unknown exception type");
            throw new Exception("Unreachable");
        }

        class Scenario
        {
            public Scenario(delegate* unmanaged<int, void> fptr, int expected) { Fptr = fptr; Expected = expected; }
            public delegate* unmanaged<int, void> Fptr;
            public int Expected;
        }

        // Do not call this method from Main as it depends on a previous test for set up.
        static void _Validate_ExceptionPropagation()
        {
            var scenarios = new[]
            {
                new Scenario((delegate* unmanaged<int, void>)&UCO_ThrowIntException, 3423),
                new Scenario((delegate* unmanaged<int, void>)&UCO_ThrowExceptionException, 5432),
                new Scenario((delegate* unmanaged<int, void>)Marshal.GetFunctionPointerForDelegate<ThrowExceptionDelegate>(DEL_ThrowIntException), 6453),
                new Scenario((delegate* unmanaged<int, void>)Marshal.GetFunctionPointerForDelegate<ThrowExceptionDelegate>(DEL_ThrowExceptionException), 5343)
            };

            foreach (var scen in scenarios)
            {
                delegate* unmanaged<int, void> testNativeMethod = scen.Fptr;
                int ret = NativeObjCMarshalTests.CallAndCatch((IntPtr)testNativeMethod, scen.Expected);
                Assert.AreEqual(scen.Expected, ret);
            }
        }

        static void Validate_Initialize_FailsOnSecondAttempt()
        {
            Console.WriteLine($"Running {nameof(Validate_Initialize_FailsOnSecondAttempt)}...");
            
            Assert.Throws<InvalidOperationException>(
                () =>
                {
                    InitializeObjectiveCMarshal();
                });
        }

        static int Main(string[] doNotUse)
        {
            try
            {
                Validate_ReferenceTrackingAPIs_InvalidArgs();
                Validate_ReferenceTracking_Scenario();
                Validate_Initialize_FailsOnSecondAttempt();
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