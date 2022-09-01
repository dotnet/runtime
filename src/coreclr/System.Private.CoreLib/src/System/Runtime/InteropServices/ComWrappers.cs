// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices
{
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
    public abstract partial class ComWrappers
    {
        /// <summary>
        /// ABI for function dispatch of a COM interface.
        /// </summary>
        public partial struct ComInterfaceDispatch
        {
            /// <summary>
            /// Given a <see cref="System.IntPtr"/> from a generated Vtable, convert to the target type.
            /// </summary>
            /// <typeparam name="T">Desired type.</typeparam>
            /// <param name="dispatchPtr">Pointer supplied to Vtable function entry.</param>
            /// <returns>Instance of type associated with dispatched function call.</returns>
            public static unsafe T GetInstance<T>(ComInterfaceDispatch* dispatchPtr) where T : class
            {
                // See the dispatch section in the runtime for details on the masking below.
                long DispatchAlignmentThisPtr = sizeof(void*) == 8 ? 64 : 16; // Should be a power of 2.
                long DispatchThisPtrMask = ~(DispatchAlignmentThisPtr - 1);
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
            ArgumentNullException.ThrowIfNull(instance);

            return TryGetOrCreateComInterfaceForObjectInternal(ObjectHandleOnStack.Create(ref impl), impl.id, ObjectHandleOnStack.Create(ref instance), flags, out retValue);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ComWrappers_TryGetOrCreateComInterfaceForObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool TryGetOrCreateComInterfaceForObjectInternal(ObjectHandleOnStack comWrappersImpl, long wrapperId, ObjectHandleOnStack instance, CreateComInterfaceFlags flags, out IntPtr retValue);

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
            if (!TryGetOrCreateObjectForComInstanceInternal(this, externalComObject, IntPtr.Zero, flags, null, out obj))
                throw new ArgumentNullException(nameof(externalComObject));

            return obj!;
        }

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
            if (!TryGetOrCreateObjectForComInstanceInternal(this, externalComObject, inner, flags, wrapper, out obj))
                throw new ArgumentNullException(nameof(externalComObject));

            return obj!;
        }

        /// <summary>
        /// Get the currently registered managed object or creates a new managed object and registers it.
        /// </summary>
        /// <param name="impl">The <see cref="ComWrappers" /> implementation to use when creating the managed object.</param>
        /// <param name="externalComObject">Object to import for usage into the .NET runtime.</param>
        /// <param name="innerMaybe">The inner instance if aggregation is involved</param>
        /// <param name="flags">Flags used to describe the external object.</param>
        /// <param name="wrapperMaybe">The <see cref="object"/> to be used as the wrapper for the external object.</param>
        /// <param name="retValue">The managed object associated with the supplied external COM object or <c>null</c> if it could not be created.</param>
        /// <returns>Returns <c>true</c> if a managed object could be retrieved/created, <c>false</c> otherwise</returns>
        /// <remarks>
        /// If <paramref name="impl" /> is <c>null</c>, the global instance (if registered) will be used.
        /// </remarks>
        private static bool TryGetOrCreateObjectForComInstanceInternal(
            ComWrappers impl,
            IntPtr externalComObject,
            IntPtr innerMaybe,
            CreateObjectFlags flags,
            object? wrapperMaybe,
            out object? retValue)
        {
            ArgumentNullException.ThrowIfNull(externalComObject);

            // If the inner is supplied the Aggregation flag should be set.
            if (innerMaybe != IntPtr.Zero && !flags.HasFlag(CreateObjectFlags.Aggregation))
                throw new InvalidOperationException(SR.InvalidOperation_SuppliedInnerMustBeMarkedAggregation);

            object? wrapperMaybeLocal = wrapperMaybe;
            retValue = null;
            return TryGetOrCreateObjectForComInstanceInternal(ObjectHandleOnStack.Create(ref impl), impl.id, externalComObject, innerMaybe, flags, ObjectHandleOnStack.Create(ref wrapperMaybeLocal), ObjectHandleOnStack.Create(ref retValue));
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ComWrappers_TryGetOrCreateObjectForComInstance")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool TryGetOrCreateObjectForComInstanceInternal(ObjectHandleOnStack comWrappersImpl, long wrapperId, IntPtr externalComObject, IntPtr innerMaybe, CreateObjectFlags flags, ObjectHandleOnStack wrapper, ObjectHandleOnStack retValue);

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
            ArgumentNullException.ThrowIfNull(instance);

            if (null != Interlocked.CompareExchange(ref s_globalInstanceForTrackerSupport, instance, null))
            {
                throw new InvalidOperationException(SR.InvalidOperation_ResetGlobalComWrappersInstance);
            }

            SetGlobalInstanceRegisteredForTrackerSupport(instance.id);
        }


        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ComWrappers_SetGlobalInstanceRegisteredForTrackerSupport")]
        [SuppressGCTransition]
        private static partial void SetGlobalInstanceRegisteredForTrackerSupport(long id);

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
        [SupportedOSPlatform("windows")]
        public static void RegisterForMarshalling(ComWrappers instance)
        {
            ArgumentNullException.ThrowIfNull(instance);

            if (null != Interlocked.CompareExchange(ref s_globalInstanceForMarshalling, instance, null))
            {
                throw new InvalidOperationException(SR.InvalidOperation_ResetGlobalComWrappersInstance);
            }

            // Indicate to the runtime that a global instance has been registered for marshalling.
            // This allows the native runtime know to call into the managed ComWrappers only if a
            // global instance is registered for marshalling.
            SetGlobalInstanceRegisteredForMarshalling(instance.id);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ComWrappers_SetGlobalInstanceRegisteredForMarshalling")]
        [SuppressGCTransition]
        private static partial void SetGlobalInstanceRegisteredForMarshalling(long id);

        /// <summary>
        /// Get the runtime provided IUnknown implementation.
        /// </summary>
        /// <param name="fpQueryInterface">Function pointer to QueryInterface.</param>
        /// <param name="fpAddRef">Function pointer to AddRef.</param>
        /// <param name="fpRelease">Function pointer to Release.</param>
        protected static void GetIUnknownImpl(out IntPtr fpQueryInterface, out IntPtr fpAddRef, out IntPtr fpRelease)
            => GetIUnknownImplInternal(out fpQueryInterface, out fpAddRef, out fpRelease);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ComWrappers_GetIUnknownImpl")]
        private static partial void GetIUnknownImplInternal(out IntPtr fpQueryInterface, out IntPtr fpAddRef, out IntPtr fpRelease);

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
