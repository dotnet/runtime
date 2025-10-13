// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.Java
{
    /// <summary>
    /// Provides helpers to create and manage GC handles used for tracking references
    /// between the managed runtime and a Java VM. These APIs allow managed objects
    /// to be referenced from native Java code so the runtime can participate in
    /// cross-reference processing and correctly control object lifetime across
    /// the managed/native boundary.
    /// </summary>
    [CLSCompliant(false)]
    [SupportedOSPlatform("android")]
    public static partial class JavaMarshal
    {
        /// <summary>
        /// Initializes the Java marshal subsystem with a callback used when the runtime
        /// needs to mark managed objects that are referenced from Java during cross-
        /// reference processing.
        /// </summary>
        /// <param name="markCrossReferences">A pointer to an unmanaged callback that
        /// will be invoked to enumerate or mark managed objects referenced from Java
        /// during a cross-reference sweep. The callback is expected to accept a
        /// <see cref="MarkCrossReferencesArgs"/> pointer describing the objects to mark.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="markCrossReferences"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the subsystem cannot be initialized or is reinitialized.</exception>
        /// <exception cref="PlatformNotSupportedException">Thrown when the runtime or platform does not support Java cross-reference marshalling.</exception>
        /// <remarks>
        /// Only a single initialization is supported for the process. The runtime
        /// stores the provided function pointer and will invoke it from internal
        /// runtime code when cross-reference marking is required.
        /// Additionally, this callback must be implemented in unmanaged code.
        /// </remarks>
        public static unsafe void Initialize(delegate* unmanaged<MarkCrossReferencesArgs*, void> markCrossReferences)
        {
            ArgumentNullException.ThrowIfNull(markCrossReferences);

            if (!InitializeInternal((IntPtr)markCrossReferences))
            {
                throw new InvalidOperationException(SR.InvalidOperation_ReinitializeJavaMarshal);
            }
        }

        /// <summary>
        /// Creates a GC handle that native Java code can hold to reference a managed
        /// object. The handle prevents the object from being reclaimed while the
        /// native side holds the reference, and an opaque <paramref name="context"/>
        /// value can be associated with the handle for later retrieval.
        /// </summary>
        /// <param name="obj">The managed object to be referenced from native code.</param>
        /// <param name="context">An opaque pointer-sized value that will be associated
        /// with the handle and can be retrieved by the runtime via <see cref="GetContext(GCHandle)"/>.
        /// Callers may use this to store native-side state or identifiers alongside
        /// the handle.</param>
        /// <returns>A <see cref="GCHandle"/> that represents the allocated reference-tracking handle.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="obj"/> is null.</exception>
        /// <exception cref="PlatformNotSupportedException">Thrown when the runtime or platform does not support Java cross-reference marshalling.</exception>
        public static unsafe GCHandle CreateReferenceTrackingHandle(object obj, void* context)
        {
            ArgumentNullException.ThrowIfNull(obj);
            return GCHandle.FromIntPtr(RuntimeImports.RhHandleAllocCrossReference(obj, (IntPtr)context));
        }

        /// <summary>
        /// Retrieves the opaque context pointer associated with a reference-tracking
        /// GC handle previously created using <see cref="CreateReferenceTrackingHandle(object, void*)"/>.
        /// </summary>
        /// <param name="obj">The <see cref="GCHandle"/> whose context should be returned.</param>
        /// <returns>The opaque context pointer associated with the handle.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the provided handle is null or does not represent a reference-tracking handle.</exception>
        /// <exception cref="PlatformNotSupportedException">Thrown when the runtime or platform does not support Java cross-reference marshalling.</exception>
        /// <remarks>
        /// The returned pointer is the exact value that was originally provided as
        /// the <paramref name="context"/> parameter when the handle was created.
        /// </remarks>
        public static unsafe void* GetContext(GCHandle obj)
        {
            IntPtr handle = GCHandle.ToIntPtr(obj);
            if (handle == IntPtr.Zero
                || !RuntimeImports.RhHandleTryGetCrossReferenceContext(handle, out nint context))
            {
                throw new InvalidOperationException(SR.InvalidOperation_IncorrectGCHandleType);
            }
            return (void*)context;
        }

        /// <summary>
        /// Completes processing of cross references after the runtime has invoked the
        /// callback provided to <see cref="Initialize" />. This notifies the runtime of
        /// handles that are no longer reachable from native Java code so the runtime
        /// can release or update them accordingly.
        /// </summary>
        /// <param name="crossReferences">A pointer to the structure containing cross-reference information produced during marking.</param>
        /// <param name="unreachableObjectHandles">A span of <see cref="GCHandle"/> values that were determined to be unreachable from the native side.</param>
        /// <exception cref="PlatformNotSupportedException">Thrown when the runtime or platform does not support Java cross-reference marshalling.</exception>
        public static unsafe void FinishCrossReferenceProcessing(
            MarkCrossReferencesArgs* crossReferences,
            ReadOnlySpan<GCHandle> unreachableObjectHandles)
        {
            fixed (GCHandle* handlesPtr = unreachableObjectHandles)
            {
                FinishCrossReferenceProcessingBridge(
                    crossReferences,
                    (nuint)unreachableObjectHandles.Length,
                    handlesPtr);
            }
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "JavaMarshal_Initialize")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool InitializeInternal(IntPtr markCrossReferences);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "JavaMarshal_FinishCrossReferenceProcessing")]
        internal static unsafe partial void FinishCrossReferenceProcessingBridge(
            MarkCrossReferencesArgs* crossReferences,
            nuint numHandles,
            GCHandle* unreachableObjectHandles);
    }
}
