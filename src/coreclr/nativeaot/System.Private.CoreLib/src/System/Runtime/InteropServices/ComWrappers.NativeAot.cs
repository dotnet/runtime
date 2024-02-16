// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Threading;

using Internal.Runtime;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Class for managing wrappers of COM IUnknown types.
    /// </summary>
    public abstract partial class ComWrappers
    {
        private const int TrackerRefShift = 32;
        private const ulong TrackerRefCounter = 1UL << TrackerRefShift;
        private const ulong DestroySentinel = 0x0000000080000000UL;
        private const ulong TrackerRefCountMask = 0xffffffff00000000UL;
        private const ulong ComRefCountMask = 0x000000007fffffffUL;
        private const int COR_E_ACCESSING_CCW = unchecked((int)0x80131544);

        internal static IntPtr DefaultIUnknownVftblPtr { get; } = CreateDefaultIUnknownVftbl();
        internal static IntPtr TaggedImplVftblPtr { get; } = CreateTaggedImplVftbl();
        internal static IntPtr DefaultIReferenceTrackerTargetVftblPtr { get; } = CreateDefaultIReferenceTrackerTargetVftbl();

        internal static readonly Guid IID_IUnknown = new Guid(0x00000000, 0x0000, 0x0000, 0xc0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
        internal static readonly Guid IID_IReferenceTrackerTarget = new Guid(0x64bd43f8, 0xbfee, 0x4ec4, 0xb7, 0xeb, 0x29, 0x35, 0x15, 0x8d, 0xae, 0x21);
        internal static readonly Guid IID_TaggedImpl = new Guid(0x5c13e51c, 0x4f32, 0x4726, 0xa3, 0xfd, 0xf3, 0xed, 0xd6, 0x3d, 0xa3, 0xa0);
        internal static readonly Guid IID_IReferenceTracker = new Guid(0x11D3B13A, 0x180E, 0x4789, 0xA8, 0xBE, 0x77, 0x12, 0x88, 0x28, 0x93, 0xE6);
        internal static readonly Guid IID_IReferenceTrackerHost = new Guid(0x29a71c6a, 0x3c42, 0x4416, 0xa3, 0x9d, 0xe2, 0x82, 0x5a, 0x7, 0xa7, 0x73);
        internal static readonly Guid IID_IReferenceTrackerManager = new Guid(0x3cf184b4, 0x7ccb, 0x4dda, 0x84, 0x55, 0x7e, 0x6c, 0xe9, 0x9a, 0x32, 0x98);
        internal static readonly Guid IID_IFindReferenceTargetsCallback = new Guid(0x04b3486c, 0x4687, 0x4229, 0x8d, 0x14, 0x50, 0x5a, 0xb5, 0x84, 0xdd, 0x88);

        private static readonly Guid IID_IInspectable = new Guid(0xAF86E2E0, 0xB12D, 0x4c6a, 0x9C, 0x5A, 0xD7, 0xAA, 0x65, 0x10, 0x1E, 0x90);
        private static readonly Guid IID_IWeakReferenceSource = new Guid(0x00000038, 0, 0, 0xC0, 0, 0, 0, 0, 0, 0, 0x46);

        private static readonly ConditionalWeakTable<object, NativeObjectWrapper> s_rcwTable = new ConditionalWeakTable<object, NativeObjectWrapper>();
        private static readonly List<GCHandle> s_referenceTrackerNativeObjectWrapperCache = new List<GCHandle>();

        private readonly ConditionalWeakTable<object, ManagedObjectWrapperHolder> _ccwTable = new ConditionalWeakTable<object, ManagedObjectWrapperHolder>();
        private readonly Lock _lock = new Lock(useTrivialWaits: true);
        private readonly Dictionary<IntPtr, GCHandle> _rcwCache = new Dictionary<IntPtr, GCHandle>();

        internal static bool TryGetComInstanceForIID(object obj, Guid iid, out IntPtr unknown, out long wrapperId)
        {
            if (obj == null
                || !s_rcwTable.TryGetValue(obj, out NativeObjectWrapper? wrapper))
            {
                unknown = IntPtr.Zero;
                wrapperId = 0;
                return false;
            }

            wrapperId = wrapper._comWrappers.id;
            return Marshal.QueryInterface(wrapper._externalComObject, iid, out unknown) == HResults.S_OK;
        }

        public static unsafe bool TryGetComInstance(object obj, out IntPtr unknown)
        {
            unknown = IntPtr.Zero;
            if (obj == null
                || !s_rcwTable.TryGetValue(obj, out NativeObjectWrapper? wrapper))
            {
                return false;
            }

            return Marshal.QueryInterface(wrapper._externalComObject, IID_IUnknown, out unknown) == HResults.S_OK;
        }

        public static unsafe bool TryGetObject(IntPtr unknown, [NotNullWhen(true)] out object? obj)
        {
            obj = null;
            if (unknown == IntPtr.Zero)
            {
                return false;
            }

            ComInterfaceDispatch* comInterfaceDispatch = TryGetComInterfaceDispatch(unknown);
            if (comInterfaceDispatch == null ||
                ComInterfaceDispatch.ToManagedObjectWrapper(comInterfaceDispatch)->MarkedToDestroy)
            {
                return false;
            }

            obj = ComInterfaceDispatch.GetInstance<object>(comInterfaceDispatch);
            return true;
        }

        /// <summary>
        /// ABI for function dispatch of a COM interface.
        /// </summary>
        public unsafe partial struct ComInterfaceDispatch
        {
            /// <summary>
            /// Given a <see cref="System.IntPtr"/> from a generated Vtable, convert to the target type.
            /// </summary>
            /// <typeparam name="T">Desired type.</typeparam>
            /// <param name="dispatchPtr">Pointer supplied to Vtable function entry.</param>
            /// <returns>Instance of type associated with dispatched function call.</returns>
            public static unsafe T GetInstance<T>(ComInterfaceDispatch* dispatchPtr) where T : class
            {
                ManagedObjectWrapper* comInstance = ToManagedObjectWrapper(dispatchPtr);
                return Unsafe.As<T>(comInstance->Holder.WrappedObject);
            }

            internal static unsafe ManagedObjectWrapper* ToManagedObjectWrapper(ComInterfaceDispatch* dispatchPtr)
            {
                return ((InternalComInterfaceDispatch*)dispatchPtr)->_thisPtr;
            }
        }

        internal unsafe struct InternalComInterfaceDispatch
        {
            public IntPtr Vtable;
            internal ManagedObjectWrapper* _thisPtr;
        }

        internal enum CreateComInterfaceFlagsEx
        {
            None = 0,

            /// <summary>
            /// The caller will provide an IUnknown Vtable.
            /// </summary>
            /// <remarks>
            /// This is useful in scenarios when the caller has no need to rely on an IUnknown instance
            /// that is used when running managed code is not possible (i.e. during a GC). In traditional
            /// COM scenarios this is common, but scenarios involving <see href="https://docs.microsoft.com/windows/win32/api/windows.ui.xaml.hosting.referencetracker/nn-windows-ui-xaml-hosting-referencetracker-ireferencetrackertarget">Reference Tracker hosting</see>
            /// calling of the IUnknown API during a GC is possible.
            /// </remarks>
            CallerDefinedIUnknown = 1,

            /// <summary>
            /// Flag used to indicate the COM interface should implement <see href="https://docs.microsoft.com/windows/win32/api/windows.ui.xaml.hosting.referencetracker/nn-windows-ui-xaml-hosting-referencetracker-ireferencetrackertarget">IReferenceTrackerTarget</see>.
            /// When this flag is passed, the resulting COM interface will have an internal implementation of IUnknown
            /// and as such none should be supplied by the caller.
            /// </summary>
            TrackerSupport = 2,

            LacksICustomQueryInterface = 1 << 29,
            IsComActivated = 1 << 30,
            IsPegged = 1 << 31,

            InternalMask = IsPegged | IsComActivated | LacksICustomQueryInterface,
        }

        internal unsafe struct ManagedObjectWrapper
        {
            public volatile IntPtr HolderHandle; // This is GC Handle
            public ulong RefCount;

            public int UserDefinedCount;
            public ComInterfaceEntry* UserDefined;
            internal InternalComInterfaceDispatch* Dispatches;

            internal CreateComInterfaceFlagsEx Flags;

            public bool IsRooted
            {
                get
                {
                    ulong refCount = Interlocked.Read(ref RefCount);
                    bool rooted = GetComCount(refCount) > 0;
                    if (!rooted)
                    {
                        rooted = GetTrackerCount(refCount) > 0 &&
                            ((Flags & CreateComInterfaceFlagsEx.IsPegged) != 0 || TrackerObjectManager.s_isGlobalPeggingOn);
                    }
                    return rooted;
                }
            }

            public ManagedObjectWrapperHolder? Holder
            {
                get
                {
                    IntPtr handle = HolderHandle;
                    if (handle == IntPtr.Zero)
                        return null;
                    else
                        return Unsafe.As<ManagedObjectWrapperHolder>(GCHandle.FromIntPtr(handle).Target);
                }
            }

            public readonly bool MarkedToDestroy => IsMarkedToDestroy(RefCount);

            public uint AddRef()
            {
                return GetComCount(Interlocked.Increment(ref RefCount));
            }

            public uint Release()
            {
                Debug.Assert(GetComCount(RefCount) != 0);
                return GetComCount(Interlocked.Decrement(ref RefCount));
            }

            public uint AddRefFromReferenceTracker()
            {
                ulong prev;
                ulong curr;
                do
                {
                    prev = RefCount;
                    curr = prev + TrackerRefCounter;
                } while (Interlocked.CompareExchange(ref RefCount, curr, prev) != prev);

                return GetTrackerCount(curr);
            }

            public uint ReleaseFromReferenceTracker()
            {
                Debug.Assert(GetTrackerCount(RefCount) != 0);
                ulong prev;
                ulong curr;
                do
                {
                    prev = RefCount;
                    curr = prev - TrackerRefCounter;
                }
                while (Interlocked.CompareExchange(ref RefCount, curr, prev) != prev);

                // If we observe the destroy sentinel, then this release
                // must destroy the wrapper.
                if (curr == DestroySentinel)
                    Destroy();

                return GetTrackerCount(curr);
            }

            public uint Peg()
            {
                SetFlag(CreateComInterfaceFlagsEx.IsPegged);
                return HResults.S_OK;
            }

            public uint Unpeg()
            {
                ResetFlag(CreateComInterfaceFlagsEx.IsPegged);
                return HResults.S_OK;
            }


            public unsafe int QueryInterfaceForTracker(in Guid riid, out IntPtr ppvObject)
            {
                if (IsMarkedToDestroy(RefCount) || Holder is null)
                {
                    ppvObject = IntPtr.Zero;
                    return COR_E_ACCESSING_CCW;
                }

                return QueryInterface(in riid, out ppvObject);
            }

            public unsafe int QueryInterface(in Guid riid, out IntPtr ppvObject)
            {
                ppvObject = AsRuntimeDefined(in riid);
                if (ppvObject == IntPtr.Zero)
                {
                    if ((Flags & CreateComInterfaceFlagsEx.LacksICustomQueryInterface) == 0)
                    {
                        var customQueryInterface = Holder.WrappedObject as ICustomQueryInterface;
                        if (customQueryInterface is null)
                        {
                            SetFlag(CreateComInterfaceFlagsEx.LacksICustomQueryInterface);
                        }
                        else
                        {
                            Guid riidLocal = riid;
                            switch (customQueryInterface.GetInterface(ref riidLocal, out ppvObject))
                            {
                                case CustomQueryInterfaceResult.Handled:
                                    return HResults.S_OK;
                                case CustomQueryInterfaceResult.NotHandled:
                                    break;
                                case CustomQueryInterfaceResult.Failed:
                                    return HResults.COR_E_INVALIDCAST;
                            }
                        }
                    }

                    ppvObject = AsUserDefined(in riid);
                    if (ppvObject == IntPtr.Zero)
                        return HResults.COR_E_INVALIDCAST;
                }

                AddRef();
                return HResults.S_OK;
            }

            public IntPtr As(in Guid riid)
            {
                // Find target interface and return dispatcher or null if not found.
                IntPtr typeMaybe = AsRuntimeDefined(in riid);
                if (typeMaybe == IntPtr.Zero)
                    typeMaybe = AsUserDefined(in riid);

                return typeMaybe;
            }

            /// <returns>true if actually destroyed</returns>
            public unsafe bool Destroy()
            {
                Debug.Assert(GetComCount(RefCount) == 0 || HolderHandle == IntPtr.Zero);

                if (HolderHandle == IntPtr.Zero)
                {
                    // We either were previously destroyed or multiple ManagedObjectWrapperHolder
                    // were created by the ConditionalWeakTable for the same object and we lost the race.
                    return true;
                }

                ulong prev, refCount;
                do
                {
                    prev = RefCount;
                    refCount = prev | DestroySentinel;
                } while (Interlocked.CompareExchange(ref RefCount, refCount, prev) != prev);

                if (refCount == DestroySentinel)
                {
                    IntPtr handle = Interlocked.Exchange(ref HolderHandle, IntPtr.Zero);
                    if (handle != IntPtr.Zero)
                    {
                        RuntimeImports.RhHandleFree(handle);
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }

            private unsafe IntPtr AsRuntimeDefined(in Guid riid)
            {
                // The order of interface lookup here is important.
                // See CreateCCW() for the expected order.
                int i = UserDefinedCount;
                if ((Flags & CreateComInterfaceFlagsEx.CallerDefinedIUnknown) == 0)
                {
                    if (riid == IID_IUnknown)
                    {
                        return (IntPtr)(Dispatches + i);
                    }

                    i++;
                }

                if ((Flags & CreateComInterfaceFlagsEx.TrackerSupport) != 0)
                {
                    if (riid == IID_IReferenceTrackerTarget)
                    {
                        return (IntPtr)(Dispatches + i);
                    }

                    i++;
                }

                {
                    if (riid == IID_TaggedImpl)
                    {
                        return (IntPtr)(Dispatches + i);
                    }
                }

                return IntPtr.Zero;
            }

            private unsafe IntPtr AsUserDefined(in Guid riid)
            {
                for (int i = 0; i < UserDefinedCount; ++i)
                {
                    if (UserDefined[i].IID == riid)
                    {
                        return (IntPtr)(Dispatches + i);
                    }
                }

                return IntPtr.Zero;
            }

            private void SetFlag(CreateComInterfaceFlagsEx flag)
            {
                int setMask = (int)flag;
                Interlocked.Or(ref Unsafe.As<CreateComInterfaceFlagsEx, int>(ref Flags), setMask);
            }

            private void ResetFlag(CreateComInterfaceFlagsEx flag)
            {
                int resetMask = ~(int)flag;
                Interlocked.And(ref Unsafe.As<CreateComInterfaceFlagsEx, int>(ref Flags), resetMask);
            }

            private static uint GetTrackerCount(ulong c)
            {
                return (uint)((c & TrackerRefCountMask) >> TrackerRefShift);
            }

            private static uint GetComCount(ulong c)
            {
                return (uint)(c & ComRefCountMask);
            }

            private static bool IsMarkedToDestroy(ulong c)
            {
                return (c & DestroySentinel) != 0;
            }
        }

        internal sealed unsafe class ManagedObjectWrapperHolder
        {
            static ManagedObjectWrapperHolder()
            {
                delegate* unmanaged<IntPtr, bool> callback = &IsRootedCallback;
                if (!RuntimeImports.RhRegisterRefCountedHandleCallback((nint)callback, MethodTable.Of<ManagedObjectWrapperHolder>()))
                {
                    throw new OutOfMemoryException();
                }
            }

            [UnmanagedCallersOnly]
            private static bool IsRootedCallback(IntPtr pObj)
            {
                // We are paused in the GC, so this is safe.
#pragma warning disable CS8500 // Takes a pointer to a managed type
                ManagedObjectWrapperHolder* holder = (ManagedObjectWrapperHolder*)&pObj;
                return holder->_wrapper->IsRooted;
#pragma warning restore CS8500
            }

            private readonly ManagedObjectWrapper* _wrapper;
            private readonly ManagedObjectWrapperReleaser _releaser;
            private readonly object _wrappedObject;

            public ManagedObjectWrapperHolder(ManagedObjectWrapper* wrapper, object wrappedObject)
            {
                _wrapper = wrapper;
                _wrappedObject = wrappedObject;
                _releaser = new ManagedObjectWrapperReleaser(wrapper);
                _wrapper->HolderHandle = RuntimeImports.RhHandleAllocRefCounted(this);
            }

            public unsafe IntPtr ComIp => _wrapper->As(in ComWrappers.IID_IUnknown);

            public object WrappedObject => _wrappedObject;

            public uint AddRef() => _wrapper->AddRef();
        }

        internal sealed unsafe class ManagedObjectWrapperReleaser
        {
            private ManagedObjectWrapper* _wrapper;

            public ManagedObjectWrapperReleaser(ManagedObjectWrapper* wrapper)
            {
                _wrapper = wrapper;
            }

            ~ManagedObjectWrapperReleaser()
            {
                IntPtr refCountedHandle = _wrapper->HolderHandle;
                if (refCountedHandle != IntPtr.Zero && RuntimeImports.RhHandleGet(refCountedHandle) != null)
                {
                    // The ManagedObjectWrapperHolder has not been fully collected, so it is still
                    // potentially reachable via the Conditional Weak Table.
                    // Keep ourselves alive in case the wrapped object is resurrected.
                    GC.ReRegisterForFinalize(this);
                    return;
                }

                // Release GC handle created when MOW was built.
                if (_wrapper->Destroy())
                {
                    NativeMemory.Free(_wrapper);
                    _wrapper = null;
                }
                else
                {
                    // There are still outstanding references on the COM side.
                    // This case should only be hit when an outstanding
                    // tracker refcount exists from AddRefFromReferenceTracker.
                    // When implementing IReferenceTrackerHost, this should be
                    // reconsidered.
                    // https://github.com/dotnet/runtime/issues/85137
                    GC.ReRegisterForFinalize(this);
                }
            }
        }

        internal unsafe class NativeObjectWrapper
        {
            internal IntPtr _externalComObject;
            private IntPtr _inner;
            internal ComWrappers _comWrappers;
            internal readonly GCHandle _proxyHandle;
            internal readonly GCHandle _proxyHandleTrackingResurrection;
            internal readonly bool _aggregatedManagedObjectWrapper;

            static NativeObjectWrapper()
            {
                // Registering the weak reference support callbacks to enable
                // consulting ComWrappers when weak references are created
                // for RCWs.
                ComAwareWeakReference.InitializeCallbacks(&ComWeakRefToObject, &PossiblyComObject, &ObjectToComWeakRef);
            }

            public static NativeObjectWrapper Create(IntPtr externalComObject, IntPtr inner, ComWrappers comWrappers, object comProxy, CreateObjectFlags flags)
            {
                if (flags.HasFlag(CreateObjectFlags.TrackerObject) &&
                    Marshal.QueryInterface(externalComObject, IID_IReferenceTracker, out IntPtr trackerObject) == HResults.S_OK)
                {
                    return new ReferenceTrackerNativeObjectWrapper(externalComObject, inner, comWrappers, comProxy, flags, trackerObject);
                }
                else
                {
                    return new NativeObjectWrapper(externalComObject, inner, comWrappers, comProxy, flags);
                }
            }

            public NativeObjectWrapper(IntPtr externalComObject, IntPtr inner, ComWrappers comWrappers, object comProxy, CreateObjectFlags flags)
            {
                _externalComObject = externalComObject;
                _inner = inner;
                _comWrappers = comWrappers;
                _proxyHandle = GCHandle.Alloc(comProxy, GCHandleType.Weak);

                // We have a separate handle tracking resurrection as we want to make sure
                // we clean up the NativeObjectWrapper only after the RCW has been finalized
                // due to it can access the native object in the finalizer. At the same time,
                // we want other callers which are using _proxyHandle such as the RCW cache to
                // see the object as not alive once it is eligible for finalization.
                _proxyHandleTrackingResurrection = GCHandle.Alloc(comProxy, GCHandleType.WeakTrackResurrection);

                // If this is an aggregation scenario and the identity object
                // is a managed object wrapper, we need to call Release() to
                // indicate this external object isn't rooted. In the event the
                // object is passed out to native code an AddRef() must be called
                // based on COM convention and will "fix" the count.
                _aggregatedManagedObjectWrapper = flags.HasFlag(CreateObjectFlags.Aggregation) && TryGetComInterfaceDispatch(_externalComObject) != null;
                if (_aggregatedManagedObjectWrapper)
                {
                    Marshal.Release(externalComObject);
                }
            }

            public virtual void Release()
            {
                if (_comWrappers != null)
                {
                    _comWrappers.RemoveRCWFromCache(_externalComObject, _proxyHandle);
                    _comWrappers = null;
                }

                if (_proxyHandle.IsAllocated)
                {
                    _proxyHandle.Free();
                }

                if (_proxyHandleTrackingResurrection.IsAllocated)
                {
                    _proxyHandleTrackingResurrection.Free();
                }

                // If the inner was supplied, we need to release our reference.
                if (_inner != IntPtr.Zero)
                {
                    Marshal.Release(_inner);
                    _inner = IntPtr.Zero;
                }

                _externalComObject = IntPtr.Zero;
            }

            ~NativeObjectWrapper()
            {
                if (_proxyHandleTrackingResurrection.IsAllocated && _proxyHandleTrackingResurrection.Target != null)
                {
                    // The RCW object has not been fully collected, so it still
                    // can make calls on the native object in its finalizer.
                    // Keep ourselves alive until it is finalized.
                    GC.ReRegisterForFinalize(this);
                    return;
                }

                Release();
            }
        }

        internal sealed class ReferenceTrackerNativeObjectWrapper : NativeObjectWrapper
        {
            private IntPtr _trackerObject;
            private readonly bool _releaseTrackerObject;
            private int _trackerObjectDisconnected; // Atomic boolean, so using int.
            internal readonly IntPtr _contextToken;
            internal readonly GCHandle _nativeObjectWrapperWeakHandle;

            public IntPtr TrackerObject => (_trackerObject == IntPtr.Zero || _trackerObjectDisconnected == 1) ? IntPtr.Zero : _trackerObject;

            public ReferenceTrackerNativeObjectWrapper(
                nint externalComObject,
                nint inner,
                ComWrappers comWrappers,
                object comProxy,
                CreateObjectFlags flags,
                IntPtr trackerObject)
                : base(externalComObject, inner, comWrappers, comProxy, flags)
            {
                Debug.Assert(flags.HasFlag(CreateObjectFlags.TrackerObject));
                Debug.Assert(trackerObject != IntPtr.Zero);

                _trackerObject = trackerObject;
                _releaseTrackerObject = true;

                TrackerObjectManager.OnIReferenceTrackerFound(_trackerObject);
                TrackerObjectManager.AfterWrapperCreated(_trackerObject);

                if (flags.HasFlag(CreateObjectFlags.Aggregation))
                {
                    // Aggregation with an IReferenceTracker instance creates an extra AddRef()
                    // on the outer (e.g. MOW) so we clean up that issue here.
                    _releaseTrackerObject = false;
                    IReferenceTracker.ReleaseFromTrackerSource(_trackerObject); // IReferenceTracker
                    Marshal.Release(_trackerObject);
                }

                _contextToken = GetContextToken();
                _nativeObjectWrapperWeakHandle = GCHandle.Alloc(this, GCHandleType.Weak);
            }

            public override void Release()
            {
                // Remove the entry from the cache that keeps track of the active NativeObjectWrappers.
                if (_nativeObjectWrapperWeakHandle.IsAllocated)
                {
                    s_referenceTrackerNativeObjectWrapperCache.Remove(_nativeObjectWrapperWeakHandle);
                    _nativeObjectWrapperWeakHandle.Free();
                }

                DisconnectTracker();

                base.Release();
            }

            public void DisconnectTracker()
            {
                // Return if already disconnected or the tracker isn't set.
                if (_trackerObject == IntPtr.Zero || Interlocked.CompareExchange(ref _trackerObjectDisconnected, 1, 0) != 0)
                {
                    return;
                }

                // Always release the tracker source during a disconnect.
                // This to account for the implied IUnknown ownership by the runtime.
                IReferenceTracker.ReleaseFromTrackerSource(_trackerObject); // IUnknown

                // Disconnect from the tracker.
                if (_releaseTrackerObject)
                {
                    IReferenceTracker.ReleaseFromTrackerSource(_trackerObject); // IReferenceTracker
                    Marshal.Release(_trackerObject);
                    _trackerObject = IntPtr.Zero;
                }
            }
        }

        /// <summary>
        /// Globally registered instance of the ComWrappers class for reference tracker support.
        /// </summary>
        private static ComWrappers? s_globalInstanceForTrackerSupport;

        /// <summary>
        /// Globally registered instance of the ComWrappers class for marshalling.
        /// </summary>
        private static ComWrappers? s_globalInstanceForMarshalling;

        private static long s_instanceCounter;
        private readonly long id = Interlocked.Increment(ref s_instanceCounter);

        internal static object? GetOrCreateObjectFromWrapper(long wrapperId, IntPtr externalComObject)
        {
            if (s_globalInstanceForTrackerSupport != null && s_globalInstanceForTrackerSupport.id == wrapperId)
            {
                return s_globalInstanceForTrackerSupport.GetOrCreateObjectForComInstance(externalComObject, CreateObjectFlags.TrackerObject);
            }
            else if (s_globalInstanceForMarshalling != null && s_globalInstanceForMarshalling.id == wrapperId)
            {
                return ComObjectForInterface(externalComObject);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Create a COM representation of the supplied object that can be passed to a non-managed environment.
        /// </summary>
        /// <param name="instance">The managed object to expose outside the .NET runtime.</param>
        /// <param name="flags">Flags used to configure the generated interface.</param>
        /// <returns>The generated COM interface that can be passed outside the .NET runtime.</returns>
        /// <remarks>
        /// If a COM representation was previously created for the specified <paramref name="instance" /> using
        /// this <see cref="ComWrappers" /> instance, the previously created COM interface will be returned.
        /// If not, a new one will be created.
        /// </remarks>
        public unsafe IntPtr GetOrCreateComInterfaceForObject(object instance, CreateComInterfaceFlags flags)
        {
            ArgumentNullException.ThrowIfNull(instance);

            ManagedObjectWrapperHolder? ccwValue;
            if (_ccwTable.TryGetValue(instance, out ccwValue))
            {
                ccwValue.AddRef();
                return ccwValue.ComIp;
            }

            ccwValue = _ccwTable.GetValue(instance, (c) =>
            {
                ManagedObjectWrapper* value = CreateCCW(c, flags);
                return new ManagedObjectWrapperHolder(value, c);
            });
            ccwValue.AddRef();
            return ccwValue.ComIp;
        }

        private unsafe ManagedObjectWrapper* CreateCCW(object instance, CreateComInterfaceFlags flags)
        {
            ComInterfaceEntry* userDefined = ComputeVtables(instance, flags, out int userDefinedCount);
            if ((userDefined == null && userDefinedCount != 0) || userDefinedCount < 0)
            {
                throw new ArgumentException();
            }

            // Maximum number of runtime supplied vtables.
            Span<IntPtr> runtimeDefinedVtable = stackalloc IntPtr[3];
            int runtimeDefinedCount = 0;

            // Check if the caller will provide the IUnknown table.
            if ((flags & CreateComInterfaceFlags.CallerDefinedIUnknown) == CreateComInterfaceFlags.None)
            {
                runtimeDefinedVtable[runtimeDefinedCount++] = DefaultIUnknownVftblPtr;
            }

            if ((flags & CreateComInterfaceFlags.TrackerSupport) != 0)
            {
                runtimeDefinedVtable[runtimeDefinedCount++] = DefaultIReferenceTrackerTargetVftblPtr;
            }

            {
                runtimeDefinedVtable[runtimeDefinedCount++] = TaggedImplVftblPtr;
            }

            // Compute size for ManagedObjectWrapper instance.
            int totalDefinedCount = runtimeDefinedCount + userDefinedCount;

            // Allocate memory for the ManagedObjectWrapper.
            IntPtr wrapperMem = (IntPtr)NativeMemory.Alloc(
                (nuint)sizeof(ManagedObjectWrapper) + (nuint)totalDefinedCount * (nuint)sizeof(InternalComInterfaceDispatch));

            // Compute the dispatch section offset and ensure it is aligned.
            ManagedObjectWrapper* mow = (ManagedObjectWrapper*)wrapperMem;

            // Dispatches follow immediately after ManagedObjectWrapper
            InternalComInterfaceDispatch* pDispatches = (InternalComInterfaceDispatch*)(wrapperMem + sizeof(ManagedObjectWrapper));
            for (int i = 0; i < totalDefinedCount; i++)
            {
                pDispatches[i].Vtable = (i < userDefinedCount) ? userDefined[i].Vtable : runtimeDefinedVtable[i - userDefinedCount];
                pDispatches[i]._thisPtr = mow;
            }

            mow->HolderHandle = IntPtr.Zero;
            mow->RefCount = 0;
            mow->UserDefinedCount = userDefinedCount;
            mow->UserDefined = userDefined;
            mow->Flags = (CreateComInterfaceFlagsEx)flags;
            mow->Dispatches = pDispatches;
            return mow;
        }

        /// <summary>
        /// Get the currently registered managed object or creates a new managed object and registers it.
        /// </summary>
        /// <param name="externalComObject">Object to import for usage into the .NET runtime.</param>
        /// <param name="flags">Flags used to describe the external object.</param>
        /// <returns>Returns a managed object associated with the supplied external COM object.</returns>
        /// <remarks>
        /// If a managed object was previously created for the specified <paramref name="externalComObject" />
        /// using this <see cref="ComWrappers" /> instance, the previously created object will be returned.
        /// If not, a new one will be created.
        /// </remarks>
        public object GetOrCreateObjectForComInstance(IntPtr externalComObject, CreateObjectFlags flags)
        {
            object? obj;
            if (!TryGetOrCreateObjectForComInstanceInternal(externalComObject, IntPtr.Zero, flags, null, out obj))
                throw new ArgumentNullException(nameof(externalComObject));

            return obj!;
        }

        /// <summary>
        /// Get the currently registered managed object or uses the supplied managed object and registers it.
        /// </summary>
        /// <param name="externalComObject">Object to import for usage into the .NET runtime.</param>
        /// <param name="flags">Flags used to describe the external object.</param>
        /// <param name="wrapper">The <see cref="object"/> to be used as the wrapper for the external object</param>
        /// <returns>Returns a managed object associated with the supplied external COM object.</returns>
        /// <remarks>
        /// If the <paramref name="wrapper"/> instance already has an associated external object a <see cref="System.NotSupportedException"/> will be thrown.
        /// </remarks>
        public object GetOrRegisterObjectForComInstance(IntPtr externalComObject, CreateObjectFlags flags, object wrapper)
        {
            return GetOrRegisterObjectForComInstance(externalComObject, flags, wrapper, IntPtr.Zero);
        }

        /// <summary>
        /// Get the currently registered managed object or uses the supplied managed object and registers it.
        /// </summary>
        /// <param name="externalComObject">Object to import for usage into the .NET runtime.</param>
        /// <param name="flags">Flags used to describe the external object.</param>
        /// <param name="wrapper">The <see cref="object"/> to be used as the wrapper for the external object</param>
        /// <param name="inner">Inner for COM aggregation scenarios</param>
        /// <returns>Returns a managed object associated with the supplied external COM object.</returns>
        /// <remarks>
        /// This method override is for registering an aggregated COM instance with its associated inner. The inner
        /// will be released when the associated wrapper is eventually freed. Note that it will be released on a thread
        /// in an unknown apartment state. If the supplied inner is not known to be a free-threaded instance then
        /// it is advised to not supply the inner.
        ///
        /// If the <paramref name="wrapper"/> instance already has an associated external object a <see cref="System.NotSupportedException"/> will be thrown.
        /// </remarks>
        public object GetOrRegisterObjectForComInstance(IntPtr externalComObject, CreateObjectFlags flags, object wrapper, IntPtr inner)
        {
            ArgumentNullException.ThrowIfNull(wrapper);

            object? obj;
            if (!TryGetOrCreateObjectForComInstanceInternal(externalComObject, inner, flags, wrapper, out obj))
                throw new ArgumentNullException(nameof(externalComObject));

            return obj!;
        }

        private static unsafe ComInterfaceDispatch* TryGetComInterfaceDispatch(IntPtr comObject)
        {
            // If the first Vtable entry is part of a ManagedObjectWrapper impl,
            // we know how to interpret the IUnknown.
            IntPtr knownQI = ((IntPtr*)((IntPtr*)comObject)[0])[0];
            if (knownQI != ((IntPtr*)DefaultIUnknownVftblPtr)[0]
                || knownQI != ((IntPtr*)DefaultIReferenceTrackerTargetVftblPtr)[0])
            {
                // It is possible the user has defined their own IUnknown impl so
                // we fallback to the tagged interface approach to be sure.
                if (0 != Marshal.QueryInterface(comObject, IID_TaggedImpl, out nint implMaybe))
                {
                    return null;
                }

                IntPtr currentVersion = (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, int>)&ITaggedImpl_IsCurrentVersion;
                int hr = ((delegate* unmanaged<IntPtr, IntPtr, int>)(*(*(void***)implMaybe + 3 /* ITaggedImpl.IsCurrentVersion slot */)))(implMaybe, currentVersion);
                Marshal.Release(implMaybe);
                if (hr != 0)
                {
                    return null;
                }
            }

            return (ComInterfaceDispatch*)comObject;
        }

        private static void DetermineIdentityAndInner(
            IntPtr externalComObject,
            IntPtr innerMaybe,
            CreateObjectFlags flags,
            out IntPtr identity,
            out IntPtr inner)
        {
            inner = innerMaybe;

            IntPtr checkForIdentity = externalComObject;

            // Check if the flags indicate we are creating
            // an object for an external IReferenceTracker instance
            // that we are aggregating with.
            bool refTrackerInnerScenario = flags.HasFlag(CreateObjectFlags.TrackerObject)
                && flags.HasFlag(CreateObjectFlags.Aggregation);
            if (refTrackerInnerScenario &&
                Marshal.QueryInterface(externalComObject, IID_IReferenceTracker, out IntPtr referenceTrackerPtr) == HResults.S_OK)
            {
                // We are checking the supplied external value
                // for IReferenceTracker since in .NET 5 API usage scenarios
                // this could actually be the inner and we want the true identity
                // not the inner . This is a trick since the only way
                // to get identity from an inner is through a non-IUnknown
                // interface QI. Once we have the IReferenceTracker
                // instance we can be sure the QI for IUnknown will really
                // be the true identity.
                using ComHolder referenceTracker = new ComHolder(referenceTrackerPtr);
                checkForIdentity = referenceTrackerPtr;
                Marshal.ThrowExceptionForHR(Marshal.QueryInterface(checkForIdentity, IID_IUnknown, out identity));
            }
            else
            {
                Marshal.ThrowExceptionForHR(Marshal.QueryInterface(externalComObject, IID_IUnknown, out identity));
            }

            // Set the inner if scenario dictates an update.
            if (innerMaybe == IntPtr.Zero &&               // User didn't supply inner - .NET 5 API scenario sanity check.
                checkForIdentity != externalComObject &&   // Target of check was changed - .NET 5 API scenario sanity check.
                externalComObject != identity &&           // The supplied object doesn't match the computed identity.
                refTrackerInnerScenario)                   // The appropriate flags were set.
            {
                inner = externalComObject;
            }
        }

#pragma warning disable IDE0060
        /// <summary>
        /// Get the currently registered managed object or creates a new managed object and registers it.
        /// </summary>
        /// <param name="externalComObject">Object to import for usage into the .NET runtime.</param>
        /// <param name="innerMaybe">The inner instance if aggregation is involved</param>
        /// <param name="flags">Flags used to describe the external object.</param>
        /// <param name="wrapperMaybe">The <see cref="object"/> to be used as the wrapper for the external object.</param>
        /// <param name="retValue">The managed object associated with the supplied external COM object or <c>null</c> if it could not be created.</param>
        /// <returns>Returns <c>true</c> if a managed object could be retrieved/created, <c>false</c> otherwise</returns>
        private unsafe bool TryGetOrCreateObjectForComInstanceInternal(
            IntPtr externalComObject,
            IntPtr innerMaybe,
            CreateObjectFlags flags,
            object? wrapperMaybe,
            out object? retValue)
        {
            if (externalComObject == IntPtr.Zero)
                throw new ArgumentNullException(nameof(externalComObject));

            if (innerMaybe != IntPtr.Zero && !flags.HasFlag(CreateObjectFlags.Aggregation))
                throw new InvalidOperationException(SR.InvalidOperation_SuppliedInnerMustBeMarkedAggregation);

            DetermineIdentityAndInner(
                externalComObject,
                innerMaybe,
                flags,
                out IntPtr identity,
                out IntPtr inner);

            using ComHolder releaseIdentity = new ComHolder(identity);

            if (!flags.HasFlag(CreateObjectFlags.UniqueInstance))
            {
                using (_lock.EnterScope())
                {
                    if (_rcwCache.TryGetValue(identity, out GCHandle handle))
                    {
                        object? cachedWrapper = handle.Target;
                        if (cachedWrapper is not null)
                        {
                            retValue = cachedWrapper;
                            return true;
                        }
                        else
                        {
                            // The GCHandle has been clear out but the NativeObjectWrapper
                            // finalizer has not yet run to remove the entry from _rcwCache
                            _rcwCache.Remove(identity);
                        }
                    }

                    if (wrapperMaybe is not null)
                    {
                        retValue = wrapperMaybe;
                        NativeObjectWrapper wrapper = NativeObjectWrapper.Create(
                            identity,
                            inner,
                            this,
                            retValue,
                            flags);
                        if (!s_rcwTable.TryAdd(retValue, wrapper))
                        {
                            wrapper.Release();
                            throw new NotSupportedException();
                        }
                        _rcwCache.Add(identity, wrapper._proxyHandle);
                        if (wrapper is ReferenceTrackerNativeObjectWrapper referenceTrackerNativeObjectWrapper)
                        {
                            s_referenceTrackerNativeObjectWrapperCache.Add(referenceTrackerNativeObjectWrapper._nativeObjectWrapperWeakHandle);
                        }
                        return true;
                    }
                }
                if (flags.HasFlag(CreateObjectFlags.Unwrap))
                {
                    ComInterfaceDispatch* comInterfaceDispatch = TryGetComInterfaceDispatch(identity);
                    if (comInterfaceDispatch != null)
                    {
                        // If we found a managed object wrapper in this ComWrappers instance
                        // and it's has the same identity pointer as the one we're creating a NativeObjectWrapper for,
                        // unwrap it. We don't AddRef the wrapper as we don't take a reference to it.
                        //
                        // A managed object can have multiple managed object wrappers, with a max of one per context.
                        // Let's say we have a managed object A and ComWrappers instances C1 and C2. Let B1 and B2 be the
                        // managed object wrappers for A created with C1 and C2 respectively.
                        // If we are asked to create an EOC for B1 with the unwrap flag on the C2 ComWrappers instance,
                        // we will create a new wrapper. In this scenario, we'll only unwrap B2.
                        object unwrapped = ComInterfaceDispatch.GetInstance<object>(comInterfaceDispatch);
                        if (_ccwTable.TryGetValue(unwrapped, out ManagedObjectWrapperHolder? unwrappedWrapperInThisContext))
                        {
                            // The unwrapped object has a CCW in this context. Compare with identity
                            // so we can see if it's the CCW for the unwrapped object in this context.
                            if (unwrappedWrapperInThisContext.ComIp == identity)
                            {
                                retValue = unwrapped;
                                return true;
                            }
                        }
                    }
                }
            }

            retValue = CreateObject(identity, flags);
            if (retValue == null)
            {
                // If ComWrappers instance cannot create wrapper, we can do nothing here.
                return false;
            }

            if (flags.HasFlag(CreateObjectFlags.UniqueInstance))
            {
                NativeObjectWrapper wrapper = NativeObjectWrapper.Create(
                    identity,
                    inner,
                    null, // No need to cache NativeObjectWrapper for unique instances. They are not cached.
                    retValue,
                    flags);
                if (!s_rcwTable.TryAdd(retValue, wrapper))
                {
                    wrapper.Release();
                    throw new NotSupportedException();
                }
                if (wrapper is ReferenceTrackerNativeObjectWrapper referenceTrackerNativeObjectWrapper)
                {
                    s_referenceTrackerNativeObjectWrapperCache.Add(referenceTrackerNativeObjectWrapper._nativeObjectWrapperWeakHandle);
                }
                return true;
            }

            using (_lock.EnterScope())
            {
                object? cachedWrapper = null;
                if (_rcwCache.TryGetValue(identity, out var existingHandle))
                {
                    cachedWrapper = existingHandle.Target;
                    if (cachedWrapper is null)
                    {
                        // The GCHandle has been clear out but the NativeObjectWrapper
                        // finalizer has not yet run to remove the entry from _rcwCache
                        _rcwCache.Remove(identity);
                    }
                }

                if (cachedWrapper is not null)
                {
                    retValue = cachedWrapper;
                }
                else
                {
                    NativeObjectWrapper wrapper = NativeObjectWrapper.Create(
                        identity,
                        inner,
                        this,
                        retValue,
                        flags);
                    if (!s_rcwTable.TryAdd(retValue, wrapper))
                    {
                        wrapper.Release();
                        throw new NotSupportedException();
                    }
                    _rcwCache.Add(identity, wrapper._proxyHandle);
                    if (wrapper is ReferenceTrackerNativeObjectWrapper referenceTrackerNativeObjectWrapper)
                    {
                        s_referenceTrackerNativeObjectWrapperCache.Add(referenceTrackerNativeObjectWrapper._nativeObjectWrapperWeakHandle);
                    }
                }
            }

            return true;
        }
#pragma warning restore IDE0060

        private void RemoveRCWFromCache(IntPtr comPointer, GCHandle expectedValue)
        {
            using (_lock.EnterScope())
            {
                // TryGetOrCreateObjectForComInstanceInternal may have put a new entry into the cache
                // in the time between the GC cleared the contents of the GC handle but before the
                // NativeObjectWrapper finalizer ran.
                if (_rcwCache.TryGetValue(comPointer, out GCHandle cachedValue) && expectedValue.Equals(cachedValue))
                {
                    _rcwCache.Remove(comPointer);
                }
            }
        }

        /// <summary>
        /// Register a <see cref="ComWrappers" /> instance to be used as the global instance for reference tracker support.
        /// </summary>
        /// <param name="instance">Instance to register</param>
        /// <remarks>
        /// This function can only be called a single time. Subsequent calls to this function will result
        /// in a <see cref="System.InvalidOperationException"/> being thrown.
        ///
        /// Scenarios where this global instance may be used are:
        ///  * Object tracking via the <see cref="CreateComInterfaceFlags.TrackerSupport" /> and <see cref="CreateObjectFlags.TrackerObject" /> flags.
        /// </remarks>
        public static void RegisterForTrackerSupport(ComWrappers instance)
        {
            ArgumentNullException.ThrowIfNull(instance);

            if (null != Interlocked.CompareExchange(ref s_globalInstanceForTrackerSupport, instance, null))
            {
                throw new InvalidOperationException(SR.InvalidOperation_ResetGlobalComWrappersInstance);
            }
        }

        /// <summary>
        /// Register a <see cref="ComWrappers" /> instance to be used as the global instance for marshalling in the runtime.
        /// </summary>
        /// <param name="instance">Instance to register</param>
        /// <remarks>
        /// This function can only be called a single time. Subsequent calls to this function will result
        /// in a <see cref="System.InvalidOperationException"/> being thrown.
        ///
        /// Scenarios where this global instance may be used are:
        ///  * Usage of COM-related Marshal APIs
        ///  * P/Invokes with COM-related types
        ///  * COM activation
        /// </remarks>
        [SupportedOSPlatformAttribute("windows")]
        public static void RegisterForMarshalling(ComWrappers instance)
        {
            ArgumentNullException.ThrowIfNull(instance);

            if (null != Interlocked.CompareExchange(ref s_globalInstanceForMarshalling, instance, null))
            {
                throw new InvalidOperationException(SR.InvalidOperation_ResetGlobalComWrappersInstance);
            }
        }

        /// <summary>
        /// Get the runtime provided IUnknown implementation.
        /// </summary>
        /// <param name="fpQueryInterface">Function pointer to QueryInterface.</param>
        /// <param name="fpAddRef">Function pointer to AddRef.</param>
        /// <param name="fpRelease">Function pointer to Release.</param>
        public static unsafe void GetIUnknownImpl(out IntPtr fpQueryInterface, out IntPtr fpAddRef, out IntPtr fpRelease)
        {
            fpQueryInterface = (IntPtr)(delegate* unmanaged<IntPtr, Guid*, IntPtr*, int>)&ComWrappers.IUnknown_QueryInterface;
            fpAddRef = (IntPtr)(delegate* unmanaged<IntPtr, uint>)&ComWrappers.IUnknown_AddRef;
            fpRelease = (IntPtr)(delegate* unmanaged<IntPtr, uint>)&ComWrappers.IUnknown_Release;
        }

        internal static IntPtr ComInterfaceForObject(object instance)
        {
            if (s_globalInstanceForMarshalling == null)
            {
                throw new NotSupportedException(SR.InvalidOperation_ComInteropRequireComWrapperInstance);
            }

            return s_globalInstanceForMarshalling.GetOrCreateComInterfaceForObject(instance, CreateComInterfaceFlags.None);
        }

        internal static unsafe IntPtr ComInterfaceForObject(object instance, Guid targetIID)
        {
            IntPtr unknownPtr = ComInterfaceForObject(instance);
            IntPtr comObjectInterface;
            ManagedObjectWrapper* wrapper = ComInterfaceDispatch.ToManagedObjectWrapper((ComInterfaceDispatch*)unknownPtr);
            int resultCode = wrapper->QueryInterface(in targetIID, out comObjectInterface);
            // We no longer need IUnknownPtr, release reference
            Marshal.Release(unknownPtr);
            if (resultCode != 0)
            {
                throw new InvalidCastException();
            }

            return comObjectInterface;
        }

        internal static object ComObjectForInterface(IntPtr externalComObject)
        {
            if (s_globalInstanceForMarshalling == null)
            {
                throw new NotSupportedException(SR.InvalidOperation_ComInteropRequireComWrapperInstance);
            }

            // TrackerObject support and unwrapping matches the built-in semantics that the global marshalling scenario mimics.
            return s_globalInstanceForMarshalling.GetOrCreateObjectForComInstance(externalComObject, CreateObjectFlags.TrackerObject | CreateObjectFlags.Unwrap);
        }

        internal static IntPtr GetOrCreateTrackerTarget(IntPtr externalComObject)
        {
            if (s_globalInstanceForTrackerSupport == null)
            {
                throw new NotSupportedException(SR.InvalidOperation_ComInteropRequireComWrapperTrackerInstance);
            }

            object obj = s_globalInstanceForTrackerSupport.GetOrCreateObjectForComInstance(externalComObject, CreateObjectFlags.TrackerObject);
            return s_globalInstanceForTrackerSupport.GetOrCreateComInterfaceForObject(obj, CreateComInterfaceFlags.TrackerSupport);
        }

        internal static void ReleaseExternalObjectsFromCurrentThread()
        {
            if (s_globalInstanceForTrackerSupport == null)
            {
                throw new NotSupportedException(SR.InvalidOperation_ComInteropRequireComWrapperTrackerInstance);
            }

            IntPtr contextToken = GetContextToken();

            List<object> objects = new List<object>();
            foreach (GCHandle weakNativeObjectWrapperHandle in s_referenceTrackerNativeObjectWrapperCache)
            {
                ReferenceTrackerNativeObjectWrapper? nativeObjectWrapper = Unsafe.As<ReferenceTrackerNativeObjectWrapper?>(weakNativeObjectWrapperHandle.Target);
                if (nativeObjectWrapper != null &&
                    nativeObjectWrapper._contextToken == contextToken)
                {
                    objects.Add(nativeObjectWrapper._proxyHandle.Target);

                    // Separate the wrapper from the tracker runtime prior to
                    // passing them.
                    nativeObjectWrapper.DisconnectTracker();
                }
            }

            s_globalInstanceForTrackerSupport.ReleaseObjects(objects);
        }

        // Used during GC callback
        internal static unsafe void WalkExternalTrackerObjects()
        {
            bool walkFailed = false;

            foreach (GCHandle weakNativeObjectWrapperHandle in s_referenceTrackerNativeObjectWrapperCache)
            {
                ReferenceTrackerNativeObjectWrapper? nativeObjectWrapper = Unsafe.As<ReferenceTrackerNativeObjectWrapper?>(weakNativeObjectWrapperHandle.Target);
                if (nativeObjectWrapper != null &&
                    nativeObjectWrapper.TrackerObject != IntPtr.Zero)
                {
                    FindReferenceTargetsCallback.s_currentRootObjectHandle = nativeObjectWrapper._proxyHandle;
                    if (IReferenceTracker.FindTrackerTargets(nativeObjectWrapper.TrackerObject, TrackerObjectManager.s_findReferencesTargetCallback) != HResults.S_OK)
                    {
                        walkFailed = true;
                        FindReferenceTargetsCallback.s_currentRootObjectHandle = default;
                        break;
                    }
                    FindReferenceTargetsCallback.s_currentRootObjectHandle = default;
                }
            }

            // Report whether walking failed or not.
            if (walkFailed)
            {
                TrackerObjectManager.s_isGlobalPeggingOn = true;
            }
            IReferenceTrackerManager.FindTrackerTargetsCompleted(TrackerObjectManager.s_trackerManager, walkFailed);
        }

        // Used during GC callback
        internal static void DetachNonPromotedObjects()
        {
            foreach (GCHandle weakNativeObjectWrapperHandle in s_referenceTrackerNativeObjectWrapperCache)
            {
                ReferenceTrackerNativeObjectWrapper? nativeObjectWrapper = Unsafe.As<ReferenceTrackerNativeObjectWrapper?>(weakNativeObjectWrapperHandle.Target);
                if (nativeObjectWrapper != null &&
                    nativeObjectWrapper.TrackerObject != IntPtr.Zero &&
                    !RuntimeImports.RhIsPromoted(nativeObjectWrapper._proxyHandle.Target))
                {
                    // Notify the wrapper it was not promoted and is being collected.
                    TrackerObjectManager.BeforeWrapperFinalized(nativeObjectWrapper.TrackerObject);
                }
            }
        }

        [UnmanagedCallersOnly]
        internal static unsafe int IUnknown_QueryInterface(IntPtr pThis, Guid* guid, IntPtr* ppObject)
        {
            ManagedObjectWrapper* wrapper = ComInterfaceDispatch.ToManagedObjectWrapper((ComInterfaceDispatch*)pThis);
            return wrapper->QueryInterface(in *guid, out *ppObject);
        }

        [UnmanagedCallersOnly]
        internal static unsafe uint IUnknown_AddRef(IntPtr pThis)
        {
            ManagedObjectWrapper* wrapper = ComInterfaceDispatch.ToManagedObjectWrapper((ComInterfaceDispatch*)pThis);
            return wrapper->AddRef();
        }

        [UnmanagedCallersOnly]
        internal static unsafe uint IUnknown_Release(IntPtr pThis)
        {
            ManagedObjectWrapper* wrapper = ComInterfaceDispatch.ToManagedObjectWrapper((ComInterfaceDispatch*)pThis);
            uint refcount = wrapper->Release();
            return refcount;
        }

        [UnmanagedCallersOnly]
        internal static unsafe int IReferenceTrackerTarget_QueryInterface(IntPtr pThis, Guid* guid, IntPtr* ppObject)
        {
            ManagedObjectWrapper* wrapper = ComInterfaceDispatch.ToManagedObjectWrapper((ComInterfaceDispatch*)pThis);
            return wrapper->QueryInterfaceForTracker(in *guid, out *ppObject);
        }

        [UnmanagedCallersOnly]
        internal static unsafe uint IReferenceTrackerTarget_AddRefFromReferenceTracker(IntPtr pThis)
        {
            ManagedObjectWrapper* wrapper = ComInterfaceDispatch.ToManagedObjectWrapper((ComInterfaceDispatch*)pThis);
            return wrapper->AddRefFromReferenceTracker();
        }

        [UnmanagedCallersOnly]
        internal static unsafe uint IReferenceTrackerTarget_ReleaseFromReferenceTracker(IntPtr pThis)
        {
            ManagedObjectWrapper* wrapper = ComInterfaceDispatch.ToManagedObjectWrapper((ComInterfaceDispatch*)pThis);
            return wrapper->ReleaseFromReferenceTracker();
        }

        [UnmanagedCallersOnly]
        internal static unsafe uint IReferenceTrackerTarget_Peg(IntPtr pThis)
        {
            ManagedObjectWrapper* wrapper = ComInterfaceDispatch.ToManagedObjectWrapper((ComInterfaceDispatch*)pThis);
            return wrapper->Peg();
        }

        [UnmanagedCallersOnly]
        internal static unsafe uint IReferenceTrackerTarget_Unpeg(IntPtr pThis)
        {
            ManagedObjectWrapper* wrapper = ComInterfaceDispatch.ToManagedObjectWrapper((ComInterfaceDispatch*)pThis);
            return wrapper->Unpeg();
        }

        [UnmanagedCallersOnly]
        internal static unsafe int ITaggedImpl_IsCurrentVersion(IntPtr pThis, IntPtr version)
        {
            return version == (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, int>)&ITaggedImpl_IsCurrentVersion
                ? HResults.S_OK
                : HResults.E_FAIL;
        }

        private static unsafe IntPtr CreateDefaultIUnknownVftbl()
        {
            IntPtr* vftbl = (IntPtr*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(ComWrappers), 3 * sizeof(IntPtr));
            GetIUnknownImpl(out vftbl[0], out vftbl[1], out vftbl[2]);
            return (IntPtr)vftbl;
        }

        // This IID represents an internal interface we define to tag any ManagedObjectWrappers we create.
        // This interface type and GUID do not correspond to any public interface; it is an internal implementation detail.
        private static unsafe IntPtr CreateTaggedImplVftbl()
        {
            IntPtr* vftbl = (IntPtr*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(ComWrappers), 4 * sizeof(IntPtr));
            GetIUnknownImpl(out vftbl[0], out vftbl[1], out vftbl[2]);
            vftbl[3] = (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, int>)&ITaggedImpl_IsCurrentVersion;
            return (IntPtr)vftbl;
        }

        private static unsafe IntPtr CreateDefaultIReferenceTrackerTargetVftbl()
        {
            IntPtr* vftbl = (IntPtr*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(ComWrappers), 7 * sizeof(IntPtr));
            vftbl[0] = (IntPtr)(delegate* unmanaged<IntPtr, Guid*, IntPtr*, int>)&ComWrappers.IReferenceTrackerTarget_QueryInterface;
            vftbl[1] = (IntPtr)(delegate* unmanaged<IntPtr, uint>)&ComWrappers.IUnknown_AddRef;
            vftbl[2] = (IntPtr)(delegate* unmanaged<IntPtr, uint>)&ComWrappers.IUnknown_Release;
            vftbl[3] = (IntPtr)(delegate* unmanaged<IntPtr, uint>)&ComWrappers.IReferenceTrackerTarget_AddRefFromReferenceTracker;
            vftbl[4] = (IntPtr)(delegate* unmanaged<IntPtr, uint>)&ComWrappers.IReferenceTrackerTarget_ReleaseFromReferenceTracker;
            vftbl[5] = (IntPtr)(delegate* unmanaged<IntPtr, uint>)&ComWrappers.IReferenceTrackerTarget_Peg;
            vftbl[6] = (IntPtr)(delegate* unmanaged<IntPtr, uint>)&ComWrappers.IReferenceTrackerTarget_Unpeg;
            return (IntPtr)vftbl;
        }

        [UnmanagedCallersOnly]
        internal static unsafe int IReferenceTrackerHost_DisconnectUnusedReferenceSources(IntPtr pThis, uint flags)
        {
            try
            {
                // Defined in windows.ui.xaml.hosting.referencetracker.h.
                const uint XAML_REFERENCETRACKER_DISCONNECT_SUSPEND = 0x00000001;

                if ((flags & XAML_REFERENCETRACKER_DISCONNECT_SUSPEND) != 0)
                {
                    RuntimeImports.RhCollect(2, InternalGCCollectionMode.Blocking | InternalGCCollectionMode.Optimized, true);
                }
                else
                {
                    GC.Collect();
                }
                return HResults.S_OK;
            }
            catch (Exception e)
            {
                return Marshal.GetHRForException(e);
            }

        }

        [UnmanagedCallersOnly]
        internal static unsafe int IReferenceTrackerHost_ReleaseDisconnectedReferenceSources(IntPtr pThis)
        {
            try
            {
                GC.WaitForPendingFinalizers();
                return HResults.S_OK;
            }
            catch (Exception e)
            {
                return Marshal.GetHRForException(e);
            }
        }

        [UnmanagedCallersOnly]
        internal static unsafe int IReferenceTrackerHost_NotifyEndOfReferenceTrackingOnThread(IntPtr pThis)
        {
            try
            {
                ReleaseExternalObjectsFromCurrentThread();
                return HResults.S_OK;
            }
            catch (Exception e)
            {
                return Marshal.GetHRForException(e);
            }

        }

        // Creates a proxy object (managed object wrapper) that points to the given IUnknown.
        // The proxy represents the following:
        //   1. Has a managed reference pointing to the external object
        //      and therefore forms a cycle that can be resolved by GC.
        //   2. Forwards data binding requests.
        //
        // For example:
        // NoCW = Native Object Com Wrapper also known as RCW
        //
        // Grid <---- NoCW             Grid <-------- NoCW
        // | ^                         |              ^
        // | |             Becomes     |              |
        // v |                         v              |
        // Rectangle                  Rectangle ----->Proxy
        //
        // Arguments
        //   obj        - An IUnknown* where a NoCW points to (Grid, in this case)
        //                    Notes:
        //                    1. We can either create a new NoCW or get back an old one from the cache.
        //                    2. This obj could be a regular tracker runtime object for data binding.
        //  ppNewReference  - The IReferenceTrackerTarget* for the proxy created
        //                    The tracker runtime will call IReferenceTrackerTarget to establish a reference.
        //
        [UnmanagedCallersOnly]
        internal static unsafe int IReferenceTrackerHost_GetTrackerTarget(IntPtr pThis, IntPtr punk, IntPtr* ppNewReference)
        {
            if (punk == IntPtr.Zero)
            {
                return HResults.E_INVALIDARG;
            }

            if (Marshal.QueryInterface(punk, IID_IUnknown, out IntPtr ppv) != HResults.S_OK)
            {
                return HResults.COR_E_INVALIDCAST;
            }

            try
            {
                using ComHolder identity = new ComHolder(ppv);
                using ComHolder trackerTarget = new ComHolder(GetOrCreateTrackerTarget(identity.Ptr));
                return Marshal.QueryInterface(trackerTarget.Ptr, IID_IReferenceTrackerTarget, out *ppNewReference);
            }
            catch (Exception e)
            {
                return Marshal.GetHRForException(e);
            }
        }

        [UnmanagedCallersOnly]
        internal static unsafe int IReferenceTrackerHost_AddMemoryPressure(IntPtr pThis, long bytesAllocated)
        {
            try
            {
                GC.AddMemoryPressure(bytesAllocated);
                return HResults.S_OK;
            }
            catch (Exception e)
            {
                return Marshal.GetHRForException(e);
            }
        }

        [UnmanagedCallersOnly]
        internal static unsafe int IReferenceTrackerHost_RemoveMemoryPressure(IntPtr pThis, long bytesAllocated)
        {
            try
            {
                GC.RemoveMemoryPressure(bytesAllocated);
                return HResults.S_OK;
            }
            catch (Exception e)
            {
                return Marshal.GetHRForException(e);
            }
        }

        // Lifetime maintained by stack - we don't care about ref counts
        [UnmanagedCallersOnly]
        internal static unsafe uint Untracked_AddRef(IntPtr pThis)
        {
            return 1;
        }

        [UnmanagedCallersOnly]
        internal static unsafe uint Untracked_Release(IntPtr pThis)
        {
            return 1;
        }

        [UnmanagedCallersOnly]
        internal static unsafe int IReferenceTrackerHost_QueryInterface(IntPtr pThis, Guid* guid, IntPtr* ppObject)
        {
            if (*guid == IID_IReferenceTrackerHost || *guid == IID_IUnknown)
            {
                *ppObject = pThis;
                return 0;
            }
            else
            {
                return HResults.COR_E_INVALIDCAST;
            }
        }

        internal static unsafe IntPtr CreateDefaultIReferenceTrackerHostVftbl()
        {
            IntPtr* vftbl = (IntPtr*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(ComWrappers), 9 * sizeof(IntPtr));
            vftbl[0] = (IntPtr)(delegate* unmanaged<IntPtr, Guid*, IntPtr*, int>)&ComWrappers.IReferenceTrackerHost_QueryInterface;
            vftbl[1] = (IntPtr)(delegate* unmanaged<IntPtr, uint>)&ComWrappers.Untracked_AddRef;
            vftbl[2] = (IntPtr)(delegate* unmanaged<IntPtr, uint>)&ComWrappers.Untracked_Release;
            vftbl[3] = (IntPtr)(delegate* unmanaged<IntPtr, uint, int>)&ComWrappers.IReferenceTrackerHost_DisconnectUnusedReferenceSources;
            vftbl[4] = (IntPtr)(delegate* unmanaged<IntPtr, int>)&ComWrappers.IReferenceTrackerHost_ReleaseDisconnectedReferenceSources;
            vftbl[5] = (IntPtr)(delegate* unmanaged<IntPtr, int>)&ComWrappers.IReferenceTrackerHost_NotifyEndOfReferenceTrackingOnThread;
            vftbl[6] = (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, IntPtr*, int>)&ComWrappers.IReferenceTrackerHost_GetTrackerTarget;
            vftbl[7] = (IntPtr)(delegate* unmanaged<IntPtr, long, int>)&ComWrappers.IReferenceTrackerHost_AddMemoryPressure;
            vftbl[8] = (IntPtr)(delegate* unmanaged<IntPtr, long, int>)&ComWrappers.IReferenceTrackerHost_RemoveMemoryPressure;
            return (IntPtr)vftbl;
        }

        private static IntPtr GetContextToken()
        {
#if TARGET_WINDOWS
            Interop.Ole32.CoGetContextToken(out IntPtr contextToken);
            return contextToken;
#else
            return IntPtr.Zero;
#endif
        }

        // Wrapper for IWeakReference
        private static unsafe class IWeakReference
        {
            public static int Resolve(IntPtr pThis, Guid guid, out IntPtr inspectable)
            {
                fixed (IntPtr* inspectablePtr = &inspectable)
                    return (*(delegate* unmanaged<IntPtr, Guid*, IntPtr*, int>**)pThis)[3](pThis, &guid, inspectablePtr);
            }
        }

        // Wrapper for IWeakReferenceSource
        private static unsafe class IWeakReferenceSource
        {
            public static int GetWeakReference(IntPtr pThis, out IntPtr weakReference)
            {
                fixed (IntPtr* weakReferencePtr = &weakReference)
                    return (*(delegate* unmanaged<IntPtr, IntPtr*, int>**)pThis)[3](pThis, weakReferencePtr);
            }
        }

        private static object? ComWeakRefToObject(IntPtr pComWeakRef, long wrapperId)
        {
            if (wrapperId == 0)
            {
                return null;
            }

            // Using the IWeakReference*, get ahold of the target native COM object's IInspectable*.  If this resolve fails or
            // returns null, then we assume that the underlying native COM object is no longer alive, and thus we cannot create a
            // new RCW for it.
            if (IWeakReference.Resolve(pComWeakRef, IID_IInspectable, out IntPtr targetPtr) == HResults.S_OK &&
                targetPtr != IntPtr.Zero)
            {
                using ComHolder target = new ComHolder(targetPtr);
                if (Marshal.QueryInterface(target.Ptr, IID_IUnknown, out IntPtr targetIdentityPtr) == HResults.S_OK)
                {
                    using ComHolder targetIdentity = new ComHolder(targetIdentityPtr);
                    return GetOrCreateObjectFromWrapper(wrapperId, targetIdentity.Ptr);
                }
            }

            return null;
        }

        private static unsafe bool PossiblyComObject(object target)
        {
            // If the RCW is an aggregated RCW, then the managed object cannot be recreated from the IUnknown
            // as the outer IUnknown wraps the managed object. In this case, don't create a weak reference backed
            // by a COM weak reference.
            return s_rcwTable.TryGetValue(target, out NativeObjectWrapper? wrapper) && !wrapper._aggregatedManagedObjectWrapper;
        }

        private static unsafe IntPtr ObjectToComWeakRef(object target, out long wrapperId)
        {
            if (TryGetComInstanceForIID(
                target,
                IID_IWeakReferenceSource,
                out IntPtr weakReferenceSourcePtr,
                out wrapperId))
            {
                using ComHolder weakReferenceSource = new ComHolder(weakReferenceSourcePtr);
                if (IWeakReferenceSource.GetWeakReference(weakReferenceSource.Ptr, out IntPtr weakReference) == HResults.S_OK)
                {
                    return weakReference;
                }
            }

            return IntPtr.Zero;
        }
    }
}
