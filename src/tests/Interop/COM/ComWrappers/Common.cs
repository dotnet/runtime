// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ComWrappersTests.Common
{
    using System;
    using System.Threading;
    using System.Runtime.InteropServices;

    //
    // Managed object with native wrapper definition.
    //
    [Guid("447BB9ED-DA48-4ABC-8963-5BB5C3E0AA09")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface ITest
    {
        void SetValue(int i);
    }

    class Test : ITest, ICustomQueryInterface
    {
        public static int InstanceCount = 0;

        private int id;
        private int value = -1;
        public Test() { id = Interlocked.Increment(ref InstanceCount); }
        ~Test() { Interlocked.Decrement(ref InstanceCount); id = -1; }

        public void SetValue(int i) => this.value = i;
        public int GetValue() => this.value;

        public bool EnableICustomQueryInterface { get; set; } = false;
        public Guid ICustomQueryInterface_GetInterfaceIID { get; set; }
        public IntPtr ICustomQueryInterface_GetInterfaceResult { get; set; }

        CustomQueryInterfaceResult ICustomQueryInterface.GetInterface(ref Guid iid, out IntPtr ppv)
        {
            ppv = IntPtr.Zero;
            if (!EnableICustomQueryInterface)
            {
                return CustomQueryInterfaceResult.NotHandled;
            }

            if (iid != ICustomQueryInterface_GetInterfaceIID)
            {
                return CustomQueryInterfaceResult.Failed;
            }

            ppv = this.ICustomQueryInterface_GetInterfaceResult;
            return CustomQueryInterfaceResult.Handled;
        }
    }

    public struct IUnknownVtbl
    {
        public static Guid IID_IUnknown = new Guid("00000000-0000-0000-C000-000000000046");

        public IntPtr QueryInterface;
        public IntPtr AddRef;
        public IntPtr Release;
    }

    public struct ITestVtbl
    {
        public IUnknownVtbl IUnknownImpl;
        public IntPtr SetValue;

        public delegate int _SetValue(IntPtr thisPtr, int i);
        public static _SetValue pSetValue = new _SetValue(SetValueInternal);

        public static int SetValueInternal(IntPtr dispatchPtr, int i)
        {
            unsafe
            {
                try
                {
                    ComWrappers.ComInterfaceDispatch.GetInstance<ITest>((ComWrappers.ComInterfaceDispatch*)dispatchPtr).SetValue(i);
                }
                catch (Exception e)
                {
                    return e.HResult;
                }
            }
            return 0; // S_OK;
        }
    }

    public class ITestObjectWrapper : ITest
    {
        private readonly ITestVtbl._SetValue _setValue;
        private readonly IntPtr _ptr;

        public ITestObjectWrapper(IntPtr ptr)
        {
            _ptr = ptr;
            VtblPtr inst = Marshal.PtrToStructure<VtblPtr>(ptr);
            ITestVtbl _vtbl = Marshal.PtrToStructure<ITestVtbl>(inst.Vtbl);
            _setValue = Marshal.GetDelegateForFunctionPointer<ITestVtbl._SetValue>(_vtbl.SetValue);
        }

        ~ITestObjectWrapper()
        {
            if (_ptr != IntPtr.Zero)
            {
                Marshal.Release(_ptr);
            }
        }

        public void SetValue(int i) => _setValue(_ptr, i);
    }

    //
    // Native interface definition with managed wrapper for tracker object
    //
    sealed class MockReferenceTrackerRuntime
    {
        private static readonly ReaderWriterLockSlim AllocLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public static IntPtr CreateTrackerObject()
        {
            IntPtr result = CreateTrackerObject(IntPtr.Zero, out IntPtr inner);
            if (inner != IntPtr.Zero)
            {
                Marshal.Release(inner);
            }
            return result;
        }

        public static IntPtr CreateTrackerObject(IntPtr outer, out IntPtr inner)
        {
            AllocLock.EnterReadLock();
            try
            {
                return CreateTrackerObject_Unsafe(outer, out inner);
            }
            finally
            {
                AllocLock.ExitReadLock();
            }
        }

        [DllImport(nameof(MockReferenceTrackerRuntime))]
        extern private static IntPtr CreateTrackerObject_Unsafe(IntPtr outer, out IntPtr inner);

        public class AllocationCountResult : IDisposable
        {
            private bool isDisposed = false;
            private ReaderWriterLockSlim allocLock;
            public AllocationCountResult(ReaderWriterLockSlim allocLock)
            {
                this.allocLock = allocLock;
                this.allocLock.EnterWriteLock();
                StartTrackerObjectAllocationCount_Unsafe();
            }

            public int GetCount() => StopTrackerObjectAllocationCount_Unsafe();

            void IDisposable.Dispose()
            {
                if (this.isDisposed)
                    return;

                this.allocLock.ExitWriteLock();
                this.isDisposed = true;
            }
        }

        public static AllocationCountResult CountTrackerObjectAllocations()
        {
            return new AllocationCountResult(AllocLock);
        }

        [DllImport(nameof(MockReferenceTrackerRuntime))]
        extern private static void StartTrackerObjectAllocationCount_Unsafe();

        [DllImport(nameof(MockReferenceTrackerRuntime))]
        extern private static int StopTrackerObjectAllocationCount_Unsafe();

        [DllImport(nameof(MockReferenceTrackerRuntime))]
        extern public static void ReleaseAllTrackerObjects();

        [DllImport(nameof(MockReferenceTrackerRuntime))]
        extern public static int Trigger_NotifyEndOfReferenceTrackingOnThread();

        [DllImport(nameof(MockReferenceTrackerRuntime))]
        extern public static IntPtr TrackerTarget_AddRefFromReferenceTrackerAndReturn(IntPtr ptr);

        [DllImport(nameof(MockReferenceTrackerRuntime))]
        extern public static int TrackerTarget_ReleaseFromReferenceTracker(IntPtr ptr);

        // Suppressing the GC transition here as we want to make sure we are in-sync
        // with the GC which is setting the connected value.
        [SuppressGCTransition]
        [DllImport(nameof(MockReferenceTrackerRuntime))]
        extern public static byte IsTrackerObjectConnected(IntPtr instance);

        // API used to wrap a QueryInterface(). This is used for testing
        // scenarios where triggering off of the QueryInterface() slot is
        // done by the runtime.
        [DllImport(nameof(MockReferenceTrackerRuntime))]
        extern public static IntPtr WrapQueryInterface(IntPtr queryInterface);
    }

    [Guid("42951130-245C-485E-B60B-4ED4254256F8")]
    public interface ITrackerObject
    {
        int AddObjectRef(IntPtr obj);
        void DropObjectRef(int id);
    };

    public struct VtblPtr
    {
        public IntPtr Vtbl;
    }

    public class ITrackerObjectWrapper : ITrackerObject, ICustomQueryInterface
    {
        private struct ITrackerObjectWrapperVtbl
        {
            public IntPtr QueryInterface;
            public _AddRef AddRef;
            public _Release Release;
            public _AddObjectRef AddObjectRef;
            public _DropObjectRef DropObjectRef;
        }

        private delegate int _AddRef(IntPtr This);
        private delegate int _Release(IntPtr This);
        private delegate int _AddObjectRef(IntPtr This, IntPtr obj, out int id);
        private delegate int _DropObjectRef(IntPtr This, int id);

        private ComWrappersHelper.ClassNative classNative;

        private readonly ITrackerObjectWrapperVtbl vtable;

        public ITrackerObjectWrapper(IntPtr instancePtr)
        {
            var inst = Marshal.PtrToStructure<VtblPtr>(instancePtr);
            this.vtable = Marshal.PtrToStructure<ITrackerObjectWrapperVtbl>(inst.Vtbl);
            this.classNative.Instance = instancePtr;
            this.classNative.Release = ComWrappersHelper.ReleaseFlags.Instance;
        }

        protected unsafe ITrackerObjectWrapper(ComWrappers cw, bool aggregateRefTracker)
        {
            ComWrappersHelper.Init<ITrackerObjectWrapper>(ref this.classNative, this, aggregateRefTracker, cw, &CreateInstance);

            var inst = Marshal.PtrToStructure<VtblPtr>(this.classNative.Instance);
            this.vtable = Marshal.PtrToStructure<ITrackerObjectWrapperVtbl>(inst.Vtbl);

            static IntPtr CreateInstance(IntPtr outer, out IntPtr inner)
            {
                return MockReferenceTrackerRuntime.CreateTrackerObject(outer, out inner);
            }
        }

        ~ITrackerObjectWrapper()
        {
            if (this.ReregisterForFinalize)
            {
                GC.ReRegisterForFinalize(this);
            }
            else
            {
                byte isConnected = MockReferenceTrackerRuntime.IsTrackerObjectConnected(this.classNative.Instance);
                if (isConnected != 0)
                {
                    throw new Exception("TrackerObject should be disconnected prior to finalization");
                }

                ComWrappersHelper.Cleanup(ref this.classNative);
            }
        }

        public bool ReregisterForFinalize { get; set; } = false;

        public int AddObjectRef(IntPtr obj)
        {
            int id;
            int hr = this.vtable.AddObjectRef(this.classNative.Instance, obj, out id);
            if (hr != 0)
            {
                throw new COMException($"{nameof(AddObjectRef)}", hr);
            }

            return id;
        }

        public void DropObjectRef(int id)
        {
            int hr = this.vtable.DropObjectRef(this.classNative.Instance, id);
            if (hr != 0)
            {
                throw new COMException($"{nameof(DropObjectRef)}", hr);
            }
        }

        CustomQueryInterfaceResult ICustomQueryInterface.GetInterface(ref Guid iid, out IntPtr ppv)
        {
            if (this.classNative.Inner == IntPtr.Zero)
            {
                ppv = IntPtr.Zero;
                return CustomQueryInterfaceResult.NotHandled;
            }

            const int S_OK = 0;
            const int E_NOINTERFACE = unchecked((int)0x80004002);

            int hr = Marshal.QueryInterface(this.classNative.Inner, ref iid, out ppv);
            if (hr == S_OK)
            {
                return CustomQueryInterfaceResult.Handled;
            }

            return hr == E_NOINTERFACE
                ? CustomQueryInterfaceResult.NotHandled
                : CustomQueryInterfaceResult.Failed;
        }
    }

    class ComWrappersHelper
    {
        private static Guid IID_IReferenceTracker = new Guid("11d3b13a-180e-4789-a8be-7712882893e6");

        [Flags]
        public enum ReleaseFlags
        {
            None = 0,
            Instance = 1,
            Inner = 2,
            ReferenceTracker = 4
        }

        public struct ClassNative
        {
            public ReleaseFlags Release;
            public IntPtr Instance;
            public IntPtr Inner;
            public IntPtr ReferenceTracker;
        }

        public unsafe static void Init<T>(
            ref ClassNative classNative,
            object thisInstance,
            bool aggregateRefTracker,
            ComWrappers cw,
            delegate*<IntPtr, out IntPtr, IntPtr> CreateInstance)
        {
            bool isAggregation = typeof(T) != thisInstance.GetType();

            {
                IntPtr outer = default;
                if (isAggregation)
                {
                    // Create a managed object wrapper (i.e. CCW) to act as the outer.
                    // Passing the CreateComInterfaceFlags.TrackerSupport can be done if
                    // IReferenceTracker support is possible.
                    //
                    // The outer is now owned in this context.
                    outer = cw.GetOrCreateComInterfaceForObject(thisInstance, CreateComInterfaceFlags.TrackerSupport);
                }

                // Create an instance of the COM/WinRT type.
                // This is typically accomplished through a call to CoCreateInstance() or RoActivateInstance().
                //
                // Ownership of the outer has been transferred to the new instance.
                // Some APIs do return a non-null inner even with a null outer. This
                // means ownership may now be owned in this context in either aggregation state.
                classNative.Instance = CreateInstance(outer, out classNative.Inner);
            }

            // TEST: Indicate if we should attempt aggregation with ReferenceTracker.
            if (aggregateRefTracker)
            {
                // Determine if the instance supports IReferenceTracker (e.g. WinUI).
                // Acquiring this interface is useful for:
                //   1) Providing an indication of what value to pass during RCW creation.
                //   2) Informing the Reference Tracker runtime during non-aggregation
                //      scenarios about new references.
                //
                // If aggregation, query the inner since that will have the implementation
                // otherwise the new instance will be used. Since the inner was composed
                // it should answer immediately without going through the outer. Either way
                // the reference count will go to the new instance.
                IntPtr queryForTracker = isAggregation ? classNative.Inner : classNative.Instance;
                int hr = Marshal.QueryInterface(queryForTracker, ref IID_IReferenceTracker, out classNative.ReferenceTracker);
                if (hr != 0)
                {
                    classNative.ReferenceTracker = default;
                }
            }

            {
                // Determine flags needed for native object wrapper (i.e. RCW) creation.
                var createObjectFlags = CreateObjectFlags.None;
                IntPtr instanceToWrap = classNative.Instance;

                // Update flags if the native instance is being used in an aggregation scenario.
                if (isAggregation)
                {
                    // Indicate the scenario is aggregation
                    createObjectFlags |= CreateObjectFlags.Aggregation;

                    // The instance supports IReferenceTracker.
                    if (classNative.ReferenceTracker != default(IntPtr))
                    {
                        createObjectFlags |= CreateObjectFlags.TrackerObject;

                        // IReferenceTracker is not needed in aggregation scenarios.
                        // It is not needed because all QueryInterface() calls on an
                        // object are followed by an immediately release of the returned
                        // pointer - see below for details.
                        Marshal.Release(classNative.ReferenceTracker);

                        // .NET 5 limitation
                        //
                        // For aggregated scenarios involving IReferenceTracker
                        // the API handles object cleanup. In .NET 5 the API
                        // didn't expose an option to handle this so we pass the inner
                        // in order to handle its lifetime.
                        //
                        // The API doesn't handle inner lifetime in any other scenario
                        // in the .NET 5 timeframe.
                        instanceToWrap = classNative.Inner;
                    }
                }

                // Create a native object wrapper (i.e. RCW).
                //
                // Note this function will call QueryInterface() on the supplied instance,
                // therefore it is important that the enclosing CCW forwards to its inner
                // if aggregation is involved. This is typically accomplished through an
                // implementation of ICustomQueryInterface.
                cw.GetOrRegisterObjectForComInstance(instanceToWrap, createObjectFlags, thisInstance);
            }

            if (isAggregation)
            {
                // We release the instance here, but continue to use it since
                // ownership was transferred to the API and it will guarantee
                // the appropriate lifetime.
                Marshal.Release(classNative.Instance);
            }
            else
            {
                // In non-aggregation scenarios where an inner exists and
                // reference tracker is involved, we release the inner.
                //
                // .NET 5 limitation - see logic above.
                if (classNative.Inner != default(IntPtr) && classNative.ReferenceTracker != default(IntPtr))
                {
                    Marshal.Release(classNative.Inner);
                }
            }

            // The following describes the valid local values to consider and details
            // on their usage during the object's lifetime.
            classNative.Release = ReleaseFlags.None;
            if (isAggregation)
            {
                // Aggregation scenarios should avoid calling AddRef() on the
                // newInstance value. This is due to the semantics of COM Aggregation
                // and the fact that calling an AddRef() on the instance will increment
                // the CCW which in turn will ensure it cannot be cleaned up. Calling
                // AddRef() on the instance when passed to unmanagec code is correct
                // since unmanaged code is required to call Release() at some point.
                if (classNative.ReferenceTracker == default(IntPtr))
                {
                    // COM scenario
                    // The pointer to dispatch on for the instance.
                    // ** Never release.
                    classNative.Release |= ReleaseFlags.None; // Instance

                    // A pointer to the inner that should be queried for
                    //    additional interfaces. Immediately after a QueryInterface()
                    //    a Release() should be called on the returned pointer but the
                    //    pointer can be retained and used.
                    // ** Release in this class's Finalizer.
                    classNative.Release |= ReleaseFlags.Inner; // Inner
                }
                else
                {
                    // WinUI scenario
                    // The pointer to dispatch on for the instance.
                    // ** Never release.
                    classNative.Release |= ReleaseFlags.None; // Instance

                    // A pointer to the inner that should be queried for
                    //    additional interfaces. Immediately after a QueryInterface()
                    //    a Release() should be called on the returned pointer but the
                    //    pointer can be retained and used.
                    // ** Never release.
                    classNative.Release |= ReleaseFlags.None; // Inner

                    // No longer needed.
                    // ** Never release.
                    classNative.Release |= ReleaseFlags.None; // ReferenceTracker
                }
            }
            else
            {
                if (classNative.ReferenceTracker == default(IntPtr))
                {
                    // COM scenario
                    // The pointer to dispatch on for the instance.
                    // ** Release in this class's Finalizer.
                    classNative.Release |= ReleaseFlags.Instance; // Instance
                }
                else
                {
                    // WinUI scenario
                    // The pointer to dispatch on for the instance.
                    // ** Release in this class's Finalizer.
                    classNative.Release |= ReleaseFlags.Instance; // Instance

                    // This instance should be used to tell the
                    //    Reference Tracker runtime whenever an AddRef()/Release()
                    //    is performed on newInstance.
                    // ** Release in this class's Finalizer.
                    classNative.Release |= ReleaseFlags.ReferenceTracker; // ReferenceTracker
                }
            }
        }

        public static void Cleanup(ref ClassNative classNative)
        {
            if (classNative.Release.HasFlag(ReleaseFlags.Inner))
            {
                Marshal.Release(classNative.Inner);
            }
            if (classNative.Release.HasFlag(ReleaseFlags.Instance))
            {
                Marshal.Release(classNative.Instance);
            }
            if (classNative.Release.HasFlag(ReleaseFlags.ReferenceTracker))
            {
                Marshal.Release(classNative.ReferenceTracker);
            }
        }
    }
}

