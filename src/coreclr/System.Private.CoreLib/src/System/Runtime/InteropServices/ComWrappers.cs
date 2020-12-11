// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Enumeration of flags for <see cref="ComWrappers.GetOrCreateComInterfaceForObject(object, CreateComInterfaceFlags)"/>.
    /// </summary>
    [Flags]
    public enum CreateComInterfaceFlags
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
    }

    /// <summary>
    /// Enumeration of flags for <see cref="ComWrappers.GetOrCreateObjectForComInstance(IntPtr, CreateObjectFlags)"/>.
    /// </summary>
    [Flags]
    public enum CreateObjectFlags
    {
        None = 0,

        /// <summary>
        /// Indicate if the supplied external COM object implements the <see href="https://docs.microsoft.com/windows/win32/api/windows.ui.xaml.hosting.referencetracker/nn-windows-ui-xaml-hosting-referencetracker-ireferencetracker">IReferenceTracker</see>.
        /// </summary>
        TrackerObject = 1,

        /// <summary>
        /// Ignore any internal caching and always create a unique instance.
        /// </summary>
        UniqueInstance = 2,
    }

    /// <summary>
    /// Internal enumeration used by the runtime to indicate the scenario for which ComWrappers is being used.
    /// </summary>
    internal enum ComWrappersScenario
    {
        Instance = 0,
        TrackerSupportGlobalInstance = 1,
        MarshallingGlobalInstance = 2,
    }

    /// <summary>
    /// Class for managing wrappers of COM IUnknown types.
    /// </summary>
    [SupportedOSPlatform("windows")]
    [CLSCompliant(false)]
    public abstract partial class ComWrappers
    {
        /// <summary>
        /// Interface type and pointer to targeted VTable.
        /// </summary>
        public struct ComInterfaceEntry
        {
            /// <summary>
            /// Interface IID.
            /// </summary>
            public Guid IID;

            /// <summary>
            /// Memory must have the same lifetime as the memory returned from the call to <see cref="ComputeVtables(object, CreateComInterfaceFlags, out int)"/>.
            /// </summary>
            public IntPtr Vtable;
        }

        /// <summary>
        /// ABI for function dispatch of a COM interface.
        /// </summary>
        public struct ComInterfaceDispatch
        {
            public IntPtr Vtable;

            /// <summary>
            /// Given a <see cref="System.IntPtr"/> from a generated Vtable, convert to the target type.
            /// </summary>
            /// <typeparam name="T">Desired type.</typeparam>
            /// <param name="dispatchPtr">Pointer supplied to Vtable function entry.</param>
            /// <returns>Instance of type associated with dispatched function call.</returns>
            public static unsafe T GetInstance<T>(ComInterfaceDispatch* dispatchPtr) where T : class
            {
                // See the dispatch section in the runtime for details on the masking below.
                const long DispatchThisPtrMask = ~0xfL;
                var comInstance = *(ComInterfaceInstance**)(((long)dispatchPtr) & DispatchThisPtrMask);

                return Unsafe.As<T>(GCHandle.InternalGet(comInstance->GcHandle));
            }

            private struct ComInterfaceInstance
            {
                public IntPtr GcHandle;
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
        public IntPtr GetOrCreateComInterfaceForObject(object instance, CreateComInterfaceFlags flags)
        {
            IntPtr ptr;
            if (!TryGetOrCreateComInterfaceForObjectInternal(this, instance, flags, out ptr))
                throw new ArgumentException(null, nameof(instance));

            return ptr;
        }

        /// <summary>
        /// Create a COM representation of the supplied object that can be passed to a non-managed environment.
        /// </summary>
        /// <param name="impl">The <see cref="ComWrappers" /> implementation to use when creating the COM representation.</param>
        /// <param name="instance">The managed object to expose outside the .NET runtime.</param>
        /// <param name="flags">Flags used to configure the generated interface.</param>
        /// <param name="retValue">The generated COM interface that can be passed outside the .NET runtime or IntPtr.Zero if it could not be created.</param>
        /// <returns>Returns <c>true</c> if a COM representation could be created, <c>false</c> otherwise</returns>
        /// <remarks>
        /// If <paramref name="impl" /> is <c>null</c>, the global instance (if registered) will be used.
        /// </remarks>
        private static bool TryGetOrCreateComInterfaceForObjectInternal(ComWrappers impl, object instance, CreateComInterfaceFlags flags, out IntPtr retValue)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            return TryGetOrCreateComInterfaceForObjectInternal(ObjectHandleOnStack.Create(ref impl), impl.id, ObjectHandleOnStack.Create(ref instance), flags, out retValue);
        }

        [DllImport(RuntimeHelpers.QCall)]
        private static extern bool TryGetOrCreateComInterfaceForObjectInternal(ObjectHandleOnStack comWrappersImpl, long wrapperId, ObjectHandleOnStack instance, CreateComInterfaceFlags flags, out IntPtr retValue);

        /// <summary>
        /// Compute the desired Vtable for <paramref name="obj"/> respecting the values of <paramref name="flags"/>.
        /// </summary>
        /// <param name="obj">Target of the returned Vtables.</param>
        /// <param name="flags">Flags used to compute Vtables.</param>
        /// <param name="count">The number of elements contained in the returned memory.</param>
        /// <returns><see cref="ComInterfaceEntry" /> pointer containing memory for all COM interface entries.</returns>
        /// <remarks>
        /// All memory returned from this function must either be unmanaged memory, pinned managed memory, or have been
        /// allocated with the <see cref="System.Runtime.CompilerServices.RuntimeHelpers.AllocateTypeAssociatedMemory(Type, int)"/> API.
        ///
        /// If the interface entries cannot be created and a negative <paramref name="count" /> or <code>null</code> and a non-zero <paramref name="count" /> are returned,
        /// the call to <see cref="ComWrappers.GetOrCreateComInterfaceForObject(object, CreateComInterfaceFlags)"/> will throw a <see cref="System.ArgumentException"/>.
        /// </remarks>
        protected unsafe abstract ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count);

        // Called by the runtime to execute the abstract instance function
        internal static unsafe void* CallComputeVtables(ComWrappersScenario scenario, ComWrappers? comWrappersImpl, object obj, CreateComInterfaceFlags flags, out int count)
        {
            ComWrappers? impl = null;
            switch (scenario)
            {
                case ComWrappersScenario.Instance:
                    impl = comWrappersImpl;
                    break;
                case ComWrappersScenario.TrackerSupportGlobalInstance:
                    impl = s_globalInstanceForTrackerSupport;
                    break;
                case ComWrappersScenario.MarshallingGlobalInstance:
                    impl = s_globalInstanceForMarshalling;
                    break;
            }

            if (impl is null)
            {
                count = -1;
                return null;
            }

            return impl.ComputeVtables(obj, flags, out count);
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
            if (!TryGetOrCreateObjectForComInstanceInternal(this, externalComObject, flags, null, out obj))
                throw new ArgumentNullException(nameof(externalComObject));

            return obj!;
        }

        /// <summary>
        /// Create a managed object for the object pointed at by <paramref name="externalComObject"/> respecting the values of <paramref name="flags"/>.
        /// </summary>
        /// <param name="externalComObject">Object to import for usage into the .NET runtime.</param>
        /// <param name="flags">Flags used to describe the external object.</param>
        /// <returns>Returns a managed object associated with the supplied external COM object.</returns>
        /// <remarks>
        /// If the object cannot be created and <code>null</code> is returned, the call to <see cref="ComWrappers.GetOrCreateObjectForComInstance(IntPtr, CreateObjectFlags)"/> will throw a <see cref="System.ArgumentNullException"/>.
        /// </remarks>
        protected abstract object? CreateObject(IntPtr externalComObject, CreateObjectFlags flags);

        // Called by the runtime to execute the abstract instance function.
        internal static object? CallCreateObject(ComWrappersScenario scenario, ComWrappers? comWrappersImpl, IntPtr externalComObject, CreateObjectFlags flags)
        {
            ComWrappers? impl = null;
            switch (scenario)
            {
                case ComWrappersScenario.Instance:
                    impl = comWrappersImpl;
                    break;
                case ComWrappersScenario.TrackerSupportGlobalInstance:
                    impl = s_globalInstanceForTrackerSupport;
                    break;
                case ComWrappersScenario.MarshallingGlobalInstance:
                    impl = s_globalInstanceForMarshalling;
                    break;
            }

            if (impl == null)
                return null;

            return impl.CreateObject(externalComObject, flags);
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
            if (wrapper == null)
                throw new ArgumentNullException(nameof(externalComObject));

            object? obj;
            if (!TryGetOrCreateObjectForComInstanceInternal(this, externalComObject, flags, wrapper, out obj))
                throw new ArgumentNullException(nameof(externalComObject));

            return obj!;
        }

        /// <summary>
        /// Get the currently registered managed object or creates a new managed object and registers it.
        /// </summary>
        /// <param name="impl">The <see cref="ComWrappers" /> implementation to use when creating the managed object.</param>
        /// <param name="externalComObject">Object to import for usage into the .NET runtime.</param>
        /// <param name="flags">Flags used to describe the external object.</param>
        /// <param name="wrapperMaybe">The <see cref="object"/> to be used as the wrapper for the external object.</param>
        /// <param name="retValue">The managed object associated with the supplied external COM object or <c>null</c> if it could not be created.</param>
        /// <returns>Returns <c>true</c> if a managed object could be retrieved/created, <c>false</c> otherwise</returns>
        /// <remarks>
        /// If <paramref name="impl" /> is <c>null</c>, the global instance (if registered) will be used.
        /// </remarks>
        private static bool TryGetOrCreateObjectForComInstanceInternal(ComWrappers impl, IntPtr externalComObject, CreateObjectFlags flags, object? wrapperMaybe, out object? retValue)
        {
            if (externalComObject == IntPtr.Zero)
                throw new ArgumentNullException(nameof(externalComObject));

            object? wrapperMaybeLocal = wrapperMaybe;
            retValue = null;
            return TryGetOrCreateObjectForComInstanceInternal(ObjectHandleOnStack.Create(ref impl), impl.id, externalComObject, flags, ObjectHandleOnStack.Create(ref wrapperMaybeLocal), ObjectHandleOnStack.Create(ref retValue));
        }

        [DllImport(RuntimeHelpers.QCall)]
        private static extern bool TryGetOrCreateObjectForComInstanceInternal(ObjectHandleOnStack comWrappersImpl, long wrapperId, IntPtr externalComObject, CreateObjectFlags flags, ObjectHandleOnStack wrapper, ObjectHandleOnStack retValue);

        /// <summary>
        /// Called when a request is made for a collection of objects to be released outside of normal object or COM interface lifetime.
        /// </summary>
        /// <param name="objects">Collection of objects to release.</param>
        protected abstract void ReleaseObjects(IEnumerable objects);

        // Call to execute the virtual instance function
        internal static void CallReleaseObjects(ComWrappers? comWrappersImpl, IEnumerable objects)
            => (comWrappersImpl ?? s_globalInstanceForTrackerSupport!).ReleaseObjects(objects);

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

            SetGlobalInstanceRegisteredForTrackerSupport(instance.id);
        }


        [DllImport(RuntimeHelpers.QCall)]
        [SuppressGCTransition]
        private static extern void SetGlobalInstanceRegisteredForTrackerSupport(long id);

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
        public static void RegisterForMarshalling(ComWrappers instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            if (null != Interlocked.CompareExchange(ref s_globalInstanceForMarshalling, instance, null))
            {
                throw new InvalidOperationException(SR.InvalidOperation_ResetGlobalComWrappersInstance);
            }

            // Indicate to the runtime that a global instance has been registered for marshalling.
            // This allows the native runtime know to call into the managed ComWrappers only if a
            // global instance is registered for marshalling.
            SetGlobalInstanceRegisteredForMarshalling(instance.id);
        }

        [DllImport(RuntimeHelpers.QCall)]
        [SuppressGCTransition]
        private static extern void SetGlobalInstanceRegisteredForMarshalling(long id);

        /// <summary>
        /// Get the runtime provided IUnknown implementation.
        /// </summary>
        /// <param name="fpQueryInterface">Function pointer to QueryInterface.</param>
        /// <param name="fpAddRef">Function pointer to AddRef.</param>
        /// <param name="fpRelease">Function pointer to Release.</param>
        protected static void GetIUnknownImpl(out IntPtr fpQueryInterface, out IntPtr fpAddRef, out IntPtr fpRelease)
            => GetIUnknownImplInternal(out fpQueryInterface, out fpAddRef, out fpRelease);

        [DllImport(RuntimeHelpers.QCall)]
        private static extern void GetIUnknownImplInternal(out IntPtr fpQueryInterface, out IntPtr fpAddRef, out IntPtr fpRelease);

        internal static int CallICustomQueryInterface(object customQueryInterfaceMaybe, ref Guid iid, out IntPtr ppObject)
        {
            var customQueryInterface = customQueryInterfaceMaybe as ICustomQueryInterface;
            if (customQueryInterface is null)
            {
                ppObject = IntPtr.Zero;
                return -1; // See TryInvokeICustomQueryInterfaceResult
            }

            return (int)customQueryInterface.GetInterface(ref iid, out ppObject);
        }
    }
}
