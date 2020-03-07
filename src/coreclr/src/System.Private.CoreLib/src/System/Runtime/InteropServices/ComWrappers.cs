// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Threading;
using System.Runtime.CompilerServices;
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
        /// Ignore the internal cache when creating an object.
        /// </summary>
        IgnoreCache = 2,
    }

    /// <summary>
    /// Class for managing wrappers of COM IUnknown types.
    /// </summary>
    [CLSCompliant(false)]
    public abstract class ComWrappers
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
            /// Must be pinned memory that is owned by the implementer of <see cref="ComputeVtables(object, CreateComInterfaceFlags, out int)"/>.
            /// The memory must live as long as any COM interface consuming the table exists.
            /// </summary>
            public IntPtr Vtable;
        }

        /// <summary>
        /// ABI for function dispatch of a COM interface.
        /// </summary>
        public struct ComInterfaceDispatch
        {
            public IntPtr vftbl;

            /// <summary>
            /// Given a <see cref="System.IntPtr"/> from a generated VTable, convert to the target type.
            /// </summary>
            /// <typeparam name="T">Desired type.</typeparam>
            /// <param name="dispatchPtr">Pointer supplied to VTable function entry.</param>
            /// <returns>Instance of type associated with dispatched function call.</returns>
            public static unsafe T GetInstance<T>(ComInterfaceDispatch* dispatchPtr) where T : class
            {
                // See the CCW dispatch section in the runtime for details on the masking below.
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
        /// Globally registered instance of the ComWrappers class.
        /// </summary>
        private static ComWrappers? s_globalInstance;

        /// <summary>
        /// Create an COM representation of the supplied object that can be passed to an non-managed environment.
        /// </summary>
        /// <param name="instance">A GC Handle to the managed object to expose outside the .NET runtime.</param>
        /// <param name="flags">Flags used to configure the generated interface.</param>
        /// <returns>The generated COM interface that can be passed outside the .NET runtime.</returns>
        public IntPtr GetOrCreateComInterfaceForObject(object instance, CreateComInterfaceFlags flags)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            ComWrappers impl = this;
            return GetOrCreateComInterfaceForObjectInternal(ObjectHandleOnStack.Create(ref impl), ObjectHandleOnStack.Create(ref instance), flags);
        }

        [DllImport(RuntimeHelpers.QCall)]
        private static extern IntPtr GetOrCreateComInterfaceForObjectInternal(ObjectHandleOnStack comWrappersImpl, ObjectHandleOnStack instance, CreateComInterfaceFlags flags);

        /// <summary>
        /// Compute the desired VTables for <paramref name="obj"/> respecting the values of <paramref name="flags"/>.
        /// </summary>
        /// <param name="obj">Target of the returned VTables.</param>
        /// <param name="flags">Flags used to compute VTables.</param>
        /// <param name="count">The number of elements contained in the returned memory.</param>
        /// <returns><see cref="ComInterfaceEntry" /> pointer containing memory for all COM interface entries.</returns>
        /// <remarks>
        /// All memory returned from this function must either be unmanaged memory, pinned managed memory, or have been
        /// allocated with the <see cref="System.Runtime.CompilerServices.RuntimeHelpers.AllocateTypeAssociatedMemory(Type, int)"/> API.
        /// </remarks>
        protected unsafe abstract ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count);

        // Call to execute the abstract instance function
        internal static unsafe void* CallComputeVtables(ComWrappers? comWrappersImpl, object obj, CreateComInterfaceFlags flags, out int count)
            => (comWrappersImpl ?? s_globalInstance!).ComputeVtables(obj, flags, out count);

        /// <summary>
        /// Get the currently registered managed object or creates a new managed object and registers it.
        /// </summary>
        /// <param name="externalComObject">Object to import for usage into the .NET runtime.</param>
        /// <param name="flags">Flags used to describe the external object.</param>
        /// <returns>Returns a managed object associated with the supplied external COM object.</returns>
        public object GetOrCreateObjectForComInstance(IntPtr externalComObject, CreateObjectFlags flags)
        {
            if (externalComObject == IntPtr.Zero)
                throw new ArgumentNullException(nameof(externalComObject));

            ComWrappers impl = this;
            object? retValue = null;
            GetOrCreateObjectForComInstanceInternal(ObjectHandleOnStack.Create(ref impl), externalComObject, flags, ObjectHandleOnStack.Create(ref retValue));

            return retValue!;
        }

        [DllImport(RuntimeHelpers.QCall)]
        private static extern void GetOrCreateObjectForComInstanceInternal(ObjectHandleOnStack comWrappersImpl, IntPtr externalComObject, CreateObjectFlags flags, ObjectHandleOnStack retValue);

        /// <summary>
        /// Create a managed object for the object pointed at by <paramref name="agileObjectRef"/> respecting the values of <paramref name="flags"/>.
        /// </summary>
        /// <param name="externalComObject">Object to import for usage into the .NET runtime.</param>
        /// <param name="agileObjectRef">IAgileReference pointing at the object to import into the .NET runtime.</param>
        /// <param name="flags">Flags used to describe the external object.</param>
        /// <returns>Returns a managed object associated with the supplied external COM object.</returns>
        /// <remarks>
        /// The <paramref name="agileObjectRef"/> is an <see href="https://docs.microsoft.com/windows/win32/api/objidl/nn-objidl-iagilereference">IAgileReference</see> instance. This type should be used to ensure the associated external object, if not known to be free-threaded, is released from the correct COM apartment.
        /// </remarks>
        protected abstract object CreateObject(IntPtr externalComObject, IntPtr agileObjectRef, CreateObjectFlags flags);

        // Call to execute the abstract instance function
        internal static object CallCreateObject(ComWrappers? comWrappersImpl, IntPtr externalComObject, IntPtr agileObjectRef, CreateObjectFlags flags)
            => (comWrappersImpl ?? s_globalInstance!).CreateObject(externalComObject, agileObjectRef, flags);

        /// <summary>
        /// Called when a request is made for a collection of objects to be released.
        /// </summary>
        /// <param name="objects">Collection of objects to release.</param>
        /// <remarks>
        /// The default implementation of this function throws <see cref="System.NotImplementedException"/>.
        /// </remarks>
        protected virtual void ReleaseObjects(IEnumerable objects)
        {
            throw new NotImplementedException();
        }

        // Call to execute the virtual instance function
        internal static void CallReleaseObjects(ComWrappers? comWrappersImpl, IEnumerable objects)
            => (comWrappersImpl ?? s_globalInstance!).ReleaseObjects(objects);

        /// <summary>
        /// Register this class's implementation to be used when a Reference Tracker Host instance is requested from another runtime.
        /// </summary>
        /// <remarks>
        /// This function can only be called a single time. Subsequent calls to this function will result
        /// in a <see cref="System.InvalidOperationException"/> being thrown.
        /// </remarks>
        public void RegisterForReferenceTrackerHost()
        {
            if (null != Interlocked.CompareExchange(ref s_globalInstance, this, null))
            {
                throw new InvalidOperationException(SR.InvalidOperation_ResetReferenceTrackerHostCallbacks);
            }
        }

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
    }
}