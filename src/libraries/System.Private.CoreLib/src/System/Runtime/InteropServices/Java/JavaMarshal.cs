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
        public static unsafe void Initialize(
            // Callback used to perform the marking of SCCs.
            delegate* unmanaged<
                nint,                               // Length of SCC collection
                StronglyConnectedComponent*,        // SCC collection
                nint,                               // Length of CCR collection
                ComponentCrossReference*,           // CCR collection
                void> markCrossReferences)
        {
#if NATIVEAOT
            throw new NotImplementedException();
#elif MONO
            throw new NotSupportedException();
#else
            ArgumentNullException.ThrowIfNull(markCrossReferences);

            if (!InitializeInternal((IntPtr)markCrossReferences))
            {
                throw new InvalidOperationException(SR.InvalidOperation_ReinitializeJavaMarshal);
            }
#endif
        }

        public static GCHandle CreateReferenceTrackingHandle(object obj, IntPtr context)
        {
#if NATIVEAOT
            throw new NotImplementedException();
#elif MONO
            throw new NotSupportedException();
#else
            ArgumentNullException.ThrowIfNull(obj);

            IntPtr handle = CreateReferenceTrackingHandleInternal(ObjectHandleOnStack.Create(ref obj), context);
            return GCHandle.FromIntPtr(handle);
#endif
        }

        public static IntPtr GetContext(GCHandle obj)
        {
#if NATIVEAOT
            throw new NotImplementedException();
#elif MONO
            throw new NotSupportedException();
#else
            IntPtr handle = GCHandle.ToIntPtr(obj);
            if (handle == IntPtr.Zero
                || !GetContextInternal(handle, out IntPtr context))
            {
                throw new InvalidOperationException(SR.InvalidOperation_IncorrectGCHandleType);
            }

            return context;
#endif
        }

        public static unsafe void ReleaseMarkCrossReferenceResources(
            Span<StronglyConnectedComponent> sccs,
            Span<ComponentCrossReference> ccrs)
        {
#if NATIVEAOT
            throw new NotImplementedException();
#elif MONO
            throw new NotSupportedException();
#else
            ReleaseMarkCrossReferenceResources(
                sccs.Length,
                Unsafe.AsPointer(ref MemoryMarshal.GetReference(sccs)),
                Unsafe.AsPointer(ref MemoryMarshal.GetReference(ccrs)));
#endif
        }

#if CORECLR
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "JavaMarshal_Initialize")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool InitializeInternal(IntPtr callback);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "JavaMarshal_CreateReferenceTrackingHandle")]
        private static partial IntPtr CreateReferenceTrackingHandleInternal(ObjectHandleOnStack obj, IntPtr context);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "JavaMarshal_ReleaseMarkCrossReferenceResources")]
        private static unsafe partial void ReleaseMarkCrossReferenceResources(int length, void* sccs, void* ccrs);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "JavaMarshal_GetContext")]
        [SuppressGCTransition]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetContextInternal(IntPtr handle, out IntPtr context);
#endif
    }
}
