// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.Java
{
    [CLSCompliant(false)]
    [SupportedOSPlatform("android")]
    public static partial class JavaMarshal
    {
        public static unsafe void Initialize(delegate* unmanaged<MarkCrossReferencesArgs*, void> markCrossReferences)
        {
            ArgumentNullException.ThrowIfNull(markCrossReferences);

            if (!InitializeInternal((IntPtr)markCrossReferences))
            {
                throw new InvalidOperationException(SR.InvalidOperation_ReinitializeJavaMarshal);
            }
        }

        public static unsafe GCHandle CreateReferenceTrackingHandle(object obj, void* context)
        {
            ArgumentNullException.ThrowIfNull(obj);
            return GCHandle.FromIntPtr(RuntimeImports.RhHandleAllocCrossReference(obj, (IntPtr)context));
        }

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
