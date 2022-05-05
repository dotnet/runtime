// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using Internal.Runtime.CompilerServices;

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

        internal static IntPtr DefaultIUnknownVftblPtr { get; } = CreateDefaultIUnknownVftbl();
        internal static IntPtr DefaultIReferenceTrackerTargetVftblPtr { get; } = CreateDefaultIReferenceTrackerTargetVftbl();

        internal static Guid IID_IUnknown = new Guid(0x00000000, 0x0000, 0x0000, 0xc0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
        internal static Guid IID_IReferenceTrackerTarget = new Guid(0x64bd43f8, 0xbfee, 0x4ec4, 0xb7, 0xeb, 0x29, 0x35, 0x15, 0x8d, 0xae, 0x21);

        private readonly ConditionalWeakTable<object, ManagedObjectWrapperHolder> _ccwTable = new ConditionalWeakTable<object, ManagedObjectWrapperHolder>();
        private readonly Lock _lock = new Lock();
        private readonly Dictionary<IntPtr, GCHandle> _rcwCache = new Dictionary<IntPtr, GCHandle>();
        private readonly ConditionalWeakTable<object, NativeObjectWrapper> _rcwTable = new ConditionalWeakTable<object, NativeObjectWrapper>();

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
                return Unsafe.As<T>(RuntimeImports.RhHandleGet(comInstance->Target));
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
            public IntPtr Target; // This is GC Handle
            public ulong RefCount;

            public int UserDefinedCount;
            public ComInterfaceEntry* UserDefined;
            internal InternalComInterfaceDispatch* Dispatches;

            internal CreateComInterfaceFlagsEx Flags;

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
                if (RefCount == DestroySentinel)
                    Destroy();

                return GetTrackerCount(RefCount);
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

            public unsafe int QueryInterface(in Guid riid, out IntPtr ppvObject)
            {
                ppvObject = AsRuntimeDefined(in riid);
                if (ppvObject == IntPtr.Zero)
                {
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

            public unsafe void Destroy()
            {
                if (Target == IntPtr.Zero)
                {
                    return;
                }

                RuntimeImports.RhHandleFree(Target);
                Target = IntPtr.Zero;
            }

            private unsafe IntPtr AsRuntimeDefined(in Guid riid)
            {
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
            private ManagedObjectWrapper* _wrapper;

            public ManagedObjectWrapperHolder(ManagedObjectWrapper* wrapper)
            {
                _wrapper = wrapper;
            }

            public unsafe IntPtr ComIp => _wrapper->As(in ComWrappers.IID_IUnknown);

            ~ManagedObjectWrapperHolder()
            {
                // Release GC handle created when MOW was built.
                _wrapper->Destroy();
                Marshal.FreeCoTaskMem((IntPtr)_wrapper);
            }
        }

        internal unsafe class NativeObjectWrapper
        {
            private readonly IntPtr _externalComObject;
            private readonly ComWrappers _comWrappers;
            internal GCHandle _proxyHandle;

            public NativeObjectWrapper(IntPtr externalComObject, ComWrappers comWrappers, object comProxy)
            {
                _externalComObject = externalComObject;
                _comWrappers = comWrappers;
                Marshal.AddRef(externalComObject);
                _proxyHandle = GCHandle.Alloc(comProxy, GCHandleType.Weak);
            }

            ~NativeObjectWrapper()
            {
                _comWrappers.RemoveRCWFromCache(_externalComObject);
                if (_proxyHandle.IsAllocated)
                {
                    _proxyHandle.Free();
                }

                Marshal.Release(_externalComObject);
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
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            ManagedObjectWrapperHolder? ccwValue;
            if (_ccwTable.TryGetValue(instance, out ccwValue))
            {
                return ccwValue.ComIp;
            }

            ccwValue = _ccwTable.GetValue(instance, (c) =>
            {
                ManagedObjectWrapper* value = CreateCCW(c, flags);
                return new ManagedObjectWrapperHolder(value);
            });
            return ccwValue.ComIp;
        }

        private unsafe ManagedObjectWrapper* CreateCCW(object instance, CreateComInterfaceFlags flags)
        {
            ComInterfaceEntry* userDefined = ComputeVtables(instance, flags, out int userDefinedCount);

            // Maximum number of runtime supplied vtables.
            Span<IntPtr> runtimeDefinedVtable = stackalloc IntPtr[4];
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

            // Compute size for ManagedObjectWrapper instance.
            int totalDefinedCount = runtimeDefinedCount + userDefinedCount;

            // Allocate memory for the ManagedObjectWrapper.
            IntPtr wrapperMem = Marshal.AllocCoTaskMem(
                sizeof(ManagedObjectWrapper) + totalDefinedCount * sizeof(InternalComInterfaceDispatch));

            // Compute the dispatch section offset and ensure it is aligned.
            ManagedObjectWrapper* mow = (ManagedObjectWrapper*)wrapperMem;

            // Dispatches follow immediately after ManagedObjectWrapper
            InternalComInterfaceDispatch* pDispatches = (InternalComInterfaceDispatch*)(wrapperMem + sizeof(ManagedObjectWrapper));
            for (int i = 0; i < totalDefinedCount; i++)
            {
                pDispatches[i].Vtable = (i < userDefinedCount) ? userDefined[i].Vtable : runtimeDefinedVtable[i - userDefinedCount];
                pDispatches[i]._thisPtr = mow;
            }

            mow->Target = RuntimeImports.RhHandleAlloc(instance, GCHandleType.Normal);
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
            if (wrapper == null)
                throw new ArgumentNullException(nameof(wrapper));

            object? obj;
            if (!TryGetOrCreateObjectForComInstanceInternal(externalComObject, inner, flags, wrapper, out obj))
                throw new ArgumentNullException(nameof(externalComObject));

            return obj!;
        }

        private unsafe ComInterfaceDispatch* TryGetComInterfaceDispatch(IntPtr comObject)
        {
            // If the first Vtable entry is part of the ManagedObjectWrapper IUnknown impl,
            // we know how to interpret the IUnknown.
            if (((IntPtr*)((IntPtr*)comObject)[0])[0] != ((IntPtr*)DefaultIUnknownVftblPtr)[0])
            {
                return null;
            }

            return (ComInterfaceDispatch*)comObject;
        }

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
                        retValue = handle.Target;
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
                // No need to cache NativeObjectWrapper for unique instances. They are not cached.
                return true;
            }

            using (LockHolder.Hold(_lock))
            {
                if (_rcwCache.TryGetValue(externalComObject, out var existingHandle))
                {
                    retValue = existingHandle.Target;
                }
                else
                {
                    NativeObjectWrapper wrapper = new NativeObjectWrapper(
                        externalComObject,
                        this,
                        retValue);
                    _rcwTable.Add(retValue, wrapper);
                    _rcwCache.Add(externalComObject, wrapper._proxyHandle);
                }
            }

            return true;
        }

        private void RemoveRCWFromCache(IntPtr comPointer)
        {
            using (LockHolder.Hold(_lock))
            {
                _rcwCache.Remove(comPointer);
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
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

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
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

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
        protected internal static unsafe void GetIUnknownImpl(out IntPtr fpQueryInterface, out IntPtr fpAddRef, out IntPtr fpRelease)
        {
            fpQueryInterface = (IntPtr)(delegate* unmanaged<IntPtr, Guid*, IntPtr*, int>)&ComWrappers.IUnknown_QueryInterface;
            fpAddRef = (IntPtr)(delegate* unmanaged<IntPtr, uint>)&ComWrappers.IUnknown_AddRef;
            fpRelease = (IntPtr)(delegate* unmanaged<IntPtr, uint>)&ComWrappers.IUnknown_Release;
        }

        internal static IntPtr ComInterfaceForObject(object instance)
        {
            if (s_globalInstanceForMarshalling == null)
            {
                throw new InvalidOperationException(SR.InvalidOperation_ComInteropRequireComWrapperInstance);
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
                throw new InvalidOperationException(SR.InvalidOperation_ComInteropRequireComWrapperInstance);
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
            if (refcount == 0)
            {
                wrapper->Destroy();
            }

            return refcount;
        }

        [UnmanagedCallersOnly]
        internal static unsafe int IReferenceTrackerTarget_QueryInterface(IntPtr pThis, Guid* guid, IntPtr* ppObject)
        {
            ManagedObjectWrapper* wrapper = ComInterfaceDispatch.ToManagedObjectWrapper((ComInterfaceDispatch*)pThis);
            return wrapper->QueryInterface(in *guid, out *ppObject);
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

        private static unsafe IntPtr CreateDefaultIUnknownVftbl()
        {
            IntPtr* vftbl = (IntPtr*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(ComWrappers), 3 * sizeof(IntPtr));
            GetIUnknownImpl(out vftbl[0], out vftbl[1], out vftbl[2]);
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
