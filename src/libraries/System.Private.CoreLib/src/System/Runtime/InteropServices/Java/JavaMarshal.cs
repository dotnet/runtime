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
#if NATIVEAOT
            throw new NotImplementedException();
#elif FEATURE_JAVAMARSHAL
            ArgumentNullException.ThrowIfNull(markCrossReferences);

            if (!InitializeInternal((IntPtr)markCrossReferences))
            {
                throw new InvalidOperationException(SR.InvalidOperation_ReinitializeJavaMarshal);
            }
#else
            throw new PlatformNotSupportedException();
#endif
        }

        public static unsafe GCHandle CreateReferenceTrackingHandle(object obj, void* context)
        {
#if NATIVEAOT
            throw new NotImplementedException();
#elif FEATURE_JAVAMARSHAL
            ArgumentNullException.ThrowIfNull(obj);

            IntPtr handle = CreateReferenceTrackingHandleInternal(ObjectHandleOnStack.Create(ref obj), context);
            return GCHandle.FromIntPtr(handle);
#else
            throw new PlatformNotSupportedException();
#endif
        }

        public static unsafe void* GetContext(GCHandle obj)
        {
#if NATIVEAOT
            throw new NotImplementedException();
#elif FEATURE_JAVAMARSHAL
            IntPtr handle = GCHandle.ToIntPtr(obj);
            if (handle == IntPtr.Zero
                || !GetContextInternal(handle, out void* context))
            {
                throw new InvalidOperationException(SR.InvalidOperation_IncorrectGCHandleType);
            }

            return context;
#else
            throw new PlatformNotSupportedException();
#endif
        }

        public static unsafe void FinishCrossReferenceProcessing(
            MarkCrossReferencesArgs* crossReferences,
            ReadOnlySpan<GCHandle> unreachableObjectHandles)
        {
#if NATIVEAOT
            throw new NotImplementedException();
#elif FEATURE_JAVAMARSHAL
            fixed (GCHandle* pHandles = unreachableObjectHandles)
            {
                FinishCrossReferenceProcessing(
                    crossReferences,
                    (nuint)unreachableObjectHandles.Length,
                    pHandles);
            }
#else
            throw new PlatformNotSupportedException();
#endif
        }

#if FEATURE_JAVAMARSHAL && !NATIVEAOT
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "JavaMarshal_Initialize")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool InitializeInternal(IntPtr callback);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "JavaMarshal_CreateReferenceTrackingHandle")]
        private static unsafe partial IntPtr CreateReferenceTrackingHandleInternal(ObjectHandleOnStack obj, void* context);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "JavaMarshal_FinishCrossReferenceProcessing")]
        private static unsafe partial void FinishCrossReferenceProcessing(MarkCrossReferencesArgs* crossReferences, nuint length, void* unreachableObjectHandles);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "JavaMarshal_GetContext")]
        [SuppressGCTransition]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static unsafe partial bool GetContextInternal(IntPtr handle, out void* context);
#endif
    }
}
