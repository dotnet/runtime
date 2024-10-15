// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection
{
    //
    // We can destroy the unmanaged part of collectible type only after the managed part is definitely gone and thus
    // nobody can call/allocate/reference anything related to the collectible assembly anymore. A call to finalizer
    // alone does not guarantee that the managed part is gone. A malicious code can keep a reference to some object
    // in a way that it survives finalization, or we can be running during shutdown where everything is finalized.
    //
    // The unmanaged LoaderAllocator keeps a reference to the managed LoaderAllocator in long weak handle. If the long
    // weak handle is null, we can be sure that the managed part of the LoaderAllocator is definitely gone and that it
    // is safe to destroy the unmanaged part. Unfortunately, we can not perform the above check in a finalizer on the
    // LoaderAllocator, but it can be performed on a helper object.
    //
    // The finalization does not have to be done using CriticalFinalizerObject. We have to go over all LoaderAllocators
    // during AppDomain shutdown anyway to avoid leaks e.g. if somebody stores reference to LoaderAllocator in a static.
    //
    internal sealed partial class LoaderAllocatorScout
    {
        // This field is set by the VM to atomically transfer the ownership to the managed loader allocator
        internal IntPtr m_nativeLoaderAllocator;

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "LoaderAllocator_Destroy")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool Destroy(IntPtr nativeLoaderAllocator);

        ~LoaderAllocatorScout()
        {
            if (m_nativeLoaderAllocator == IntPtr.Zero)
                return;

            // Destroy returns false if the managed LoaderAllocator is still alive.
            if (!Destroy(m_nativeLoaderAllocator))
            {
                // Somebody might have been holding a reference on us via weak handle.
                // We will keep trying. It will be hopefully released eventually.
                GC.ReRegisterForFinalize(this);
            }
        }
    }

    internal sealed class LoaderAllocator
    {
        private LoaderAllocator()
        {
            m_slots = new object[5];
            // m_slotsUsed = 0;

            m_scout = new LoaderAllocatorScout();
        }

#pragma warning disable CA1823, 414, 169
        private LoaderAllocatorScout m_scout;
        private object[] m_slots;
        internal CerHashtable<RuntimeMethodInfo, RuntimeMethodInfo> m_methodInstantiations;
        private int m_slotsUsed;
#pragma warning restore CA1823, 414, 169
    }
}
