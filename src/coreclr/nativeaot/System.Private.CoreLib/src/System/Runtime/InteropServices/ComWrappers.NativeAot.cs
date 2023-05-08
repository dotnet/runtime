// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Threading;

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

        internal static Guid IID_IUnknown = new Guid(0x00000000, 0x0000, 0x0000, 0xc0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
        internal static Guid IID_IReferenceTrackerTarget = new Guid(0x64bd43f8, 0xbfee, 0x4ec4, 0xb7, 0xeb, 0x29, 0x35, 0x15, 0x8d, 0xae, 0x21);
        internal static Guid IID_TaggedImpl = new Guid(0x5c13e51c, 0x4f32, 0x4726, 0xa3, 0xfd, 0xf3, 0xed, 0xd6, 0x3d, 0xa3, 0xa0);

        private static readonly ConditionalWeakTable<object, NativeObjectWrapper> s_rcwTable = new ConditionalWeakTable<object, NativeObjectWrapper>();

        private readonly ConditionalWeakTable<object, ManagedObjectWrapperHolder> _ccwTable = new ConditionalWeakTable<object, ManagedObjectWrapperHolder>();
        private readonly Lock _lock = new Lock();
        private readonly Dictionary<IntPtr, GCHandle> _rcwCache = new Dictionary<IntPtr, GCHandle>();

        public static unsafe bool TryGetComInstance(object obj, out IntPtr unknown)
        {
            unknown = IntPtr.Zero;
            if (obj == null
                || !s_rcwTable.TryGetValue(obj, out NativeObjectWrapper? wrapper))
            {
                return false;
            }

            return Marshal.QueryInterface(wrapper._externalComObject, ref IID_IUnknown, out unknown) == 0;
        }

        public static unsafe bool TryGetObject(IntPtr unknown, [NotNullWhen(true)] out object? obj)
        {
            obj = null;
            if (unknown == IntPtr.Zero)
            {
                return false;
            }

            ComInterfaceDispatch* comInterfaceDispatch = TryGetComInterfaceDispatch(unknown);
            if (comInterfaceDispatch == null)
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
                        // TODO: global pegging state
                        // https://github.com/dotnet/runtime/issues/85137
                        rooted = GetTrackerCount(refCount) > 0 && (Flags & CreateComInterfaceFlagsEx.IsPegged) != 0;
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

        internal unsafe class ManagedObjectWrapperHolder
        {
            static ManagedObjectWrapperHolder()
            {
                delegate* unmanaged<IntPtr, bool> callback = &IsRootedCallback;
                if (!RuntimeImports.RhRegisterRefCountedHandleCallback((nint)callback, typeof(ManagedObjectWrapperHolder).GetEEType()))
                {
                    throw new OutOfMemoryException();
                }
            }

            [UnmanagedCallersOnly]
            static bool IsRootedCallback(IntPtr pObj)
            {
                // We are paused in the GC, so this is safe.
#pragma warning disable CS8500 // Takes a pointer to a managed type
                ManagedObjectWrapperHolder* holder = (ManagedObjectWrapperHolder*)&pObj;
                return holder->_wrapper->IsRooted;
#pragma warning restore CS8500
            }

            private ManagedObjectWrapper* _wrapper;
            private object _wrappedObject;

            public ManagedObjectWrapperHolder(ManagedObjectWrapper* wrapper, object wrappedObject)
            {
                _wrapper = wrapper;
                _wrappedObject = wrappedObject;
            }

            public void InitializeHandle()
            {
                IntPtr handle = RuntimeImports.RhHandleAllocRefCounted(this);
                IntPtr prev = Interlocked.CompareExchange(ref _wrapper->HolderHandle, handle, IntPtr.Zero);
                if (prev != IntPtr.Zero)
                {
                    RuntimeImports.RhHandleFree(handle);
                }
            }

            public unsafe IntPtr ComIp => _wrapper->As(in ComWrappers.IID_IUnknown);

            public object WrappedObject => _wrappedObject;

            public uint AddRef() => _wrapper->AddRef();

            ~ManagedObjectWrapperHolder()
            {
                // Release GC handle created when MOW was built.
                if (_wrapper->Destroy())
                {
                    NativeMemory.Free(_wrapper);
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
            private ComWrappers _comWrappers;
            internal GCHandle _proxyHandle;

            public NativeObjectWrapper(IntPtr externalComObject, ComWrappers comWrappers, object comProxy)
            {
                _externalComObject = externalComObject;
                _comWrappers = comWrappers;
                Marshal.AddRef(externalComObject);
                _proxyHandle = GCHandle.Alloc(comProxy, GCHandleType.Weak);
            }

            public void Release()
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

                if (_externalComObject != IntPtr.Zero)
                {
                    Marshal.Release(_externalComObject);
                    _externalComObject = IntPtr.Zero;
                }
            }

            ~NativeObjectWrapper()
            {
                Release();
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
            ccwValue.InitializeHandle();
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
            mow->RefCount = 1;
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
                if (0 != Marshal.QueryInterface(comObject, ref IID_TaggedImpl, out nint implMaybe))
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

            if (flags.HasFlag(CreateObjectFlags.Aggregation))
                throw new NotImplementedException();

            if (flags.HasFlag(CreateObjectFlags.Unwrap))
            {
                var comInterfaceDispatch = TryGetComInterfaceDispatch(externalComObject);
                if (comInterfaceDispatch != null)
                {
                    retValue = ComInterfaceDispatch.GetInstance<object>(comInterfaceDispatch);
                    return true;
                }
            }

            if (!flags.HasFlag(CreateObjectFlags.UniqueInstance))
            {
                using (LockHolder.Hold(_lock))
                {
                    if (_rcwCache.TryGetValue(externalComObject, out GCHandle handle))
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
                            _rcwCache.Remove(externalComObject);
                        }
                    }

                    if (wrapperMaybe is not null)
                    {
                        retValue = wrapperMaybe;
                        NativeObjectWrapper wrapper = new NativeObjectWrapper(
                            externalComObject,
                            this,
                            retValue);
                        if (!s_rcwTable.TryAdd(retValue, wrapper))
                        {
                            wrapper.Release();
                            throw new NotSupportedException();
                        }
                        _rcwCache.Add(externalComObject, wrapper._proxyHandle);
                        return true;
                    }
                }
            }

            retValue = CreateObject(externalComObject, flags);
            if (retValue == null)
            {
                // If ComWrappers instance cannot create wrapper, we can do nothing here.
                return false;
            }

            if (flags.HasFlag(CreateObjectFlags.UniqueInstance))
            {
                NativeObjectWrapper wrapper = new NativeObjectWrapper(
                    externalComObject,
                    null, // No need to cache NativeObjectWrapper for unique instances. They are not cached.
                    retValue);
                if (!s_rcwTable.TryAdd(retValue, wrapper))
                {
                    wrapper.Release();
                    throw new NotSupportedException();
                }
                return true;
            }

            using (LockHolder.Hold(_lock))
            {
                object? cachedWrapper = null;
                if (_rcwCache.TryGetValue(externalComObject, out var existingHandle))
                {
                    cachedWrapper = existingHandle.Target;
                    if (cachedWrapper is null)
                    {
                        // The GCHandle has been clear out but the NativeObjectWrapper
                        // finalizer has not yet run to remove the entry from _rcwCache
                        _rcwCache.Remove(externalComObject);
                    }
                }

                if (cachedWrapper is not null)
                {
                    retValue = cachedWrapper;
                }
                else
                {
                    NativeObjectWrapper wrapper = new NativeObjectWrapper(
                        externalComObject,
                        this,
                        retValue);
                    if (!s_rcwTable.TryAdd(retValue, wrapper))
                    {
                        wrapper.Release();
                        throw new NotSupportedException();
                    }
                    _rcwCache.Add(externalComObject, wrapper._proxyHandle);
                }
            }

            return true;
        }
#pragma warning restore IDE0060

        private void RemoveRCWFromCache(IntPtr comPointer, GCHandle expectedValue)
        {
            using (LockHolder.Hold(_lock))
            {
                // TryGetOrCreateObjectForComInstanceInternal may have put a new entry into the cache
                // in the time between the GC cleared the contents of the GC handle but before the
                // NativeObjectWrapper finializer ran.
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

            return s_globalInstanceForMarshalling.GetOrCreateObjectForComInstance(externalComObject, CreateObjectFlags.Unwrap);
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
    }
}
