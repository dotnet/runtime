// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ComWrappersTests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    using ComWrappersTests.Common;
    using TestLibrary;

    class Program
    {
        class TestComWrappers : ComWrappers
        {
            protected unsafe override ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
            {
                IntPtr fpQueryInteface = default;
                IntPtr fpAddRef = default;
                IntPtr fpRelease = default;
                ComWrappers.GetIUnknownImpl(out fpQueryInteface, out fpAddRef, out fpRelease);

                ComInterfaceEntry* entryRaw = null;
                count = 0;
                if (obj is Test)
                {
                    var vtbl = new ITestVtbl()
                    {
                        IUnknownImpl = new IUnknownVtbl()
                        {
                            QueryInterface = fpQueryInteface,
                            AddRef = fpAddRef,
                            Release = fpRelease
                        },
                        SetValue = Marshal.GetFunctionPointerForDelegate(ITestVtbl.pSetValue)
                    };
                    var vtblRaw = RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(ITestVtbl), sizeof(ITestVtbl));
                    Marshal.StructureToPtr(vtbl, vtblRaw, false);

                    entryRaw = (ComInterfaceEntry*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(ITestVtbl), sizeof(ComInterfaceEntry));
                    entryRaw->IID = typeof(ITest).GUID;
                    entryRaw->Vtable = vtblRaw;
                    count = 1;
                }

                return entryRaw;
            }

            protected override object CreateObject(IntPtr externalComObject, CreateObjectFlags flag)
            {
                var iid = typeof(ITrackerObject).GUID;
                IntPtr iTrackerComObject;
                int hr = Marshal.QueryInterface(externalComObject, ref iid, out iTrackerComObject);
                Assert.AreEqual(0, hr);

                return new ITrackerObjectWrapper(iTrackerComObject);
            }

            public const int ReleaseObjectsCallAck = unchecked((int)-1);

            protected override void ReleaseObjects(IEnumerable objects)
            {
                throw new Exception() { HResult = ReleaseObjectsCallAck };
            }

            public static void ValidateIUnknownImpls()
            {
                Console.WriteLine($"Running {nameof(ValidateIUnknownImpls)}...");

                ComWrappers.GetIUnknownImpl(out IntPtr fpQueryInteface, out IntPtr fpAddRef, out IntPtr fpRelease);

                Assert.AreNotEqual(fpQueryInteface, IntPtr.Zero);
                Assert.AreNotEqual(fpAddRef, IntPtr.Zero);
                Assert.AreNotEqual(fpRelease, IntPtr.Zero);
            }
        }

        static void ForceGC()
        {
            // Trigger the GC multiple times and then
            // wait for all finalizers since that is where
            // most of the cleanup occurs.
            for (int i = 0; i < 5; ++i)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        static void ValidateComInterfaceCreation()
        {
            Console.WriteLine($"Running {nameof(ValidateComInterfaceCreation)}...");

            var testObj = new Test();

            var wrappers = new TestComWrappers();

            // Allocate a wrapper for the object
            IntPtr comWrapper = wrappers.GetOrCreateComInterfaceForObject(testObj, CreateComInterfaceFlags.TrackerSupport);
            Assert.AreNotEqual(IntPtr.Zero, comWrapper);

            // Get a wrapper for an object and verify it is the same one.
            IntPtr comWrapperMaybe = wrappers.GetOrCreateComInterfaceForObject(testObj, CreateComInterfaceFlags.TrackerSupport);
            Assert.AreEqual(comWrapper, comWrapperMaybe);

            // Release the wrapper
            int count = Marshal.Release(comWrapper);
            Assert.AreEqual(1, count);
            count = Marshal.Release(comWrapperMaybe);
            Assert.AreEqual(0, count);

            // Create a new wrapper
            IntPtr comWrapperNew = wrappers.GetOrCreateComInterfaceForObject(testObj, CreateComInterfaceFlags.TrackerSupport);

            // Once a wrapper is created for a managed object it is always present
            Assert.AreEqual(comWrapperNew, comWrapper);

            // Release the new wrapper
            count = Marshal.Release(comWrapperNew);
            Assert.AreEqual(0, count);
        }

        static void ValidateComInterfaceCreationRoundTrip()
        {
            Console.WriteLine($"Running {nameof(ValidateComInterfaceCreationRoundTrip)}...");

            var testObj = new Test();

            var wrappers = new TestComWrappers();

            // Allocate a wrapper for the object
            IntPtr comWrapper = wrappers.GetOrCreateComInterfaceForObject(testObj, CreateComInterfaceFlags.None);
            Assert.AreNotEqual(IntPtr.Zero, comWrapper);

            var testObjUnwrapped = wrappers.GetOrCreateObjectForComInstance(comWrapper, CreateObjectFlags.Unwrap);
            Assert.AreEqual(testObj, testObjUnwrapped);

            // Release the wrapper
            int count = Marshal.Release(comWrapper);
            Assert.AreEqual(0, count);
        }

        static void ValidateFallbackQueryInterface()
        {
            Console.WriteLine($"Running {nameof(ValidateFallbackQueryInterface)}...");

            var testObj = new Test()
                {
                    EnableICustomQueryInterface = true
                };

            var wrappers = new TestComWrappers();

            // Allocate a wrapper for the object
            IntPtr comWrapper = wrappers.GetOrCreateComInterfaceForObject(testObj, CreateComInterfaceFlags.None);

            testObj.ICustomQueryInterface_GetInterfaceResult = new IntPtr(0x2000000);

            IntPtr result;
            var anyGuid = new Guid("1E42439C-DCB5-4701-ACBD-87FE92E785DE");
            testObj.ICustomQueryInterface_GetInterfaceIID = anyGuid;
            int hr = Marshal.QueryInterface(comWrapper, ref anyGuid, out result);
            Assert.AreEqual(0, hr);
            Assert.AreEqual(testObj.ICustomQueryInterface_GetInterfaceResult, result);

            var anyGuid2 = new Guid("7996D0F9-C8DD-4544-B708-0F75C6FF076F");
            hr = Marshal.QueryInterface(comWrapper, ref anyGuid2, out result);
            const int E_NOINTERFACE = unchecked((int)0x80004002);
            Assert.AreEqual(E_NOINTERFACE, hr);
            Assert.AreEqual(IntPtr.Zero, result);

            int count = Marshal.Release(comWrapper);
            Assert.AreEqual(0, count);
        }

        static void ValidateCreateObjectCachingScenario()
        {
            Console.WriteLine($"Running {nameof(ValidateCreateObjectCachingScenario)}...");

            var cw = new TestComWrappers();

            // Get an object from a tracker runtime.
            IntPtr trackerObjRaw = MockReferenceTrackerRuntime.CreateTrackerObject();

            var trackerObj1 = (ITrackerObjectWrapper)cw.GetOrCreateObjectForComInstance(trackerObjRaw, CreateObjectFlags.TrackerObject);
            var trackerObj2 = (ITrackerObjectWrapper)cw.GetOrCreateObjectForComInstance(trackerObjRaw, CreateObjectFlags.TrackerObject);
            Assert.AreEqual(trackerObj1, trackerObj2);

            // Ownership has been transferred to the wrapper.
            Marshal.Release(trackerObjRaw);

            var trackerObj3 = (ITrackerObjectWrapper)cw.GetOrCreateObjectForComInstance(trackerObjRaw, CreateObjectFlags.TrackerObject | CreateObjectFlags.UniqueInstance);
            Assert.AreNotEqual(trackerObj1, trackerObj3);
        }

        static void ValidateWrappersInstanceIsolation()
        {
            Console.WriteLine($"Running {nameof(ValidateWrappersInstanceIsolation)}...");   

            var cw1 = new TestComWrappers();
            var cw2 = new TestComWrappers();

            var testObj = new Test();

            // Allocate a wrapper for the object
            IntPtr comWrapper1 = cw1.GetOrCreateComInterfaceForObject(testObj, CreateComInterfaceFlags.TrackerSupport);
            IntPtr comWrapper2 = cw2.GetOrCreateComInterfaceForObject(testObj, CreateComInterfaceFlags.TrackerSupport);
            Assert.AreNotEqual(comWrapper1, IntPtr.Zero);
            Assert.AreNotEqual(comWrapper2, IntPtr.Zero);
            Assert.AreNotEqual(comWrapper1, comWrapper2);

            IntPtr comWrapper3 = cw1.GetOrCreateComInterfaceForObject(testObj, CreateComInterfaceFlags.TrackerSupport);
            IntPtr comWrapper4 = cw2.GetOrCreateComInterfaceForObject(testObj, CreateComInterfaceFlags.TrackerSupport);
            Assert.AreNotEqual(comWrapper3, comWrapper4);
            Assert.AreEqual(comWrapper1, comWrapper3);
            Assert.AreEqual(comWrapper2, comWrapper4);

            Marshal.Release(comWrapper1);
            Marshal.Release(comWrapper2);
            Marshal.Release(comWrapper3);
            Marshal.Release(comWrapper4);

            // Get an object from a tracker runtime.
            IntPtr trackerObjRaw = MockReferenceTrackerRuntime.CreateTrackerObject();

            // Create objects for the COM instance
            var trackerObj1 = (ITrackerObjectWrapper)cw1.GetOrCreateObjectForComInstance(trackerObjRaw, CreateObjectFlags.TrackerObject);
            var trackerObj2 = (ITrackerObjectWrapper)cw2.GetOrCreateObjectForComInstance(trackerObjRaw, CreateObjectFlags.TrackerObject);
            Assert.AreNotEqual(trackerObj1, trackerObj2);

            var trackerObj3 = (ITrackerObjectWrapper)cw1.GetOrCreateObjectForComInstance(trackerObjRaw, CreateObjectFlags.TrackerObject);
            var trackerObj4 = (ITrackerObjectWrapper)cw2.GetOrCreateObjectForComInstance(trackerObjRaw, CreateObjectFlags.TrackerObject);
            Assert.AreNotEqual(trackerObj3, trackerObj4);
            Assert.AreEqual(trackerObj1, trackerObj3);
            Assert.AreEqual(trackerObj2, trackerObj4);

            Marshal.Release(trackerObjRaw);
        }

        static void ValidatePrecreatedExternalWrapper()
        {
            Console.WriteLine($"Running {nameof(ValidatePrecreatedExternalWrapper)}...");

            var cw = new TestComWrappers();

            // Get an object from a tracker runtime.
            IntPtr trackerObjRaw = MockReferenceTrackerRuntime.CreateTrackerObject();

            // Manually create a wrapper
            var iid = typeof(ITrackerObject).GUID;
            IntPtr iTestComObject;
            int hr = Marshal.QueryInterface(trackerObjRaw, ref iid, out iTestComObject);
            Assert.AreEqual(0, hr);
            var nativeWrapper = new ITrackerObjectWrapper(iTestComObject);

            // Register wrapper, but supply the wrapper.
            var nativeWrapper2 = (ITrackerObjectWrapper)cw.GetOrRegisterObjectForComInstance(trackerObjRaw, CreateObjectFlags.TrackerObject, nativeWrapper);
            Assert.AreEqual(nativeWrapper, nativeWrapper2);

            // Ownership has been transferred to the wrapper.
            Marshal.Release(trackerObjRaw);

            // Validate reuse of a wrapper fails.
            IntPtr trackerObjRaw2 = MockReferenceTrackerRuntime.CreateTrackerObject();
            Assert.Throws<NotSupportedException>(
                () =>
                {
                    cw.GetOrRegisterObjectForComInstance(trackerObjRaw2, CreateObjectFlags.None, nativeWrapper2);
                });
            Marshal.Release(trackerObjRaw2);

            // Validate passing null wrapper fails.
            Assert.Throws<ArgumentNullException>(
                () =>
                {
                    cw.GetOrRegisterObjectForComInstance(trackerObjRaw, CreateObjectFlags.None, null);
                });
        }

        static void ValidateExternalWrapperCacheCleanUp()
        {
            Console.WriteLine($"Running {nameof(ValidateExternalWrapperCacheCleanUp)}...");

            var cw = new TestComWrappers();

            // Get an object from a tracker runtime.
            IntPtr trackerObjRaw = MockReferenceTrackerRuntime.CreateTrackerObject();

            // Create a wrapper for the object instance.
            var weakRef1 = CreateAndRegisterWrapper(cw, trackerObjRaw);

            // Run the GC to have the wrapper marked for collection.
            ForceGC();

            // Create a new wrapper for the same external object.
            var weakRef2 = CreateAndRegisterWrapper(cw, trackerObjRaw);

            // We are using a tracking resurrection WeakReference<T> so we should be able
            // to get back the objects as they are all continually re-registering for Finalization.
            Assert.IsTrue(weakRef1.TryGetTarget(out ITrackerObjectWrapper wrapper1));
            Assert.IsTrue(weakRef2.TryGetTarget(out ITrackerObjectWrapper wrapper2));

            // Check that the two wrappers aren't equal, meaning we created a new wrapper since
            // the first wrapper was removed from the internal cache.
            Assert.AreNotEqual(wrapper1, wrapper2);

            // Let the wrappers Finalize.
            wrapper1.ReregisterForFinalize = false;
            wrapper2.ReregisterForFinalize = false;

            static WeakReference<ITrackerObjectWrapper> CreateAndRegisterWrapper(ComWrappers cw, IntPtr trackerObjRaw)
            {
                // Manually create a wrapper
                var iid = typeof(ITrackerObject).GUID;
                IntPtr iTestComObject;
                int hr = Marshal.QueryInterface(trackerObjRaw, ref iid, out iTestComObject);
                Assert.AreEqual(0, hr);
                var nativeWrapper = new ITrackerObjectWrapper(iTestComObject);

                nativeWrapper = (ITrackerObjectWrapper)cw.GetOrRegisterObjectForComInstance(trackerObjRaw, CreateObjectFlags.None, nativeWrapper);

                // Set this on the return instead of during creation since the returned wrapper may be the one from
                // the internal cache and not the one passed in above.
                nativeWrapper.ReregisterForFinalize = true;

                return new WeakReference<ITrackerObjectWrapper>(nativeWrapper, trackResurrection: true);
            }
        }

        static void ValidateSuppliedInnerNotAggregation()
        {
            Console.WriteLine($"Running {nameof(ValidateSuppliedInnerNotAggregation)}...");

            var cw = new TestComWrappers();

            // Attempt to register a non-zero instance with a non-zero inner value without
            // indicating the scenario is aggregaion.
            var invalidInstance = new IntPtr(1);
            var invalidInner = new IntPtr(2);
            Assert.Throws<InvalidOperationException>(
                () =>
                {
                    cw.GetOrRegisterObjectForComInstance(invalidInstance, CreateObjectFlags.None, new object(), invalidInner);
                });
        }

        static void ValidateIUnknownImpls()
            => TestComWrappers.ValidateIUnknownImpls();

        class BadComWrappers : ComWrappers
        {
            public enum FailureMode
            {
                ReturnInvalid,
                ThrowException,
            }

            public const int ExceptionErrorCode = 0x27;

            public FailureMode ComputeVtablesMode { get; set; }
            public FailureMode CreateObjectMode { get; set; }

            protected unsafe override ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
            {
                switch (ComputeVtablesMode)
                {
                    case FailureMode.ReturnInvalid:
                    {
                        count = -1;
                        return null;
                    }
                    case FailureMode.ThrowException:
                        throw new Exception() { HResult = ExceptionErrorCode };
                    default:
                        Assert.Fail("Invalid failure mode");
                        throw new Exception("UNREACHABLE");
                }
            }

            protected override object CreateObject(IntPtr externalComObject, CreateObjectFlags flags)
            {
                switch (CreateObjectMode)
                {
                    case FailureMode.ReturnInvalid:
                        return null;
                    case FailureMode.ThrowException:
                        throw new Exception() { HResult = ExceptionErrorCode };
                    default:
                        Assert.Fail("Invalid failure mode");
                        throw new Exception("UNREACHABLE");
                }
            }

            protected override void ReleaseObjects(IEnumerable objects)
            {
                throw new NotSupportedException();
            }
        }

        static void ValidateBadComWrapperImpl()
        {
            Console.WriteLine($"Running {nameof(ValidateBadComWrapperImpl)}...");

            var wrapper = new BadComWrappers();

            Assert.Throws<ArgumentException>(
                () =>
                {
                    wrapper.ComputeVtablesMode = BadComWrappers.FailureMode.ReturnInvalid;
                    wrapper.GetOrCreateComInterfaceForObject(new Test(), CreateComInterfaceFlags.None);
                });

            try
            {
                wrapper.ComputeVtablesMode = BadComWrappers.FailureMode.ThrowException;
                wrapper.GetOrCreateComInterfaceForObject(new Test(), CreateComInterfaceFlags.None);
            }
            catch (Exception e)
            {
                Assert.AreEqual(BadComWrappers.ExceptionErrorCode, e.HResult);
            }

            IntPtr trackerObjRaw = MockReferenceTrackerRuntime.CreateTrackerObject();

            Assert.Throws<ArgumentNullException>(
                () =>
                {
                    wrapper.CreateObjectMode = BadComWrappers.FailureMode.ReturnInvalid;
                    wrapper.GetOrCreateObjectForComInstance(trackerObjRaw, CreateObjectFlags.None);
                });

            try
            {
                wrapper.CreateObjectMode = BadComWrappers.FailureMode.ThrowException;
                wrapper.GetOrCreateObjectForComInstance(trackerObjRaw, CreateObjectFlags.None);
            }
            catch (Exception e)
            {
                Assert.AreEqual(BadComWrappers.ExceptionErrorCode, e.HResult);
            }

            Marshal.Release(trackerObjRaw);
        }

        static void ValidateRuntimeTrackerScenario()
        {
            Console.WriteLine($"Running {nameof(ValidateRuntimeTrackerScenario)}...");

            var cw = new TestComWrappers();

            // Get an object from a tracker runtime.
            IntPtr trackerObjRaw = MockReferenceTrackerRuntime.CreateTrackerObject();

            // Create a managed wrapper for the native object.
            var trackerObj = (ITrackerObjectWrapper)cw.GetOrCreateObjectForComInstance(trackerObjRaw, CreateObjectFlags.TrackerObject);

            // Ownership has been transferred to the wrapper.
            Marshal.Release(trackerObjRaw);

            var testWrapperIds = new List<int>();
            for (int i = 0; i < 1000; ++i)
            {
                // Create a native wrapper for the managed object.
                IntPtr testWrapper = cw.GetOrCreateComInterfaceForObject(new Test(), CreateComInterfaceFlags.TrackerSupport);

                // Pass the managed object to the native object.
                int id = trackerObj.AddObjectRef(testWrapper);

                // Retain the managed object wrapper ptr.
                testWrapperIds.Add(id);

                Marshal.Release(testWrapper);
            }

            Assert.IsTrue(testWrapperIds.Count <= Test.InstanceCount);

            ForceGC();

            Assert.IsTrue(testWrapperIds.Count <= Test.InstanceCount);

            // Remove the managed object ref from the native object.
            foreach (int id in testWrapperIds)
            {
                trackerObj.DropObjectRef(id);
            }

            testWrapperIds.Clear();

            ForceGC();
        }

        static void ValidateQueryInterfaceAfterManagedObjectCollected()
        {
            Console.WriteLine($"Running {nameof(ValidateQueryInterfaceAfterManagedObjectCollected)}...");

            var cw = new TestComWrappers();

            {
                // Activate the Reference Tracker system in the .NET runtime by consuming an IReferenceTracker instance.
                IntPtr trackerObjRaw = MockReferenceTrackerRuntime.CreateTrackerObject();
                var trackerObj = (ITrackerObjectWrapper)cw.GetOrCreateObjectForComInstance(trackerObjRaw, CreateObjectFlags.TrackerObject);
                Marshal.Release(trackerObjRaw);
            }

            int refCount;
            IntPtr refTrackerTarget;

            {
                // Create a native wrapper over a managed object.
                IntPtr testWrapper = CreateWrapper(cw);

                refTrackerTarget = MockReferenceTrackerRuntime.TrackerTarget_AddRefFromReferenceTrackerAndReturn(testWrapper);

                // Ownership has been transferred to the IReferenceTrackerTarget instance.
                // The COM reference count should be 0 and indicates to the GC the managed object
                // can be collected.
                refCount = Marshal.Release(testWrapper);
                Assert.AreEqual(0, refCount);
            }

            ForceGC();

            // Calling QueryInterface on an IReferenceTrackerTarget instance is permitted when
            // the wrapper lifetime has been extended. However, the QueryInterface may fail
            // if the associated managed object was collected. The failure here is an important
            // part of the contract for a Reference Tracker runtime.
            var iid = typeof(ITest).GUID;
            IntPtr iTestComObject;
            int hr = Marshal.QueryInterface(refTrackerTarget, ref iid, out iTestComObject);

            const int COR_E_ACCESSING_CCW = unchecked((int)0x80131544);
            Assert.AreEqual(COR_E_ACCESSING_CCW, hr);

            // Release the IReferenceTrackerTarget instance.
            refCount = MockReferenceTrackerRuntime.TrackerTarget_ReleaseFromReferenceTracker(refTrackerTarget);
            Assert.AreEqual(0, refCount);

            static IntPtr CreateWrapper(TestComWrappers cw)
            {
                return cw.GetOrCreateComInterfaceForObject(new Test(), CreateComInterfaceFlags.TrackerSupport);
            }
        }

        unsafe class Derived : ITrackerObjectWrapper
        {
            public Derived(ComWrappers cw, bool aggregateRefTracker)
                : base(cw, aggregateRefTracker)
            { }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static WeakReference<Derived> AllocateAndUseBaseType(ComWrappers cw, bool aggregateRefTracker)
            {
                var derived = new Derived(cw, aggregateRefTracker);

                // Use the base type
                IntPtr testWrapper = cw.GetOrCreateComInterfaceForObject(new Test(), CreateComInterfaceFlags.TrackerSupport);
                int id = derived.AddObjectRef(testWrapper);

                // Tell the tracker runtime to release its hold on the base instance.
                MockReferenceTrackerRuntime.ReleaseAllTrackerObjects();

                // Validate the GC is tracking the entire Derived type.
                ForceGC();

                derived.DropObjectRef(id);

                return new WeakReference<Derived>(derived);
            }
        }

        static void ValidateAggregationWithComObject()
        {
            Console.WriteLine($"Running {nameof(ValidateAggregationWithComObject)}...");

            using var allocTracker = MockReferenceTrackerRuntime.CountTrackerObjectAllocations();
            var cw = new TestComWrappers();
            WeakReference<Derived> weakRef = Derived.AllocateAndUseBaseType(cw, aggregateRefTracker: false);

            ForceGC();

            // Validate all instances were cleaned up
            Assert.IsFalse(weakRef.TryGetTarget(out _));
            Assert.AreEqual(0, allocTracker.GetCount());
        }

        static void ValidateAggregationWithReferenceTrackerObject()
        {
            Console.WriteLine($"Running {nameof(ValidateAggregationWithReferenceTrackerObject)}...");

            using var allocTracker = MockReferenceTrackerRuntime.CountTrackerObjectAllocations();
            var cw = new TestComWrappers();
            WeakReference<Derived> weakRef = Derived.AllocateAndUseBaseType(cw, aggregateRefTracker: true);

            ForceGC();

            // Validate all instances were cleaned up.
            Assert.IsFalse(weakRef.TryGetTarget(out _));

            // Reference counter cleanup requires additional GCs since the Finalizer is used
            // to clean up the Reference Tracker runtime references.
            ForceGC();

            Assert.AreEqual(0, allocTracker.GetCount());
        }

        static int Main(string[] doNotUse)
        {
            try
            {
                ValidateComInterfaceCreation();
                ValidateComInterfaceCreationRoundTrip();
                ValidateFallbackQueryInterface();
                ValidateCreateObjectCachingScenario();
                ValidateWrappersInstanceIsolation();
                ValidatePrecreatedExternalWrapper();
                ValidateExternalWrapperCacheCleanUp();
                ValidateSuppliedInnerNotAggregation();
                ValidateIUnknownImpls();
                ValidateBadComWrapperImpl();
                ValidateRuntimeTrackerScenario();
                ValidateQueryInterfaceAfterManagedObjectCollected();
                ValidateAggregationWithComObject();
                ValidateAggregationWithReferenceTrackerObject();
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

