// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        /// Ignore the internal cache when creating a instance.
        /// </summary>
        IgnoreCache = 2,

        /// <summary>
        /// Flag used to indicate the COM interface should implement <see href="https://docs.microsoft.com/windows/win32/api/windows.ui.xaml.hosting.referencetracker/nn-windows-ui-xaml-hosting-referencetracker-ireferencetrackertarget">IReferenceTrackerTarget</see>.
        /// When this flag is passed, the resulting COM interface will have an internal implementation of IUnknown
        /// and as such none should be supplied by the caller.
        /// </summary>
        TrackerSupport = 4,
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
        /// Create an COM representation of the supplied object that can be passed to an non-managed environment.
        /// </summary>
        /// <param name="instance">A GC Handle to the managed object to expose outside the .NET runtime.</param>
        /// <param name="flags">Flags used to configure the generated interface.</param>
        /// <returns>The generated COM interface that can be passed outside the .NET runtime.</returns>
        public IntPtr GetOrCreateComInterfaceForObject(object instance, CreateComInterfaceFlags flags)
        {
            throw new NotImplementedException();
        }

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

        /// <summary>
        /// Get the currently registered managed object or creates a new managed object and registers it.
        /// </summary>
        /// <param name="externalComObject">Object to import for usage into the .NET runtime.</param>
        /// <param name="flags">Flags used to describe the external object.</param>
        /// <returns>Returns a managed object associated with the supplied external COM object.</returns>
        public object GetOrCreateObjectForComInstance(IntPtr externalComObject, CreateObjectFlags flags)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Create a managed object for <paramref name="externalComObject"/> respecting the values of <paramref name="flags"/>.
        /// </summary>
        /// <param name="externalComObject">Object to import for usage into the .NET runtime.</param>
        /// <param name="flags">Flags used to describe the external object.</param>
        /// <returns>Returns a managed object associated with the supplied external COM object.</returns>
        protected abstract object CreateObject(IntPtr externalComObject, CreateObjectFlags flags);

        /// <summary>
        /// Register this class's implementation to be used when a Reference Tracker Host instance is requested from another runtime.
        /// </summary>
        /// <remarks>
        /// This function can only be called a single time. Subsequent calls to this function will result
        /// in a <see cref="System.InvalidOperationException"/> being thrown.
        /// </remarks>
        public void RegisterForReferenceTrackerHost()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get the runtime provided IUnknown implementation.
        /// </summary>
        /// <param name="fpQueryInterface">Function pointer to QueryInterface.</param>
        /// <param name="fpAddRef">Function pointer to AddRef.</param>
        /// <param name="fpRelease">Function pointer to Release.</param>
        /// [TODO] Consider just returning a struct. Perhaps an Enum for input?
        protected static void GetIUnknownImpl(out IntPtr fpQueryInterface, out IntPtr fpAddRef, out IntPtr fpRelease)
        {
            throw new NotImplementedException();
        }
    }
}