// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Runtime.Versioning;

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

        /// <summary>
        /// Defined when COM aggregation is involved (that is an inner instance supplied).
        /// </summary>
        Aggregation = 4,

        /// <summary>
        /// Check if the supplied instance is actually a wrapper and if so return the underlying
        /// managed object rather than creating a new wrapper.
        /// </summary>
        /// <remarks>
        /// This matches the built-in RCW semantics for COM interop.
        /// </remarks>
        Unwrap = 8,
    }

    /// <summary>
    /// Class for managing wrappers of COM IUnknown types.
    /// </summary>
    [UnsupportedOSPlatform("android")]
    [UnsupportedOSPlatform("browser")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
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
        public partial struct ComInterfaceDispatch
        {
            public IntPtr Vtable;
        }

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

        /// <summary>
        /// Called when a request is made for a collection of objects to be released outside of normal object or COM interface lifetime.
        /// </summary>
        /// <param name="objects">Collection of objects to release.</param>
        protected abstract void ReleaseObjects(IEnumerable objects);
    }
}
