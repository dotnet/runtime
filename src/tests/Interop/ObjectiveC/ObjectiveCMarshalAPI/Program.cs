// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
namespace ObjectiveCMarshalAPI
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.InteropServices.ObjectiveC;
    using System.Threading;

    using Xunit;

    class NativeObjCMarshalTests
    {
        [DllImport(nameof(NativeObjCMarshalTests))]
        public static extern unsafe void GetExports(
            out delegate* unmanaged<void> beginEndCallback,
            out delegate* unmanaged<IntPtr, int> isReferencedCallback,
            out delegate* unmanaged<IntPtr, void> trackedObjectEnteredFinalization);

        [DllImport(nameof(NativeObjCMarshalTests))]
        public static extern unsafe void SetImports(
            delegate* unmanaged<void> beforeThrowNativeExceptionCallback);

        [DllImport(nameof(NativeObjCMarshalTests))]
        public static extern int CallAndCatch(IntPtr fptr, int a);

        [DllImport(nameof(NativeObjCMarshalTests))]
        public static extern IntPtr GetThrowInt();

        [DllImport(nameof(NativeObjCMarshalTests))]
        public static extern IntPtr GetThrowException();
    }

    public unsafe class Program
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
        // the RefCountDown will then be set to nuint.MaxValue, see the EnteredFinalizerCb
        // callback. In the object's finalizer the RefCountUp can be checked to ensure
        // the count down value was respected.
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
                    Assert.Equal(nuint.MaxValue, _contract->RefCountDown);  // Validate finalizer queue callback
                    Assert.Equal(_expectedCount, _contract->RefCountUp);    // Validate "is referenced" callback
                }

                FinalizeCount++;
            }

            public IntPtr Contract { get => (IntPtr)_contract; }

            public void SetContractMemory(IntPtr mem, uint count)
            {
                _contract = (Contract*)mem;

                // Contract should be 0 initialized when supplied.
                Assert.Equal((nuint)0, _contract->RefCountDown);
                Assert.Equal((nuint)0, _contract->RefCountUp);

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

        class HasNoHashCode : Base
        {
        }

        class HasHashCode : Base
        {
            public HasHashCode()
            {
                // this will write a hash code into the object header.
                RuntimeHelpers.GetHashCode(this);
            }
        }

        class HasThinLockHeld : Base
        {
            public HasThinLockHeld()
            {
                // This will write lock information into the object header.
                // An attempt to generate a hash code for this object will cause the lock to be
                // upgrade to a thick lock.
                Monitor.Enter(this);
            }
        }

        class HasSyncBlock : Base
        {
            public HasSyncBlock()
            {
                RuntimeHelpers.GetHashCode(this);
                Monitor.Enter(this);
            }
        }

        static void InitializeObjectiveCMarshal()
        {
            delegate* unmanaged<void> beginEndCallback;
            delegate* unmanaged<IntPtr, int> isReferencedCallback;
            delegate* unmanaged<IntPtr, void> trackedObjectEnteredFinalization;
            NativeObjCMarshalTests.GetExports(out beginEndCallback, out isReferencedCallback, out trackedObjectEnteredFinalization);

            delegate* unmanaged<void> beforeThrow = &BeforeThrowNativeException;
            NativeObjCMarshalTests.SetImports(beforeThrow);

            ObjectiveCMarshal.Initialize(beginEndCallback, isReferencedCallback, trackedObjectEnteredFinalization, OnUnhandledExceptionPropagationHandler);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static GCHandle AllocAndTrackObject<T>(uint count) where T : Base, new()
        {
            var obj = new T();
            GCHandle h = ObjectiveCMarshal.CreateReferenceTrackingHandle(obj, out Span<IntPtr> s);

            // Validate contract length for tagged memory.
            Assert.Equal(2, s.Length);

            // Make the "is referenced" callback run at least 'count' number of times.
            fixed (void* p = s)
                obj.SetContractMemory((IntPtr)p, count);
            return h;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Validate_AllocAndFreeAnotherHandle<T>(GCHandle handle) where T : Base, new()
        {
            var obj = (T)handle.Target;
            GCHandle h = ObjectiveCMarshal.CreateReferenceTrackingHandle(obj, out Span<IntPtr> s);

            // Validate the memory is the same but the GCHandles are distinct.
            fixed (void* p = s)
                Assert.Equal(obj.Contract, new IntPtr(p));

            Assert.NotEqual(handle, h);
            h.Free();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void AllocUntrackedObject<T>() where T : Base, new()
        {
            new T();
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

            // Ensure objects who have no tagged memory allocated are handled when they enter the
            // finalization queue. The NativeAOT implementation looks up objects in a hash table,
            // so we exercise the various ways a hash code can be stored.
            AllocUntrackedObject<HasNoHashCode>();
            AllocUntrackedObject<HasHashCode>();
            AllocUntrackedObject<HasThinLockHeld>();
            AllocUntrackedObject<HasSyncBlock>();

            // Provide the minimum number of times the reference callback should run.
            // See IsRefCb() in NativeObjCMarshalTests.cpp for usage logic.
            const uint callbackCount = 3;
            {
                GCHandle h = AllocAndTrackObject<Base>(callbackCount);
                handles.Add(h);
                Validate_AllocAndFreeAnotherHandle<Base>(h);
            }
            {
                GCHandle h = AllocAndTrackObject<Derived>(callbackCount);
                handles.Add(h);
                Validate_AllocAndFreeAnotherHandle<Derived>(h);
            }
            {
                GCHandle h = AllocAndTrackObject<DerivedWithFinalizer>(callbackCount);
                handles.Add(h);
                Validate_AllocAndFreeAnotherHandle<DerivedWithFinalizer>(h);
            }

            // Trigger the GC
            for (int i = 0; i < (callbackCount + 2); ++i)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            // Validate we finalized all the objects we allocated.
            // It is important to validate the count prior to freeing
            // the handles to verify they are not keeping objects alive.
            Assert.Equal(Base.FinalizeCount, Base.AllocCount);

            // Clean up all allocated handles that are no longer needed.
            foreach (var h in handles)
            {
                h.Free();
            }

            // Validate the exception propagation logic.
            _Validate_ExceptionPropagation();
        }

        [UnmanagedCallersOnly]
        private static void BeforeThrowNativeException()
        {
            // This function is called from the exception propagation callback.
            // It ensures that the thread was transitioned to preemptive mode.
            GC.Collect();
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

        static bool s_finallyExecuted;

        [UnmanagedCallersOnly]
        static void UCO_ThrowIntException(int a)
        {
            try
            {
                throw new IntException(a);
            }
            finally
            {
                s_finallyExecuted = true;
            }
        }

        [UnmanagedCallersOnly]
        static void UCO_ThrowExceptionException(int _)
        {
            try
            {
                throw new ExceptionException();
            }
            finally
            {
                s_finallyExecuted = true;
            }
        }

        delegate void ThrowExceptionDelegate(int a);

        static void DEL_ThrowIntException(int a)
        {
            try
            {
                throw new IntException(a);
            }
            finally
            {
                s_finallyExecuted = true;
            }
        }

        static void DEL_ThrowExceptionException(int _)
        {
            try
            {
                throw new ExceptionException();
            }
            finally
            {
                s_finallyExecuted = true;
            }
        }

        static unsafe delegate* unmanaged<IntPtr, void> OnUnhandledExceptionPropagationHandler(
            Exception e,
            RuntimeMethodHandle lastMethodHandle,
            out IntPtr context)
        {
            // Not yet implemented For NativeAOT.
            // https://github.com/dotnet/runtime/issues/80985
            if (!TestLibrary.Utilities.IsNativeAot)
            {
                var lastMethod = (MethodInfo)MethodBase.GetMethodFromHandle(lastMethodHandle);
                Assert.True(lastMethod != null);
            }

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
            throw new UnreachableException();
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
            Console.WriteLine($"Running {nameof(_Validate_ExceptionPropagation)}");

            var delThrowInt = new ThrowExceptionDelegate(DEL_ThrowIntException);
            var delThrowException = new ThrowExceptionDelegate(DEL_ThrowExceptionException);
            var scenarios = new[]
            {
                new Scenario((delegate* unmanaged<int, void>)&UCO_ThrowIntException, 3423),
                new Scenario((delegate* unmanaged<int, void>)&UCO_ThrowExceptionException, 5432),
                new Scenario((delegate* unmanaged<int, void>)Marshal.GetFunctionPointerForDelegate(delThrowInt), 6453),
                new Scenario((delegate* unmanaged<int, void>)Marshal.GetFunctionPointerForDelegate(delThrowException), 5343)
            };

            foreach (var scen in scenarios)
            {
                s_finallyExecuted = false;
                delegate* unmanaged<int, void> testNativeMethod = scen.Fptr;
                int ret = NativeObjCMarshalTests.CallAndCatch((IntPtr)testNativeMethod, scen.Expected);
                Assert.Equal(scen.Expected, ret);
                Assert.True(s_finallyExecuted, "Finally block not executed.");
            }

            GC.KeepAlive(delThrowInt);
            GC.KeepAlive(delThrowException);
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

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
        public static int TestEntryPoint()
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
