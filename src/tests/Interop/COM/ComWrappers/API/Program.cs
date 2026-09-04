// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ComWrappersTests
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.InteropServices.Marshalling;
    using System.Threading;

    using ComWrappersTests.Common;
    using TestLibrary;
    using Xunit;

    public class Program : IDisposable
    {
        record class WrappedUserState(object? UserState);

        class TestComWrappers : ComWrappers
        {
            private static IntPtr fpQueryInterface = default;
            private static IntPtr fpAddRef = default;
            private static IntPtr fpRelease = default;
            private static IntPtr fpWrappedQueryInterface = default;

            static TestComWrappers()
            {
                ComWrappers.GetIUnknownImpl(out fpQueryInterface, out fpAddRef, out fpRelease);
                fpWrappedQueryInterface = MockReferenceTrackerRuntime.WrapQueryInterface(fpQueryInterface);
            }

            public bool UseManualReleaseITestObjectWrapper { get; init; }

            protected unsafe override ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
            {
                ComInterfaceEntry* entryRaw = null;
                count = 0;
                if (obj is Test)
                {
                    // If the caller is requesting an IUnknown definition we supply 2 vtables
                    count = flags.HasFlag(CreateComInterfaceFlags.CallerDefinedIUnknown) ? 2 : 1;
                    entryRaw = (ComInterfaceEntry*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(ITestVtbl), sizeof(ComInterfaceEntry) * count);

                    int index = 0;
                    if (flags.HasFlag(CreateComInterfaceFlags.CallerDefinedIUnknown))
                    {
                        // This IUnknown wraps the QueryInterface to validate proper detection
                        // of ComWrappers created managed object wrappers.
                        var vtbl = new IUnknownVtbl()
                        {
                            QueryInterface = fpWrappedQueryInterface,
                            AddRef = fpAddRef,
                            Release = fpRelease
                        };

                        var vtblRaw = RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(ITestVtbl), sizeof(IUnknownVtbl));
                        Marshal.StructureToPtr(vtbl, vtblRaw, false);

                        entryRaw[index].IID = IUnknownVtbl.IID_IUnknown;
                        entryRaw[index].Vtable = vtblRaw;
                        index++;
                    }

                    {
                        var vtbl = new ITestVtbl()
                        {
                            IUnknownImpl = new IUnknownVtbl()
                            {
                                QueryInterface = fpQueryInterface,
                                AddRef = fpAddRef,
                                Release = fpRelease
                            },
                            SetValue = Marshal.GetFunctionPointerForDelegate(ITestVtbl.pSetValue)
                        };
                        var vtblRaw = RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(ITestVtbl), sizeof(ITestVtbl));
                        Marshal.StructureToPtr(vtbl, vtblRaw, false);

                        entryRaw[index].IID = typeof(ITest).GUID;
                        entryRaw[index].Vtable = vtblRaw;
                        index++;
                    }
                }
                else if (obj is NotWrappedObject)
                {
                    // Return a single vtable for the INotWrappedObject interface.
                    // Or two if the caller is requesting an IUnknown definition.
                    count = flags.HasFlag(CreateComInterfaceFlags.CallerDefinedIUnknown) ? 2 : 1;
                    entryRaw = (ComInterfaceEntry*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(NotWrappedObject), sizeof(ComInterfaceEntry) * count);

                    var vtbl = new IUnknownVtbl()
                    {
                        QueryInterface = fpQueryInterface,
                        AddRef = fpAddRef,
                        Release = fpRelease
                    };

                    int index = 0;

                    if (flags.HasFlag(CreateComInterfaceFlags.CallerDefinedIUnknown))
                    {
                        var vtblRaw = RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(NotWrappedObject), sizeof(IUnknownVtbl));
                        Marshal.StructureToPtr(vtbl, vtblRaw, false);

                        entryRaw[index].IID = IUnknownVtbl.IID_IUnknown;
                        entryRaw[index].Vtable = vtblRaw;
                        index++;
                    }

                    {
                        var vtblRaw = RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(NotWrappedObject), sizeof(IUnknownVtbl));
                        Marshal.StructureToPtr(vtbl, vtblRaw, false);

                        entryRaw[index].IID = typeof(INotWrappedObject).GUID;
                        entryRaw[index].Vtable = vtblRaw;
                        index++;
                    }
                }

                return entryRaw;
            }

            protected override object CreateObject(IntPtr externalComObject, CreateObjectFlags flag)
            {
                IntPtr iTrackerComObject;
                int hr = Marshal.QueryInterface(externalComObject, typeof(ITrackerObject).GUID, out iTrackerComObject);
                if (hr == 0)
                {
                    return new ITrackerObjectWrapper(iTrackerComObject);
                }
                IntPtr iTest;
                hr = Marshal.QueryInterface(externalComObject, typeof(ITest).GUID, out iTest);
                if (hr == 0)
                {
                    if (UseManualReleaseITestObjectWrapper)
                    {
                        return new ManualReleaseITestObjectWrapper(iTest);
                    }
                    else
                    {
                        return new ITestObjectWrapper(iTest);
                    }
                }

                Assert.Fail("The COM object should support ITrackerObject or ITest for all tests in this test suite.");
                return null;
            }

            public bool CalledUserStateOverload { get; set; } = false;

            public bool CallBaseCreateObject { get; set; } = false;

            protected override object CreateObject(IntPtr externalComObject, CreateObjectFlags flags, object? userState, out CreatedWrapperFlags createdWrapperFlags)
            {
                CalledUserStateOverload = true;

                if (CallBaseCreateObject)
                {
                    return base.CreateObject(externalComObject, flags, userState, out createdWrapperFlags);
                }

                createdWrapperFlags = CreatedWrapperFlags.None;

                int hr = Marshal.QueryInterface(externalComObject, typeof(INotWrappedObject).GUID, out IntPtr iNotWrappedObject);
                if (hr == 0)
                {
                    // This is a non-wrapped object, return the user state as an object.
                    Marshal.Release(iNotWrappedObject);
                    createdWrapperFlags = CreatedWrapperFlags.NonWrapping;
                    return new WrappedUserState(userState);
                }

                object result = CreateObject(externalComObject, flags);
                if (result is ITrackerObjectWrapper trackerObj)
                {
                    createdWrapperFlags = CreatedWrapperFlags.TrackerObject;
                }

                return result;
            }

            public const int ReleaseObjectsCallAck = unchecked((int)-1);

            protected override void ReleaseObjects(IEnumerable objects)
            {
                throw new Exception() { HResult = ReleaseObjectsCallAck };
            }

            public static void ValidateIUnknownImpls()
            {
                Console.WriteLine($"Running {nameof(ValidateIUnknownImpls)}...");

                ComWrappers.GetIUnknownImpl(out IntPtr fpQueryInterface, out IntPtr fpAddRef, out IntPtr fpRelease);

                Assert.NotEqual(fpQueryInterface, IntPtr.Zero);
                Assert.NotEqual(fpAddRef, IntPtr.Zero);
                Assert.NotEqual(fpRelease, IntPtr.Zero);
            }
        }

        public void Dispose() => ForceGC();

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

        [MethodImpl(MethodImplOptions.NoInlining)]
        [ActiveIssue("Not supported on Mono", TestRuntimes.Mono)]
        [Fact]
        public void ValidateComInterfaceCreation()
        {
            Console.WriteLine($"Running {nameof(ValidateComInterfaceCreation)}...");

            var testObj = new Test();

            var wrappers = new TestComWrappers();

            // Allocate a wrapper for the object
            IntPtr comWrapper = wrappers.GetOrCreateComInterfaceForObject(testObj, CreateComInterfaceFlags.TrackerSupport);
            Assert.NotEqual(IntPtr.Zero, comWrapper);

            // Get a wrapper for an object and verify it is the same one.
            IntPtr comWrapperMaybe = wrappers.GetOrCreateComInterfaceForObject(testObj, CreateComInterfaceFlags.TrackerSupport);
            Assert.Equal(comWrapper, comWrapperMaybe);

            // Release the wrapper
            int count = Marshal.Release(comWrapper);
            Assert.Equal(1, count);
            count = Marshal.Release(comWrapperMaybe);
            Assert.Equal(0, count);

            // Create a new wrapper
            IntPtr comWrapperNew = wrappers.GetOrCreateComInterfaceForObject(testObj, CreateComInterfaceFlags.TrackerSupport);

            // Once a wrapper is created for a managed object it is always present
            Assert.Equal(comWrapperNew, comWrapper);

            // Release the new wrapper
            count = Marshal.Release(comWrapperNew);
            Assert.Equal(0, count);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [ActiveIssue("Not supported on Mono", TestRuntimes.Mono)]
        [Fact]
        public void ValidateComInterfaceCreationRoundTrip()
        {
            Console.WriteLine($"Running {nameof(ValidateComInterfaceCreationRoundTrip)}...");

            var testObj = new Test();

            var wrappers = new TestComWrappers();

            // Allocate a wrapper for the object
            IntPtr comWrapper = wrappers.GetOrCreateComInterfaceForObject(testObj, CreateComInterfaceFlags.None);
            Assert.NotEqual(IntPtr.Zero, comWrapper);

            var testObjUnwrapped = wrappers.GetOrCreateObjectForComInstance(comWrapper, CreateObjectFlags.Unwrap);
            Assert.Same(testObj, testObjUnwrapped);

            // UniqueInstance and Unwrap should always be a new com object, never unwrapped
            var testObjUniqueUnwrapped = (ITestObjectWrapper)wrappers.GetOrCreateObjectForComInstance(comWrapper, CreateObjectFlags.Unwrap | CreateObjectFlags.UniqueInstance);
            Assert.NotSame(testObj, testObjUniqueUnwrapped);
            testObjUniqueUnwrapped.FinalRelease();

            // Release the wrapper
            int count = Marshal.Release(comWrapper);
            Assert.Equal(0, count);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [ActiveIssue("Not supported on Mono", TestRuntimes.Mono)]
        [Fact]
        public void ValidateComInterfaceUnwrapWrapperSpecific()
        {
            Console.WriteLine($"Running {nameof(ValidateComInterfaceUnwrapWrapperSpecific)}...");

            var testObj = new Test();

            var wrappers = new TestComWrappers();

            // Allocate a wrapper for the object
            IntPtr comWrapper = wrappers.GetOrCreateComInterfaceForObject(testObj, CreateComInterfaceFlags.None);
            Assert.NotEqual(IntPtr.Zero, comWrapper);

            // Make sure that unwrapping the wrapper in the same ComWrappers context gets back the same object
            var testObjUnwrapped = GetUnwrappedObjectHandleForComInstance(wrappers, comWrapper);
            AssertSameInstanceAndFreeHandle(testObj, testObjUnwrapped);

            // Make sure that unwrapping the wrapper in a different ComWrappers context gets back a different object
            var wrappers2 = new TestComWrappers();
            var testObjWrapper2 = GetUnwrappedObjectHandleForComInstance(wrappers2, comWrapper);
            AssertNotSameInstanceAndFreeHandle(testObj, testObjWrapper2);

            // Make sure that unwrapping a wrapper from a different ComWrappers context in a context that has created a CCW
            // for the object only unwraps the wrapper from that context, not from any context.
            var wrappers3 = new TestComWrappers();
            IntPtr comWrapper3 = wrappers3.GetOrCreateComInterfaceForObject(testObj, CreateComInterfaceFlags.None);

            Assert.NotEqual(IntPtr.Zero, comWrapper3);
            Assert.NotEqual(comWrapper, comWrapper3);

            var testObjWrapper3 = GetUnwrappedObjectHandleForComInstance(wrappers3, comWrapper);
            AssertNotSameInstanceAndFreeHandle(testObj, testObjWrapper3);
            AssertSameInstanceAndFreeHandle(testObj, GetUnwrappedObjectHandleForComInstance(wrappers3, comWrapper3));

            // Force a GC to release the new managed object wrappers we made
            ForceGC();

            // Release the COM wrappers
            int count = Marshal.Release(comWrapper);
            count = Marshal.Release(comWrapper3);
            Assert.Equal(0, count);

            // Make sure that all possible references to the CCW over the RCW are never on the same frame
            // as the rest of the test (to ensure that the GC does collect it).
            [MethodImpl(MethodImplOptions.NoInlining)]
            static GCHandle GetUnwrappedObjectHandleForComInstance(ComWrappers wrapper, nint comWrapper)
            {
                var obj = wrapper.GetOrCreateObjectForComInstance(comWrapper, CreateObjectFlags.Unwrap);
                return GCHandle.Alloc(obj);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static void AssertSameInstanceAndFreeHandle(object obj, GCHandle handle)
            {
                Assert.True(handle.IsAllocated);
                Assert.Same(obj, handle.Target);
                handle.Free();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static void AssertNotSameInstanceAndFreeHandle(object obj, GCHandle handle)
            {
                Assert.True(handle.IsAllocated);
                Assert.NotSame(obj, handle.Target);
                handle.Free();
            }
        }

        [ActiveIssue("Not supported on Mono", TestRuntimes.Mono)]
        [Fact]
        public void ValidateComObjectExtendsManagedLifetime()
        {
            Console.WriteLine($"Running {nameof(ValidateComObjectExtendsManagedLifetime)}...");

            // Cleanup any existing objects
            ForceGC();
            Assert.Equal(0, Test.InstanceCount);

            // Allocate a wrapper for the object
            IntPtr comWrapper = CreateObjectAndGetComInterface();
            Assert.NotEqual(IntPtr.Zero, comWrapper);

            // GC should not free object
            Assert.Equal(1, Test.InstanceCount);
            ForceGC();
            Assert.Equal(1, Test.InstanceCount);

            // Release the wrapper
            int count = Marshal.Release(comWrapper);
            Assert.Equal(0, count);

            // Check that the object is no longer rooted.
            ForceGC();
            Assert.Equal(0, Test.InstanceCount);

            [MethodImpl(MethodImplOptions.NoInlining)]
            static IntPtr CreateObjectAndGetComInterface()
            {
                var wrappers = new TestComWrappers();
                return wrappers.GetOrCreateComInterfaceForObject(new Test(), CreateComInterfaceFlags.None);
            }
        }

        // Just because one use of a COM interface returned from GetOrCreateComInterfaceForObject
        // hits zero ref count does not mean future calls to GetOrCreateComInterfaceForObject
        // should return an unusable object.
        [MethodImpl(MethodImplOptions.NoInlining)]
        [ActiveIssue("Not supported on Mono", TestRuntimes.Mono)]
        [Fact]
        public void ValidateCreatingAComInterfaceForObjectAfterTheFirstIsFree()
        {
            Console.WriteLine($"Running {nameof(ValidateCreatingAComInterfaceForObjectAfterTheFirstIsFree)}...");

            var wrappers = new TestComWrappers();
            var testInstance = new Test();

            CallSetValue(wrappers, testInstance, 1);
            CallSetValue(wrappers, testInstance, 2);

            GC.KeepAlive(testInstance);

            unsafe static void CallSetValue(TestComWrappers wrappers, Test testInstance, int value)
            {
                IntPtr nativeInstance = wrappers.GetOrCreateComInterfaceForObject(testInstance, CreateComInterfaceFlags.None);
                Assert.NotEqual(IntPtr.Zero, nativeInstance);

                nint itestPtr;
                Assert.Equal(0, Marshal.QueryInterface(nativeInstance, typeof(ITest).GUID, out itestPtr));

                var inst = (ComWrappers.ComInterfaceDispatch*)itestPtr;
                var vtbl = (ITestVtbl*)(inst->Vtable);
                var setValue = (delegate* unmanaged<nint, int, int>)vtbl->SetValue;

                Assert.Equal(0, setValue(itestPtr, value));
                Assert.Equal(value, testInstance.GetValue());

                // release for QueryInterface
                Assert.Equal(1, Marshal.Release(itestPtr));
                // release for GetOrCreateComInterfaceForObject
                Assert.Equal(0, Marshal.Release(nativeInstance));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [ActiveIssue("Not supported on Mono", TestRuntimes.Mono)]
        [Fact]
        public void ValidateManagedObjectWrapperResurrection()
        {
            Console.WriteLine($"Running {nameof(ValidateManagedObjectWrapperResurrection)}...");

            var wrappers = new TestComWrappers();

            try
            {
                CreateResurrectingTestInstance(wrappers);

                ForceGC();

                CallSetValue(wrappers);
            }
            finally
            {
                Test.Resurrected = null;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static void CreateResurrectingTestInstance(ComWrappers wrapper)
            {
                Test testInstance = new Test()
                {
                    EnableResurrection = true,
                };
                IntPtr nativeInstance = wrapper.GetOrCreateComInterfaceForObject(testInstance, CreateComInterfaceFlags.None);
                Assert.Equal(0, Marshal.Release(nativeInstance));
            }

            unsafe static void CallSetValue(ComWrappers wrappers)
            {
                Assert.NotNull(Test.Resurrected);
                IntPtr nativeInstance = wrappers.GetOrCreateComInterfaceForObject(Test.Resurrected, CreateComInterfaceFlags.None);
                Assert.NotEqual(IntPtr.Zero, nativeInstance);

                nint itestPtr;
                Assert.Equal(0, Marshal.QueryInterface(nativeInstance, typeof(ITest).GUID, out itestPtr));

                var inst = (ComWrappers.ComInterfaceDispatch*)itestPtr;
                var vtbl = (ITestVtbl*)(inst->Vtable);
                var setValue = (delegate* unmanaged<nint, int, int>)vtbl->SetValue;

                Assert.Equal(0, setValue(itestPtr, 42));
                Assert.Equal(42, Test.Resurrected.GetValue());

                // release for QueryInterface
                Assert.Equal(1, Marshal.Release(itestPtr));
                // release for GetOrCreateComInterfaceForObject
                Assert.Equal(0, Marshal.Release(nativeInstance));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [ActiveIssue("Not supported on Mono", TestRuntimes.Mono)]
        [Fact]
        public void ValidateFallbackQueryInterface()
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
            int hr = Marshal.QueryInterface(comWrapper, anyGuid, out result);
            Assert.Equal(0, hr);
            Assert.Equal(testObj.ICustomQueryInterface_GetInterfaceResult, result);

            var anyGuid2 = new Guid("7996D0F9-C8DD-4544-B708-0F75C6FF076F");
            hr = Marshal.QueryInterface(comWrapper, anyGuid2, out result);
            const int E_NOINTERFACE = unchecked((int)0x80004002);
            Assert.Equal(E_NOINTERFACE, hr);
            Assert.Equal(IntPtr.Zero, result);

            int count = Marshal.Release(comWrapper);
            Assert.Equal(0, count);
        }

        [ActiveIssue("Not supported on Mono", TestRuntimes.Mono)]
        [Fact]
        public void ValidateCreateObjectCachingScenario()
        {
            Console.WriteLine($"Running {nameof(ValidateCreateObjectCachingScenario)}...");

            var cw = new TestComWrappers();

            // Get an object from a tracker runtime.
            IntPtr trackerObjRaw = MockReferenceTrackerRuntime.CreateTrackerObject();

            var trackerObj1 = (ITrackerObjectWrapper)cw.GetOrCreateObjectForComInstance(trackerObjRaw, CreateObjectFlags.TrackerObject);
            var trackerObj2 = (ITrackerObjectWrapper)cw.GetOrCreateObjectForComInstance(trackerObjRaw, CreateObjectFlags.TrackerObject);
            Assert.Equal(trackerObj1, trackerObj2);

            // Ownership has been transferred to the wrapper.
            Marshal.Release(trackerObjRaw);

            var trackerObj3 = (ITrackerObjectWrapper)cw.GetOrCreateObjectForComInstance(trackerObjRaw, CreateObjectFlags.TrackerObject | CreateObjectFlags.UniqueInstance);
            Assert.NotEqual(trackerObj1, trackerObj3);
        }

        private sealed unsafe class PlainProxyComWrappers : ComWrappers
        {
            protected override ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
            {
                count = 0;
                return null;
            }

            protected override object CreateObject(IntPtr externalComObject, CreateObjectFlags flags) => new();

            protected override void ReleaseObjects(IEnumerable objects) => throw new NotImplementedException();
        }

        // An RCW is only resolvable back to its COM instance once its wrapper is in the wrapper table, and
        // that registration cannot be done while holding a cache lock. So an entry is published before it
        // is registered, and an entry in that state is not handed out, or a thread could be given an RCW
        // that does not resolve yet. Every round here is a fresh COM instance that all the threads race to
        // create the RCW for, which is the shape that hits it: handing such an entry out fails in the low
        // tens out of these several thousand attempts, and skipping it, never.
        [ActiveIssue("Not supported on Mono", TestRuntimes.Mono)]
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
        public void ValidateCreateObjectRaceResolvesImmediately()
        {
            Console.WriteLine($"Running {nameof(ValidateCreateObjectRaceResolvesImmediately)}...");

            const int ThreadCount = 16;
            const int RoundCount = 500;

            IntPtr[] instances = new IntPtr[RoundCount];

            for (int i = 0; i < instances.Length; i++)
            {
                instances[i] = MockReferenceTrackerRuntime.CreateTrackerObject();
            }

            var cw = new PlainProxyComWrappers();
            var failures = new ConcurrentQueue<Exception>();

            // The RCWs are kept alive for the whole run, so that no wrapper is finalized underneath a
            // round that is still being checked.
            object[] proxies = new object[RoundCount];

            using var barrier = new Barrier(ThreadCount);

            int unresolved = 0;
            var threads = new Thread[ThreadCount];

            for (int t = 0; t < threads.Length; t++)
            {
                threads[t] = new Thread(() =>
                {
                    for (int round = 0; round < RoundCount; round++)
                    {
                        try
                        {
                            // Line every thread up on the same instance, so they all miss the cache and
                            // race to publish it rather than finding each other's entries.
                            barrier.SignalAndWait(TimeSpan.FromMinutes(1));

                            object proxy = cw.GetOrCreateObjectForComInstance(instances[round], CreateObjectFlags.None);

                            proxies[round] = proxy;

                            // The RCW is in hand, so it has to name the COM instance it was created for.
                            if (ComWrappers.TryGetComInstance(proxy, out IntPtr unknown))
                            {
                                Marshal.Release(unknown);
                            }
                            else
                            {
                                Interlocked.Increment(ref unresolved);
                            }
                        }
                        catch (Exception e)
                        {
                            // Carry on to the next round regardless, so that the other threads are not
                            // left waiting on a barrier this one has stopped arriving at.
                            failures.Enqueue(e);
                        }
                    }
                })
                { IsBackground = true, Name = $"ComWrappers resolve {t}" };

                threads[t].Start();
            }

            foreach (Thread thread in threads)
            {
                Assert.True(thread.Join(TimeSpan.FromMinutes(2)), "A worker thread did not finish, which suggests a deadlock while racing to publish.");
            }

            Assert.Empty(failures);
            Assert.Equal(0, unresolved);

            GC.KeepAlive(proxies);

            foreach (IntPtr instance in instances)
            {
                Marshal.Release(instance);
            }
        }

        // Cache entries are published, read and removed from several threads at once, and the GC handle
        // behind an entry is freed while other threads may still be looking that entry up. This hammers
        // all of those paths together, with collections and finalizers running underneath, to catch an
        // entry ever being read after the handle behind it was freed.
        [ActiveIssue("Not supported on Mono", TestRuntimes.Mono)]
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
        public void ValidateCreateObjectConcurrentCacheAccess()
        {
            Console.WriteLine($"Running {nameof(ValidateCreateObjectConcurrentCacheAccess)}...");

            const int ThreadCount = 8;
            const int IterationCount = 300;
            const int InstanceCount = 4;

            var cw = new TestComWrappers();

            IntPtr[] instances = new IntPtr[InstanceCount];
            IntPtr[] identities = new IntPtr[InstanceCount];

            for (int i = 0; i < instances.Length; i++)
            {
                instances[i] = MockReferenceTrackerRuntime.CreateTrackerObject();

                // ComWrappers keys the cache on the identity IUnknown, which is what a round trip back to
                // native produces, and that is not necessarily the pointer the object was created as.
                Assert.Equal(0, Marshal.QueryInterface(instances[i], IUnknownVtbl.IID_IUnknown, out identities[i]));
            }

            using var start = new Barrier(ThreadCount + 1);
            var failures = new ConcurrentQueue<Exception>();
            var threads = new Thread[ThreadCount];

            for (int t = 0; t < threads.Length; t++)
            {
                int index = t;

                threads[t] = new Thread(() =>
                {
                    try
                    {
                        Assert.True(start.SignalAndWait(TimeSpan.FromMinutes(1)), "Timed out waiting for the start barrier.");

                        for (int i = 0; i < IterationCount; i++)
                        {
                            // Every thread walks the instances from a different offset, so the threads are
                            // spread over the buckets rather than all queueing on one of them.
                            int slot = (i + index) % instances.Length;

                            var wrapper = (ITrackerObjectWrapper)cw.GetOrCreateObjectForComInstance(instances[slot], CreateObjectFlags.None);

                            Assert.NotNull(wrapper);

                            // Asking again while this thread still holds the wrapper has to produce the same
                            // object, which is the guarantee the cache exists to provide.
                            var again = (ITrackerObjectWrapper)cw.GetOrCreateObjectForComInstance(instances[slot], CreateObjectFlags.None);

                            Assert.Same(wrapper, again);

                            // Round trip it back to a native pointer, which reads the wrapper the cache entry
                            // resolves to, and has to name the instance this thread asked for.
                            Assert.True(ComWrappers.TryGetComInstance(wrapper, out IntPtr unknown));
                            Assert.Equal(identities[slot], unknown);
                            Marshal.Release(unknown);

                            if ((i % 16) == index % 16)
                            {
                                // Drop everything this thread is holding and collect, so that entries go dead
                                // and wrapper finalizers run while the other threads are still using the cache.
                                GC.Collect();
                                GC.WaitForPendingFinalizers();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        failures.Enqueue(e);
                    }
                })
                { IsBackground = true, Name = $"ComWrappers cache {index}" };

                threads[t].Start();
            }

            Assert.True(start.SignalAndWait(TimeSpan.FromMinutes(1)), "Timed out waiting for the start barrier.");

            foreach (Thread thread in threads)
            {
                Assert.True(thread.Join(TimeSpan.FromMinutes(2)), "A worker thread did not finish, which suggests a deadlock in the RCW cache.");
            }

            Assert.Empty(failures);

            ForceGC();

            foreach (IntPtr identity in identities)
            {
                Marshal.Release(identity);
            }

            foreach (IntPtr instance in instances)
            {
                Marshal.Release(instance);
            }
        }

        // Registering a caller supplied object and creating one race through the same cache, and only one
        // of them can win. Whichever does, every caller has to come back with it, and the objects that
        // lost have to be left exactly as they were rather than half registered.
        [ActiveIssue("Not supported on Mono", TestRuntimes.Mono)]
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
        public void ValidateRegisterAndCreateRaceForSameComInstance()
        {
            Console.WriteLine($"Running {nameof(ValidateRegisterAndCreateRaceForSameComInstance)}...");

            const int ThreadCount = 8;

            IntPtr instanceRaw = MockReferenceTrackerRuntime.CreateTrackerObject();

            Assert.Equal(0, Marshal.QueryInterface(instanceRaw, IUnknownVtbl.IID_IUnknown, out IntPtr identity));

            var cw = new PlainProxyComWrappers();
            var failures = new ConcurrentQueue<Exception>();

            // Half of these are left null, for the threads that ask for an object to be created
            // rather than bringing one of their own.
            object?[] supplied = new object?[ThreadCount];
            object[] results = new object[ThreadCount];
            var threads = new Thread[ThreadCount];

            using var barrier = new Barrier(ThreadCount);

            for (int t = 0; t < threads.Length; t++)
            {
                int index = t;

                // Every other thread brings its own object to register, the rest ask for one to be created.
                supplied[index] = (index % 2) == 0 ? new object() : null;

                threads[t] = new Thread(() =>
                {
                    try
                    {
                        barrier.SignalAndWait(TimeSpan.FromMinutes(1));

                        results[index] = supplied[index] is object toRegister
                            ? cw.GetOrRegisterObjectForComInstance(instanceRaw, CreateObjectFlags.None, toRegister)
                            : cw.GetOrCreateObjectForComInstance(instanceRaw, CreateObjectFlags.None);
                    }
                    catch (Exception e)
                    {
                        failures.Enqueue(e);
                    }
                })
                { IsBackground = true, Name = $"ComWrappers register race {index}" };

                threads[t].Start();
            }

            foreach (Thread thread in threads)
            {
                Assert.True(thread.Join(TimeSpan.FromMinutes(2)), "A worker thread did not finish, which suggests a deadlock while racing to publish.");
            }

            Assert.Empty(failures);

            object winner = results[0];

            foreach (object result in results)
            {
                Assert.Same(winner, result);
            }

            Assert.True(ComWrappers.TryGetComInstance(winner, out IntPtr winnerUnknown));
            Assert.Equal(identity, winnerUnknown);
            Marshal.Release(winnerUnknown);

            // A supplied object that lost has to look like an object that was never handed over at all,
            // and has to still be usable as the wrapper for some other COM instance.
            IntPtr otherRaw = MockReferenceTrackerRuntime.CreateTrackerObject();
            bool reusedOne = false;

            foreach (object? candidate in supplied)
            {
                if (candidate is null || ReferenceEquals(candidate, winner))
                {
                    continue;
                }

                Assert.False(ComWrappers.TryGetComInstance(candidate, out IntPtr loserUnknown));
                Assert.Equal(IntPtr.Zero, loserUnknown);

                if (!reusedOne)
                {
                    Assert.Same(candidate, cw.GetOrRegisterObjectForComInstance(otherRaw, CreateObjectFlags.None, candidate));
                    reusedOne = true;
                }
            }

            Marshal.Release(otherRaw);
            Marshal.Release(identity);
            Marshal.Release(instanceRaw);
        }

        // A registration is rejected when the object handed over is already the RCW for another COM
        // instance. The existing coverage rejects one for a COM instance the cache has never seen; this
        // one rejects it for a COM instance that still has an entry whose RCW has been collected, which
        // is a different path through the cache because the entry is there and has to be taken over.
        [ActiveIssue("Not supported on Mono", TestRuntimes.Mono)]
        [Fact]
        public void ValidateRejectedRegistrationOverDeadEntry()
        {
            Console.WriteLine($"Running {nameof(ValidateRejectedRegistrationOverDeadEntry)}...");

            var cw = new PlainProxyComWrappers();

            IntPtr firstRaw = MockReferenceTrackerRuntime.CreateTrackerObject();
            IntPtr secondRaw = MockReferenceTrackerRuntime.CreateTrackerObject();

            // An RCW that belongs to the first COM instance, so registering it for another is rejected.
            object owned = cw.GetOrCreateObjectForComInstance(firstRaw, CreateObjectFlags.None);

            // Leave a dead entry behind for the second instance: collect its RCW without draining
            // finalizers, so the entry is still there but no longer names anything.
            CreateAndAbandon(cw, secondRaw);

            GC.Collect();

            Assert.Throws<NotSupportedException>(() => cw.GetOrRegisterObjectForComInstance(secondRaw, CreateObjectFlags.None, owned));

            // The rejection must not have disturbed the instance the object does belong to.
            Assert.True(ComWrappers.TryGetComInstance(owned, out IntPtr ownedUnknown));

            Assert.Equal(0, Marshal.QueryInterface(firstRaw, IUnknownVtbl.IID_IUnknown, out IntPtr firstIdentity));
            Assert.Equal(firstIdentity, ownedUnknown);

            Marshal.Release(firstIdentity);
            Marshal.Release(ownedUnknown);

            // And the second instance has to be usable afterwards, rather than left holding whatever the
            // rejected attempt put there.
            object recovered = cw.GetOrCreateObjectForComInstance(secondRaw, CreateObjectFlags.None);

            Assert.NotSame(owned, recovered);
            Assert.Same(recovered, cw.GetOrCreateObjectForComInstance(secondRaw, CreateObjectFlags.None));

            Assert.True(ComWrappers.TryGetComInstance(recovered, out IntPtr recoveredUnknown));
            Marshal.Release(recoveredUnknown);

            GC.KeepAlive(owned);
            GC.KeepAlive(recovered);

            Marshal.Release(secondRaw);
            Marshal.Release(firstRaw);

            [MethodImpl(MethodImplOptions.NoInlining)]
            static void CreateAndAbandon(ComWrappers cw, IntPtr comInstance)
            {
                _ = cw.GetOrCreateObjectForComInstance(comInstance, CreateObjectFlags.None);
            }
        }

        // Hands every caller the one wrapper, so that only one native reference is taken however many
        // threads race. Creating a wrapper per caller and abandoning the losers is not an option here:
        // ITrackerObjectWrapper's finalizer fails the run if its tracker object is still connected, and
        // the winner keeps it connected for as long as the test needs it.
        private sealed class SharedTrackerComWrappers : TestComWrappers
        {
            private readonly Barrier _barrier;
            private readonly object _lock = new();
            private object? _proxy;

            public SharedTrackerComWrappers(Barrier barrier) => _barrier = barrier;

            /// <summary>How many callers reached <see cref="CreateObject"/>, so a test can prove they all raced.</summary>
            public int CreateObjectCount;

            protected override object CreateObject(IntPtr externalComObject, CreateObjectFlags flags)
            {
                Interlocked.Increment(ref CreateObjectCount);

                _barrier.SignalAndWait(TimeSpan.FromMinutes(1));

                lock (_lock)
                {
                    return _proxy ??= base.CreateObject(externalComObject, flags);
                }
            }
        }

        // The concurrent test above uses CreateObjectFlags.None, so the wrapper it builds is not a
        // reference tracker one and putting it in the tracker handle cache does nothing. This runs the
        // same race with tracker objects, where one thread publishes the entry and the rest pick its
        // wrapper up, and then checks what that registration is for: the wrapper has to be walked, or
        // the managed objects the native object is holding are not kept alive through a collection.
        [ActiveIssue("Not supported on Mono", TestRuntimes.Mono)]
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
        public void ValidateCreateObjectConcurrentTrackerRegistration()
        {
            Console.WriteLine($"Running {nameof(ValidateCreateObjectConcurrentTrackerRegistration)}...");

            const int ThreadCount = 8;

            IntPtr trackerObjRaw = MockReferenceTrackerRuntime.CreateTrackerObject();

            using var barrier = new Barrier(ThreadCount);

            var cw = new SharedTrackerComWrappers(barrier);
            var failures = new ConcurrentQueue<Exception>();

            object[] results = new object[ThreadCount];
            var threads = new Thread[ThreadCount];

            for (int t = 0; t < threads.Length; t++)
            {
                int index = t;

                threads[t] = new Thread(() =>
                {
                    try
                    {
                        results[index] = cw.GetOrCreateObjectForComInstance(trackerObjRaw, CreateObjectFlags.TrackerObject);
                    }
                    catch (Exception e)
                    {
                        failures.Enqueue(e);
                    }
                })
                { IsBackground = true, Name = $"ComWrappers tracker {index}" };

                threads[t].Start();
            }

            foreach (Thread thread in threads)
            {
                Assert.True(thread.Join(TimeSpan.FromMinutes(2)), "A worker thread did not finish, which suggests a deadlock while racing to publish.");
            }

            Assert.Empty(failures);

            // Every thread really did miss the cache, so they all raced to publish rather than most of
            // them quietly finding an entry someone else had already added.
            Assert.Equal(ThreadCount, cw.CreateObjectCount);

            // Ownership has been transferred to the wrapper.
            Marshal.Release(trackerObjRaw);

            var trackerObj = (ITrackerObjectWrapper)results[0];

            foreach (object result in results)
            {
                Assert.Same(trackerObj, result);
            }

            Assert.True(ComWrappers.TryGetComInstance(trackerObj, out IntPtr unknown));

            Marshal.Release(unknown);

            // Whichever thread ended up publishing it, the wrapper has to have made it into the tracker
            // handle cache, because that is what the runtime walks. If it had not, the managed objects
            // reachable only through the native object would not be reported and would not survive here.
            var testWrapperIds = new List<int>();

            for (int i = 0; i < 1000; ++i)
            {
                IntPtr testWrapper = cw.GetOrCreateComInterfaceForObject(new Test(), CreateComInterfaceFlags.TrackerSupport);

                testWrapperIds.Add(trackerObj.AddObjectRef(testWrapper));

                Marshal.Release(testWrapper);
            }

            ForceGC();

            Assert.True(testWrapperIds.Count <= Test.InstanceCount);

            foreach (int id in testWrapperIds)
            {
                trackerObj.DropObjectRef(id);
            }

            testWrapperIds.Clear();

            ForceGC();

            GC.KeepAlive(trackerObj);
        }

        // Hands every caller the same object, and holds them all inside 'CreateObject' until they have
        // all arrived, so that they are guaranteed to have missed the cache and to be racing to publish.
        private sealed unsafe class SharedProxyComWrappers : ComWrappers
        {
            private readonly Barrier _barrier;

            public SharedProxyComWrappers(Barrier barrier) => _barrier = barrier;

            public object Proxy { get; } = new();

            /// <summary>How many callers reached <see cref="CreateObject"/>, so a test can prove they all raced.</summary>
            public int CreateObjectCount;

            protected override ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
            {
                count = 0;
                return null;
            }

            protected override object CreateObject(IntPtr externalComObject, CreateObjectFlags flags)
            {
                Interlocked.Increment(ref CreateObjectCount);

                // Bounded rather than infinite so that a failure shows up as a failed assertion on the
                // count below rather than as a hung test run.
                _barrier.SignalAndWait(TimeSpan.FromMinutes(1));

                return Proxy;
            }

            protected override void ReleaseObjects(IEnumerable objects) => throw new NotImplementedException();
        }

        // Several threads can all miss the cache for one COM instance and all call 'CreateObject', and an
        // implementation is free to hand each of them the same object. Only one of those threads publishes
        // it and the rest release the wrapper they built, so this checks that losing that race leaves the
        // object usable and still mapped to the COM instance it was created for.
        [ActiveIssue("Not supported on Mono", TestRuntimes.Mono)]
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
        public void ValidateCreateObjectRaceReturningSameObject()
        {
            Console.WriteLine($"Running {nameof(ValidateCreateObjectRaceReturningSameObject)}...");

            const int ThreadCount = 4;

            IntPtr trackerObjRaw = MockReferenceTrackerRuntime.CreateTrackerObject();

            // The cache is keyed on the identity IUnknown, which is what this round trip produces.
            Assert.Equal(0, Marshal.QueryInterface(trackerObjRaw, IUnknownVtbl.IID_IUnknown, out IntPtr identity));

            using var barrier = new Barrier(ThreadCount);

            var cw = new SharedProxyComWrappers(barrier);
            var failures = new ConcurrentQueue<Exception>();

            object[] results = new object[ThreadCount];
            var threads = new Thread[ThreadCount];

            for (int t = 0; t < threads.Length; t++)
            {
                int index = t;

                threads[t] = new Thread(() =>
                {
                    try
                    {
                        results[index] = cw.GetOrCreateObjectForComInstance(trackerObjRaw, CreateObjectFlags.None);
                    }
                    catch (Exception e)
                    {
                        failures.Enqueue(e);
                    }
                })
                { IsBackground = true, Name = $"ComWrappers shared proxy {index}" };

                threads[t].Start();
            }

            foreach (Thread thread in threads)
            {
                Assert.True(thread.Join(TimeSpan.FromMinutes(2)), "A worker thread did not finish, which suggests a deadlock while racing to publish.");
            }

            Assert.Empty(failures);

            // Every thread really did miss the cache and go through 'CreateObject', so they all raced to
            // publish rather than most of them quietly finding an entry someone else had already added.
            Assert.Equal(ThreadCount, cw.CreateObjectCount);

            // Winning or losing the race, every thread is handed the one object 'CreateObject' returned.
            foreach (object result in results)
            {
                Assert.Same(cw.Proxy, result);
            }

            // Losing the race releases a wrapper, and it has to be the loser's rather than the published
            // one, so the object still has to round trip back to the instance it was created for.
            Assert.True(ComWrappers.TryGetComInstance(cw.Proxy, out IntPtr unknown));
            Assert.Equal(identity, unknown);
            Marshal.Release(unknown);

            // And the cache still has to hand out that same object afterwards.
            Assert.Same(cw.Proxy, cw.GetOrCreateObjectForComInstance(trackerObjRaw, CreateObjectFlags.None));

            Marshal.Release(identity);
            Marshal.Release(trackerObjRaw);
        }

        // Hands every caller a distinct object and keeps all of them, so a test can inspect the ones that
        // lost the race to publish. Callers are held on a barrier so they are all guaranteed to have missed
        // the cache and to be racing.
        private sealed unsafe class DistinctProxyComWrappers : ComWrappers
        {
            private readonly Barrier _barrier;

            public DistinctProxyComWrappers(Barrier barrier) => _barrier = barrier;

            public ConcurrentQueue<object> Created { get; } = new();

            protected override ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
            {
                count = 0;
                return null;
            }

            protected override object CreateObject(IntPtr externalComObject, CreateObjectFlags flags)
            {
                _barrier.SignalAndWait(TimeSpan.FromMinutes(1));

                object proxy = new();

                Created.Enqueue(proxy);

                return proxy;
            }

            protected override void ReleaseObjects(IEnumerable objects) => throw new NotImplementedException();
        }

        // Only one of the objects 'CreateObject' produces for a COM instance can be published, and the
        // wrappers built for the others are released. Nothing may be left behind for those objects: an
        // implementation is free to keep hold of everything it returned, and the ones that lost have to
        // behave exactly like objects that were never handed to ComWrappers at all.
        [ActiveIssue("Not supported on Mono", TestRuntimes.Mono)]
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
        public void ValidateCreateObjectRaceLeavesNothingBehindForLosers()
        {
            Console.WriteLine($"Running {nameof(ValidateCreateObjectRaceLeavesNothingBehindForLosers)}...");

            const int ThreadCount = 4;

            IntPtr trackerObjRaw = MockReferenceTrackerRuntime.CreateTrackerObject();

            Assert.Equal(0, Marshal.QueryInterface(trackerObjRaw, IUnknownVtbl.IID_IUnknown, out IntPtr identity));

            using var barrier = new Barrier(ThreadCount);

            var cw = new DistinctProxyComWrappers(barrier);
            var failures = new ConcurrentQueue<Exception>();

            object[] results = new object[ThreadCount];
            var threads = new Thread[ThreadCount];

            for (int t = 0; t < threads.Length; t++)
            {
                int index = t;

                threads[t] = new Thread(() =>
                {
                    try
                    {
                        results[index] = cw.GetOrCreateObjectForComInstance(trackerObjRaw, CreateObjectFlags.None);
                    }
                    catch (Exception e)
                    {
                        failures.Enqueue(e);
                    }
                })
                { IsBackground = true, Name = $"ComWrappers distinct proxy {index}" };

                threads[t].Start();
            }

            foreach (Thread thread in threads)
            {
                Assert.True(thread.Join(TimeSpan.FromMinutes(2)), "A worker thread did not finish, which suggests a deadlock while racing to publish.");
            }

            Assert.Empty(failures);

            Assert.Equal(ThreadCount, cw.Created.Count);

            object winner = results[0];

            // Everyone gets the one object that was published, whichever thread produced it.
            foreach (object result in results)
            {
                Assert.Same(winner, result);
            }

            Assert.True(ComWrappers.TryGetComInstance(winner, out IntPtr winnerUnknown));
            Assert.Equal(identity, winnerUnknown);
            Marshal.Release(winnerUnknown);

            int winners = 0;

            foreach (object created in cw.Created)
            {
                if (ReferenceEquals(created, winner))
                {
                    winners++;
                    continue;
                }

                // A loser must look like an ordinary managed object. If a wrapper were left registered for
                // it, these would throw instead, because releasing a wrapper zeroes the COM pointer that
                // both of these paths hand to 'Marshal.QueryInterface'.
                Assert.False(ComWrappers.TryGetComInstance(created, out IntPtr loserUnknown));
                Assert.Equal(IntPtr.Zero, loserUnknown);

                _ = new WeakReference<object>(created);
            }

            Assert.Equal(1, winners);

            // And a loser must still be usable as the wrapper for some other COM instance, rather than
            // being permanently associated with the one it lost the race for.
            IntPtr otherObjRaw = MockReferenceTrackerRuntime.CreateTrackerObject();

            foreach (object created in cw.Created)
            {
                if (ReferenceEquals(created, winner))
                {
                    continue;
                }

                Assert.Same(created, cw.GetOrRegisterObjectForComInstance(otherObjRaw, CreateObjectFlags.None, created));
                break;
            }

            Marshal.Release(otherObjRaw);
            Marshal.Release(identity);
            Marshal.Release(trackerObjRaw);
        }

        // Verify that if a GC nulls the contents of a weak GCHandle but has not yet
        // run finializers to remove that GCHandle from the cache, the state of the system is valid.
        [ActiveIssue("Not supported on Mono", TestRuntimes.Mono)]
        [Fact]
        public void ValidateCreateObjectWeakHandleCacheCleanUp()
        {
            Console.WriteLine($"Running {nameof(ValidateCreateObjectWeakHandleCacheCleanUp)}...");

            var cw = new TestComWrappers();

            // Get an object from a tracker runtime.
            IntPtr trackerObjRaw = MockReferenceTrackerRuntime.CreateTrackerObject();

            // Create the first native object wrapper and run the GC.
            CreateObject(cw, trackerObjRaw);

            // Only attempt to run the GC, don't wait for the finalizer. We do this
            // because of the multiple phase clean-up for ComWrappers caches.
            // See weak GC handles in the NativeAOT scenario.
            GC.Collect();

            // Try to create another wrapper for the same object. The above GC
            // may have collected parts of the ComWrapper cache, but not fully
            // cleared the contents of the cache.
            CreateObject(cw, trackerObjRaw);
            ForceGC();

            Marshal.Release(trackerObjRaw);

            [MethodImpl(MethodImplOptions.NoInlining)]
            static void CreateObject(ComWrappers cw, IntPtr trackerObj)
            {
                var obj = (ITrackerObjectWrapper)cw.GetOrCreateObjectForComInstance(trackerObj, CreateObjectFlags.None);
                Assert.NotNull(obj);
            }
        }

        // A dead cache entry is replaced in place by the next RCW created for the same COM instance, so for a
        // while the entry under that key belongs to a different wrapper than the one that is about to finalize.
        // The old wrapper must remove only the entry it published, and leave the replacement in place.
        [ActiveIssue("Not supported on Mono", TestRuntimes.Mono)]
        [Fact]
        public void ValidateReplacedCacheEntrySurvivesOldWrapperCleanUp()
        {
            Console.WriteLine($"Running {nameof(ValidateReplacedCacheEntrySurvivesOldWrapperCleanUp)}...");

            var cw = new TestComWrappers();

            IntPtr trackerObjRaw = MockReferenceTrackerRuntime.CreateTrackerObject();

            CreateAndAbandonWrapper(cw, trackerObjRaw);

            // Collect without draining finalizers, so the entry goes dead while the wrapper that owns it has
            // most likely not run its finalizer yet.
            GC.Collect();

            // This takes over the dead entry, replacing the handle stored under that key.
            var replacement = (ITrackerObjectWrapper)cw.GetOrCreateObjectForComInstance(trackerObjRaw, CreateObjectFlags.None);
            Assert.NotNull(replacement);

            // Now let the abandoned wrapper finalize and release, which removes its own cache entry.
            ForceGC();

            // The replacement is still alive, so it has to still be cached.
            var lookup = (ITrackerObjectWrapper)cw.GetOrCreateObjectForComInstance(trackerObjRaw, CreateObjectFlags.None);
            Assert.Same(replacement, lookup);

            // It also has to still resolve back to the COM instance it wraps.
            Assert.True(ComWrappers.TryGetComInstance(replacement, out IntPtr unknown));
            Marshal.Release(unknown);

            GC.KeepAlive(replacement);

            Marshal.Release(trackerObjRaw);

            [MethodImpl(MethodImplOptions.NoInlining)]
            static void CreateAndAbandonWrapper(ComWrappers cw, IntPtr instance)
            {
                var obj = (ITrackerObjectWrapper)cw.GetOrCreateObjectForComInstance(instance, CreateObjectFlags.None);
                Assert.NotNull(obj);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [ActiveIssue("Not supported on Mono", TestRuntimes.Mono)]
        [Fact]
        public void ValidateMappingAPIs()
        {
            Console.WriteLine($"Running {nameof(ValidateMappingAPIs)}...");

            var cw = new TestComWrappers();

            // Allocate a wrapper for the managed instance
            var managedObj = new Test();
            IntPtr managedWrapper = cw.GetOrCreateComInterfaceForObject(managedObj, CreateComInterfaceFlags.None);
            Assert.NotEqual(IntPtr.Zero, managedWrapper);

            // Allocate wrapper with user defined IUnknown.
            // Using a new ComWrappers instance because
            // a native wrapper for a managed object is associated
            // with its allocating ComWrappers instance.
            var cwAlt = new TestComWrappers();
            IntPtr managedWrapper2 = cwAlt.GetOrCreateComInterfaceForObject(managedObj, CreateComInterfaceFlags.CallerDefinedIUnknown);
            Assert.NotEqual(IntPtr.Zero, managedWrapper2);

            // Create a wrapper for the unmanaged instance
            IntPtr unmanagedObj = MockReferenceTrackerRuntime.CreateTrackerObject();
            Assert.Equal(0, Marshal.QueryInterface(unmanagedObj, IUnknownVtbl.IID_IUnknown, out IntPtr unmanagedObjIUnknown));
            var unmanagedWrapper = cw.GetOrCreateObjectForComInstance(unmanagedObj, CreateObjectFlags.None);

            // Also allocate a unique instance to validate looking from an uncached instance
            var unmanagedWrapperUnique = cw.GetOrCreateObjectForComInstance(unmanagedObj, CreateObjectFlags.UniqueInstance);

            // Verify TryGetObject
            Assert.True(ComWrappers.TryGetObject(managedWrapper, out object managedObjOther));
            Assert.Equal(managedObj, managedObjOther);
            Assert.True(ComWrappers.TryGetObject(managedWrapper2, out object managedObjOther2));
            Assert.Equal(managedObj, managedObjOther2);
            Assert.False(ComWrappers.TryGetObject(unmanagedObj, out object _));

            // Verify TryGetComInstance
            Assert.False(ComWrappers.TryGetComInstance(managedObj, out IntPtr _));
            Assert.True(ComWrappers.TryGetComInstance(unmanagedWrapper, out IntPtr unmanagedObjOther));
            Assert.True(ComWrappers.TryGetComInstance(unmanagedWrapperUnique, out IntPtr unmanagedObjOtherUnique));
            Assert.Equal(unmanagedObjIUnknown, unmanagedObjOther);
            Assert.Equal(unmanagedObjIUnknown, unmanagedObjOtherUnique);
            Marshal.Release(unmanagedObjOther);
            Marshal.Release(unmanagedObjOtherUnique);

            // Release unmanaged resources
            int count = Marshal.Release(managedWrapper);
            Assert.Equal(0, count);
            count = Marshal.Release(managedWrapper2);
            Assert.Equal(0, count);
            Marshal.Release(unmanagedObj);
            Marshal.Release(unmanagedObjIUnknown);
        }

        class Resurrecter()
        {
            public ManualReleaseITestObjectWrapper? UnmanagedWrapper;

            ~Resurrecter()
            {
                if (UnmanagedWrapper != null)
                {
                    GC.ReRegisterForFinalize(this);
                }
            }
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        [ActiveIssue("Not supported on Mono", TestRuntimes.Mono)]
        [Fact]
        public void ValidateNativeObjectWrapperResurrection()
        {
            Console.WriteLine($"Running {nameof(ValidateNativeObjectWrapperResurrection)}...");

            var cw = new TestComWrappers()
            {
                UseManualReleaseITestObjectWrapper = true,
            };

            WeakGCHandle<Resurrecter> resurrecter;
            nint unmanagedObj = AllocateWrapper(cw, out resurrecter);
            Assert.Equal(0, Marshal.QueryInterface(unmanagedObj, IUnknownVtbl.IID_IUnknown, out IntPtr unmanagedObjIUnknown));
            ForceGC();
            AssertNativeObjectWrapperAlive(cw, resurrecter, unmanagedObjIUnknown);

            resurrecter.Dispose();
            Marshal.Release(unmanagedObjIUnknown);
            Assert.Equal(0, Marshal.Release(unmanagedObj));

            [MethodImpl(MethodImplOptions.NoInlining)]
            static nint AllocateWrapper(ComWrappers cw, out WeakGCHandle<Resurrecter> handle)
            {
                Test test = new();
                nint comWrapper = cw.GetOrCreateComInterfaceForObject(test, CreateComInterfaceFlags.None);
                Assert.NotEqual(IntPtr.Zero, comWrapper);

                var unmanagedWrapper = (ManualReleaseITestObjectWrapper)cw.GetOrCreateObjectForComInstance(comWrapper, CreateObjectFlags.UniqueInstance);
                Resurrecter resurrecter = new()
                {
                    UnmanagedWrapper = unmanagedWrapper,
                };
                handle = new WeakGCHandle<Resurrecter>(resurrecter, true);
                return comWrapper;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static void AssertNativeObjectWrapperAlive(ComWrappers cw, WeakGCHandle<Resurrecter> handle, IntPtr unmanagedObj)
            {
                Assert.True(handle.TryGetTarget(out Resurrecter resurrecter));
                ManualReleaseITestObjectWrapper? unmanagedWrapper = resurrecter.UnmanagedWrapper;
                Assert.NotNull(resurrecter);
                Assert.True(ComWrappers.TryGetComInstance(unmanagedWrapper, out IntPtr unmanagedObjOther));
                Assert.Equal(unmanagedObj, unmanagedObjOther);
                resurrecter.UnmanagedWrapper = null;
                Marshal.Release(unmanagedObjOther);
                unmanagedWrapper.FinalRelease();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [ActiveIssue("Not supported on Mono", TestRuntimes.Mono)]
        [Fact]
        public void ValidateWrappersInstanceIsolation()
        {
            Console.WriteLine($"Running {nameof(ValidateWrappersInstanceIsolation)}...");

            var cw1 = new TestComWrappers();
            var cw2 = new TestComWrappers();

            var testObj = new Test();

            // Allocate a wrapper for the object
            IntPtr comWrapper1 = cw1.GetOrCreateComInterfaceForObject(testObj, CreateComInterfaceFlags.TrackerSupport);
            IntPtr comWrapper2 = cw2.GetOrCreateComInterfaceForObject(testObj, CreateComInterfaceFlags.TrackerSupport);
            Assert.NotEqual(comWrapper1, IntPtr.Zero);
            Assert.NotEqual(comWrapper2, IntPtr.Zero);
            Assert.NotEqual(comWrapper1, comWrapper2);

            IntPtr comWrapper3 = cw1.GetOrCreateComInterfaceForObject(testObj, CreateComInterfaceFlags.TrackerSupport);
            IntPtr comWrapper4 = cw2.GetOrCreateComInterfaceForObject(testObj, CreateComInterfaceFlags.TrackerSupport);
            Assert.NotEqual(comWrapper3, comWrapper4);
            Assert.Equal(comWrapper1, comWrapper3);
            Assert.Equal(comWrapper2, comWrapper4);

            Marshal.Release(comWrapper1);
            Marshal.Release(comWrapper2);
            Marshal.Release(comWrapper3);
            Marshal.Release(comWrapper4);

            // Get an object from a tracker runtime.
            IntPtr trackerObjRaw = MockReferenceTrackerRuntime.CreateTrackerObject();

            // Create objects for the COM instance
            var trackerObj1 = (ITrackerObjectWrapper)cw1.GetOrCreateObjectForComInstance(trackerObjRaw, CreateObjectFlags.TrackerObject);
            var trackerObj2 = (ITrackerObjectWrapper)cw2.GetOrCreateObjectForComInstance(trackerObjRaw, CreateObjectFlags.TrackerObject);
            Assert.NotEqual(trackerObj1, trackerObj2);

            var trackerObj3 = (ITrackerObjectWrapper)cw1.GetOrCreateObjectForComInstance(trackerObjRaw, CreateObjectFlags.TrackerObject);
            var trackerObj4 = (ITrackerObjectWrapper)cw2.GetOrCreateObjectForComInstance(trackerObjRaw, CreateObjectFlags.TrackerObject);
            Assert.NotEqual(trackerObj3, trackerObj4);
            Assert.Equal(trackerObj1, trackerObj3);
            Assert.Equal(trackerObj2, trackerObj4);

            Marshal.Release(trackerObjRaw);
        }

        [ActiveIssue("Not supported on Mono", TestRuntimes.Mono)]
        [Fact]
        public void ValidatePrecreatedExternalWrapper()
        {
            Console.WriteLine($"Running {nameof(ValidatePrecreatedExternalWrapper)}...");

            var cw = new TestComWrappers();

            // Get an object from a tracker runtime.
            IntPtr trackerObjRaw = MockReferenceTrackerRuntime.CreateTrackerObject();

            // Manually create a wrapper
            IntPtr iTestComObject;
            int hr = Marshal.QueryInterface(trackerObjRaw, typeof(ITrackerObject).GUID, out iTestComObject);
            Assert.Equal(0, hr);
            var nativeWrapper = new ITrackerObjectWrapper(iTestComObject);

            // Register wrapper, but supply the wrapper.
            var nativeWrapper2 = (ITrackerObjectWrapper)cw.GetOrRegisterObjectForComInstance(trackerObjRaw, CreateObjectFlags.TrackerObject, nativeWrapper);
            Assert.Equal(nativeWrapper, nativeWrapper2);

            // Ownership has been transferred to the wrapper.
            Marshal.Release(trackerObjRaw);

            // Validate reuse of a wrapper fails.
            IntPtr trackerObjRaw2 = MockReferenceTrackerRuntime.CreateTrackerObject();
            Assert.Throws<NotSupportedException>(
                () =>
                {
                    cw.GetOrRegisterObjectForComInstance(trackerObjRaw2, CreateObjectFlags.None, nativeWrapper2);
                });

            // The rejected registration must not leave anything behind for that COM instance, so creating a
            // wrapper for it now has to work and has to produce a usable object.
            var recovered = (ITrackerObjectWrapper)cw.GetOrCreateObjectForComInstance(trackerObjRaw2, CreateObjectFlags.None);
            Assert.NotNull(recovered);
            Assert.NotEqual(nativeWrapper2, recovered);

            // Asking again returns the same wrapper, which confirms the entry that was just created is the live
            // one rather than something left over from the rejected attempt.
            var recoveredAgain = (ITrackerObjectWrapper)cw.GetOrCreateObjectForComInstance(trackerObjRaw2, CreateObjectFlags.None);
            Assert.Equal(recovered, recoveredAgain);

            Marshal.Release(trackerObjRaw2);

            // Validate passing null wrapper fails.
            Assert.Throws<ArgumentNullException>(
                () =>
                {
                    cw.GetOrRegisterObjectForComInstance(trackerObjRaw, CreateObjectFlags.None, null);
                });
        }

        [ActiveIssue("Not supported on Mono", TestRuntimes.Mono)]
        [Fact]
        public void ValidateExternalWrapperCacheCleanUp()
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
            Assert.True(weakRef1.TryGetTarget(out ITrackerObjectWrapper wrapper1));
            Assert.True(weakRef2.TryGetTarget(out ITrackerObjectWrapper wrapper2));

            // Check that the two wrappers aren't equal, meaning we created a new wrapper since
            // the first wrapper was removed from the internal cache.
            Assert.NotEqual(wrapper1, wrapper2);

            // Let the wrappers Finalize.
            wrapper1.ReregisterForFinalize = false;
            wrapper2.ReregisterForFinalize = false;

            static WeakReference<ITrackerObjectWrapper> CreateAndRegisterWrapper(ComWrappers cw, IntPtr trackerObjRaw)
            {
                // Manually create a wrapper
                IntPtr iTestComObject;
                int hr = Marshal.QueryInterface(trackerObjRaw, typeof(ITrackerObject).GUID, out iTestComObject);
                Assert.Equal(0, hr);
                var nativeWrapper = new ITrackerObjectWrapper(iTestComObject);

                nativeWrapper = (ITrackerObjectWrapper)cw.GetOrRegisterObjectForComInstance(trackerObjRaw, CreateObjectFlags.None, nativeWrapper);

                // Set this on the return instead of during creation since the returned wrapper may be the one from
                // the internal cache and not the one passed in above.
                nativeWrapper.ReregisterForFinalize = true;

                return new WeakReference<ITrackerObjectWrapper>(nativeWrapper, trackResurrection: true);
            }
        }

        // Every other test in this file uses an RCW type that has a finalizer, and a wrapper allocates a
        // second GC handle for those. An RCW with no finalizer gets a single handle that tracks
        // resurrection instead, so its cache entries go dead at a different point in a collection than
        // the ones covered above.
        [ActiveIssue("Not supported on Mono", TestRuntimes.Mono)]
        [Fact]
        public void ValidateExternalWrapperCacheCleanUpWithoutFinalizer()
        {
            Console.WriteLine($"Running {nameof(ValidateExternalWrapperCacheCleanUpWithoutFinalizer)}...");

            var cw = new TestComWrappers()
            {
                UseManualReleaseITestObjectWrapper = true,
            };

            var test = new Test();

            IntPtr comWrapper = cw.GetOrCreateComInterfaceForObject(test, CreateComInterfaceFlags.None);

            Assert.NotEqual(IntPtr.Zero, comWrapper);

            WeakReference<object> first = CreateAndAbandonWrapper(cw, comWrapper);

            // Collect without draining finalizers, so the entry is dead but the wrapper that owns it has
            // not run yet and so has not removed it. Creating again has to see through the dead entry.
            GC.Collect();

            // This RCW type has no finalizer, so a single collection is enough for it to be gone. That is
            // the property this test exists for: such an RCW gets one handle that tracks resurrection, and
            // the cache holds that handle, so the entry dies here rather than after finalization.
            Assert.False(first.TryGetTarget(out _));

            WeakReference<object> second = CreateAndAbandonWrapper(cw, comWrapper);

            // The dead entry did not prevent a new RCW being created and cached for the same instance.
            // Checked in a separate frame so that the strong reference it needs does not outlive it and
            // keep the RCW alive through the collection below.
            AssertUsableAndDrop(second);

            // Now let the wrapper finalizers run, which is what removes entries, and check the cache is
            // still able to hand out a working RCW afterwards.
            ForceGC();

            Assert.False(second.TryGetTarget(out _));

            var third = (ManualReleaseITestObjectWrapper)cw.GetOrCreateObjectForComInstance(comWrapper, CreateObjectFlags.None);

            Assert.True(ComWrappers.TryGetComInstance(third, out IntPtr unknown));

            Assert.Equal(0, Marshal.QueryInterface(comWrapper, IUnknownVtbl.IID_IUnknown, out IntPtr identity));
            Assert.Equal(identity, unknown);

            Marshal.Release(identity);
            Marshal.Release(unknown);

            third.FinalRelease();

            GC.KeepAlive(test);

            Marshal.Release(comWrapper);

            [MethodImpl(MethodImplOptions.NoInlining)]
            static void AssertUsableAndDrop(WeakReference<object> reference)
            {
                Assert.True(reference.TryGetTarget(out object? target));
                Assert.True(ComWrappers.TryGetComInstance(target, out IntPtr unknown));

                Marshal.Release(unknown);
            }

            // This wrapper type releases the interface pointer its constructor took by hand rather than
            // from a finalizer, so it is released here before the wrapper is dropped.
            [MethodImpl(MethodImplOptions.NoInlining)]
            static WeakReference<object> CreateAndAbandonWrapper(ComWrappers cw, IntPtr comWrapper)
            {
                var wrapper = (ManualReleaseITestObjectWrapper)cw.GetOrCreateObjectForComInstance(comWrapper, CreateObjectFlags.None);

                wrapper.FinalRelease();

                return new WeakReference<object>(wrapper);
            }
        }

        [ActiveIssue("Not supported on Mono", TestRuntimes.Mono)]
        [Fact]
        public void ValidateSuppliedInnerNotAggregation()
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

        [ActiveIssue("Not supported on Mono", TestRuntimes.Mono)]
        [Fact]
        public void ValidateIUnknownImpls()
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
                        throw new UnreachableException();
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
                        throw new UnreachableException();
                }
            }

            protected override void ReleaseObjects(IEnumerable objects)
            {
                throw new NotSupportedException();
            }
        }

        [ActiveIssue("Not supported on Mono", TestRuntimes.Mono)]
        [Fact]
        public void ValidateBadComWrapperImpl()
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
                Assert.Equal(BadComWrappers.ExceptionErrorCode, e.HResult);
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
                Assert.Equal(BadComWrappers.ExceptionErrorCode, e.HResult);
            }

            Marshal.Release(trackerObjRaw);
        }

        private void ValidateRuntimeTrackerScenarioCore(ComWrappers cw, Func<IntPtr, object> createObjectFunc)
        {
            // Get an object from a tracker runtime.
            IntPtr trackerObjRaw = MockReferenceTrackerRuntime.CreateTrackerObject();

            // Create a managed wrapper for the native object.
            var trackerObj = (ITrackerObjectWrapper)createObjectFunc(trackerObjRaw);

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

            Assert.True(testWrapperIds.Count <= Test.InstanceCount);

            ForceGC();

            Assert.True(testWrapperIds.Count <= Test.InstanceCount);

            // Remove the managed object ref from the native object.
            foreach (int id in testWrapperIds)
            {
                trackerObj.DropObjectRef(id);
            }

            testWrapperIds.Clear();

            ForceGC();
        }

        [ActiveIssue("Not supported on Mono", TestRuntimes.Mono)]
        [Fact]
        public void ValidateRuntimeTrackerScenario()
        {
            Console.WriteLine($"Running {nameof(ValidateRuntimeTrackerScenario)}...");

            var cw = new TestComWrappers();

            ValidateRuntimeTrackerScenarioCore(cw, (trackerObjRaw) =>
            {
                return cw.GetOrCreateObjectForComInstance(trackerObjRaw, CreateObjectFlags.TrackerObject);
            });
        }

        [ActiveIssue("Not supported on Mono", TestRuntimes.Mono)]
        [Fact]
        public void ValidateRuntimeTrackerScenarioUserStateOverload()
        {
            Console.WriteLine($"Running {nameof(ValidateRuntimeTrackerScenarioUserStateOverload)}...");

            var cw = new TestComWrappers();

            ValidateRuntimeTrackerScenarioCore(cw, (trackerObjRaw) =>
            {
                return cw.GetOrCreateObjectForComInstance(trackerObjRaw, CreateObjectFlags.None, userState: null);
            });
        }

        [ActiveIssue("Not supported on Mono", TestRuntimes.Mono)]
        [Fact]
        public void ValidateQueryInterfaceAfterManagedObjectCollected()
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
                Assert.Equal(0, refCount);
            }

            ForceGC();

            // Calling QueryInterface on an IReferenceTrackerTarget instance is permitted when
            // the wrapper lifetime has been extended. However, the QueryInterface may fail
            // if the associated managed object was collected. The failure here is an important
            // part of the contract for a Reference Tracker runtime.
            IntPtr iTestComObject;
            int hr = Marshal.QueryInterface(refTrackerTarget, typeof(ITest).GUID, out iTestComObject);

            const int COR_E_ACCESSING_CCW = unchecked((int)0x80131544);
            Assert.Equal(COR_E_ACCESSING_CCW, hr);

            // Release the IReferenceTrackerTarget instance.
            refCount = MockReferenceTrackerRuntime.TrackerTarget_ReleaseFromReferenceTracker(refTrackerTarget);
            Assert.Equal(0, refCount);

            // Inlining this method could unintentionally extend the lifetime of
            // the Test instance. This lifetime extension makes clean-up of the CCW
            // impossible when desired because the GC sees the object as reachable.
            [MethodImpl(MethodImplOptions.NoInlining)]
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
                Marshal.Release(testWrapper);

                // Tell the tracker runtime to release its hold on the base instance.
                MockReferenceTrackerRuntime.ReleaseAllTrackerObjects();

                // Validate the GC is tracking the entire Derived type.
                ForceGC();

                derived.DropObjectRef(id);

                return new WeakReference<Derived>(derived);
            }
        }

        [ActiveIssue("Not supported on Mono", TestRuntimes.Mono)]
        [Fact]
        public void ValidateAggregationWithComObject()
        {
            Console.WriteLine($"Running {nameof(ValidateAggregationWithComObject)}...");

            using var allocTracker = MockReferenceTrackerRuntime.CountTrackerObjectAllocations();
            var cw = new TestComWrappers();
            WeakReference<Derived> weakRef = Derived.AllocateAndUseBaseType(cw, aggregateRefTracker: false);

            ForceGC();

            // Validate all instances were cleaned up
            Assert.False(weakRef.TryGetTarget(out _));
            Assert.Equal(0, allocTracker.GetCount());
        }

        [ActiveIssue("Not supported on Mono", TestRuntimes.Mono)]
        [Fact]
        public void ValidateAggregationWithReferenceTrackerObject()
        {
            Console.WriteLine($"Running {nameof(ValidateAggregationWithReferenceTrackerObject)}...");

            using var allocTracker = MockReferenceTrackerRuntime.CountTrackerObjectAllocations();
            var cw = new TestComWrappers();
            WeakReference<Derived> weakRef = Derived.AllocateAndUseBaseType(cw, aggregateRefTracker: true);

            ForceGC();

            // Validate all instances were cleaned up.
            Assert.False(weakRef.TryGetTarget(out _));

            // Reference counter cleanup requires additional GCs since the Finalizer is used
            // to clean up the Reference Tracker runtime references.
            ForceGC();

            Assert.Equal(0, allocTracker.GetCount());
        }

        [ActiveIssue("Not supported on Mono", TestRuntimes.Mono)]
        [Fact]
        public void ComWrappersNoLockAroundQueryInterface()
        {
            Console.WriteLine($"Running {nameof(ComWrappersNoLockAroundQueryInterface)}...");

            var cw = new RecursiveSimpleComWrappers();
            var managedObject = new RecursiveCrossThreadQI(cw);

            IntPtr comObject = cw.GetOrCreateComInterfaceForObject(managedObject, CreateComInterfaceFlags.None);
            try
            {
                // The nested call has to use this same COM instance. The RCW cache is partitioned into buckets
                // keyed off the COM instance, so using a different instance would only exercise the same lock by
                // chance, and the test would no longer reliably catch a regression.
                managedObject.NestedComObject = comObject;

                _ = cw.GetOrCreateObjectForComInstance(comObject, CreateObjectFlags.TrackerObject);
            }
            finally
            {
                Marshal.Release(comObject);
            }

            Assert.True(managedObject.NestedCallCompleted);
        }

        private class RecursiveCrossThreadQI(ComWrappers wrappers) : ICustomQueryInterface
        {
            public IntPtr NestedComObject { get; set; }

            public bool NestedCallCompleted { get; private set; }

            CustomQueryInterfaceResult ICustomQueryInterface.GetInterface(ref Guid iid, out IntPtr ppv)
            {
                ppv = IntPtr.Zero;
                if (iid == ComWrappersHelper.IID_IReferenceTracker)
                {
                    Console.WriteLine("Attempting to create a new COM object on a different thread.");
                    IntPtr nestedComObject = NestedComObject;
                    Thread thread = new Thread(() =>
                    {
                        // Make sure that ComWrappers isn't locking in GetOrCreateObjectForComInstance
                        // around the QI call by calling it on a different thread from within a QI call to register a new managed wrapper
                        // for a COM object representing "this".
                        _ = wrappers.GetOrCreateObjectForComInstance(nestedComObject, CreateObjectFlags.None);
                    });
                    thread.Start();

                    // The result is recorded and asserted by the caller.
                    NestedCallCompleted = thread.Join(TimeSpan.FromSeconds(20)); // 20 seconds should be more than long enough for the thread to complete
                }

                return CustomQueryInterfaceResult.Failed;
            }
        }

        private unsafe class RecursiveSimpleComWrappers : ComWrappers
        {
            protected override ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
            {
                count = 0;
                return null;
            }

            protected override object CreateObject(IntPtr externalComObject, CreateObjectFlags flags)
            {
                return new object();
            }

            protected override void ReleaseObjects(IEnumerable objects)
            {
                throw new NotImplementedException();
            }
        }

        [ActiveIssue("Not supported on Mono", TestRuntimes.Mono)]
        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // COM apartments are Windows-specific
        [Xunit.SkipOnCoreClrAttribute("Depends on marshalled calli", RuntimeTestModes.InterpreterActive)]
        public unsafe void CrossApartmentQueryInterface_NoDeadlock()
        {
            Console.WriteLine($"Running {nameof(CrossApartmentQueryInterface_NoDeadlock)}...");
            using ManualResetEvent hasAgileReference = new(false);
            using ManualResetEvent testCompleted = new(false);

            IntPtr agileReference = IntPtr.Zero;
            try
            {
                Thread staThread = new(() =>
                {
                    var cw = new RecursiveSimpleComWrappers();
                    IntPtr comObject = cw.GetOrCreateComInterfaceForObject(new RecursiveQI(cw), CreateComInterfaceFlags.None);
                    try
                    {
                        Marshal.ThrowExceptionForHR(RoGetAgileReference(0, IUnknownVtbl.IID_IUnknown, comObject, out agileReference));
                    }
                    finally
                    {
                        Marshal.Release(comObject);
                    }
                    hasAgileReference.Set();
                    testCompleted.WaitOne();
                });
                staThread.SetApartmentState(ApartmentState.STA);

                Thread mtaThread = new(() =>
                {
                    hasAgileReference.WaitOne();
                    IntPtr comObject;
                    int hr = ((delegate* unmanaged<IntPtr, in Guid, out IntPtr, int>)(*(*(void***)agileReference + 3 /* IAgileReference.Resolve slot */)))(agileReference, IUnknownVtbl.IID_IUnknown, out comObject);
                    Marshal.ThrowExceptionForHR(hr);
                    try
                    {
                        var cw = new RecursiveSimpleComWrappers();
                        // Make sure that ComWrappers isn't locking in GetOrCreateObjectForComInstance
                        // across the QI call
                        // by forcing marshalling across COM apartments.
                        _ = cw.GetOrCreateObjectForComInstance(comObject, CreateObjectFlags.TrackerObject);
                    }
                    finally
                    {
                        Marshal.Release(comObject);
                    }
                    testCompleted.Set();
                });
                mtaThread.SetApartmentState(ApartmentState.MTA);

                staThread.Start();
                mtaThread.Start();
                testCompleted.WaitOne();
            }
            finally
            {
                if (agileReference != IntPtr.Zero)
                {
                    Marshal.Release(agileReference);
                }
            }
        }

        [DllImport("ole32.dll")]
        private static extern int RoGetAgileReference(int options, in Guid iid, IntPtr unknown, out IntPtr agileReference);

        private class RecursiveQI(ComWrappers? wrappers) : ICustomQueryInterface
        {
            CustomQueryInterfaceResult ICustomQueryInterface.GetInterface(ref Guid iid, out IntPtr ppv)
            {
                ppv = IntPtr.Zero;
                if (wrappers is not null)
                {
                    Console.WriteLine("Attempting to create a new COM object on the same thread.");
                    IntPtr comObject = wrappers.GetOrCreateComInterfaceForObject(new RecursiveQI(null), CreateComInterfaceFlags.None);
                    try
                    {
                        // Make sure that ComWrappers isn't locking in GetOrCreateObjectForComInstance
                        // around the QI call by calling it on a different thread from within a QI call to register a new managed wrapper
                        // for a COM object representing "this".
                        _ = wrappers.GetOrCreateObjectForComInstance(comObject, CreateObjectFlags.None);
                    }
                    finally
                    {
                        Marshal.Release(comObject);
                    }
                }

                return CustomQueryInterfaceResult.NotHandled;
            }
        }

        [ActiveIssue("Not supported on Mono", TestRuntimes.Mono)]
        [Fact]
        public void UserStateOverloadNotCalledWhenNoUserStatePassed()
        {
            Console.WriteLine($"Running {nameof(UserStateOverloadNotCalledWhenNoUserStatePassed)}...");

            var testObj = new Test();

            var wrappers = new TestComWrappers();

            // Allocate a wrapper for the object
            IntPtr comWrapper = wrappers.GetOrCreateComInterfaceForObject(testObj, CreateComInterfaceFlags.None);
            Assert.NotEqual(IntPtr.Zero, comWrapper);

            var testObjFromNative = (ITestObjectWrapper)wrappers.GetOrCreateObjectForComInstance(comWrapper, CreateObjectFlags.None);

            Assert.False(wrappers.CalledUserStateOverload);

            testObjFromNative.FinalRelease();
        }

        [ActiveIssue("Not supported on Mono", TestRuntimes.Mono)]
        [Theory]
        [InlineData(null)]
        [InlineData(1)]
        [InlineData("testString")]
        public void UserStatePassedThrough(object? userState)
        {
            Console.WriteLine($"Running {nameof(UserStatePassedThrough)}...");

            var testObj = new NotWrappedObject();

            var wrappers = new TestComWrappers();

            // Allocate a wrapper for the object
            IntPtr comWrapper = wrappers.GetOrCreateComInterfaceForObject(testObj, CreateComInterfaceFlags.None);
            Assert.NotEqual(IntPtr.Zero, comWrapper);

            var testObjFromNative = (WrappedUserState)wrappers.GetOrCreateObjectForComInstance(comWrapper, CreateObjectFlags.None, userState);

            Assert.True(wrappers.CalledUserStateOverload);
            Assert.Same(userState, testObjFromNative.UserState);

            Assert.False(ComWrappers.TryGetComInstance(testObjFromNative, out _));
        }

        [ActiveIssue("Not supported on Mono", TestRuntimes.Mono)]
        [Fact]
        public void UserStateBaseImplementationThrows()
        {
            Console.WriteLine($"Running {nameof(UserStateBaseImplementationThrows)}...");

            var testObj = new NotWrappedObject();

            var wrappers = new TestComWrappers();

            // Allocate a wrapper for the object
            IntPtr comWrapper = wrappers.GetOrCreateComInterfaceForObject(testObj, CreateComInterfaceFlags.None);
            Assert.NotEqual(IntPtr.Zero, comWrapper);

            wrappers.CallBaseCreateObject = true;

            Assert.Throws<NotImplementedException>(() => wrappers.GetOrCreateObjectForComInstance(comWrapper, CreateObjectFlags.None, userState: null));
        }
    }
}

