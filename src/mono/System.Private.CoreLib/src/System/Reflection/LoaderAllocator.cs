// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection
{
    // See src/coreclr/System.Private.CoreLib/src/System/Reflection/LoaderAllocator.cs for
    // more comments
    internal sealed class LoaderAllocatorScout
    {
        internal IntPtr m_native;

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool Destroy(IntPtr native);

        internal LoaderAllocatorScout(IntPtr native)
        {
            m_native = native;
        }

        ~LoaderAllocatorScout()
        {
            if (!Destroy(m_native))
            {
                GC.ReRegisterForFinalize(this);
            }
        }
    }

    //
    // This object is allocated by the runtime, every object
    // in a collectible alc has an implicit reference to it, maintained by
    // the GC, or an explicit reference through a field.
    //
    [StructLayout(LayoutKind.Sequential)]
    internal sealed class LoaderAllocator
    {
#region Sync with MonoManagedLoaderAllocator in object-internals.h
#pragma warning disable CA1823, 414, 169
        private LoaderAllocatorScout m_scout;
        // These point to objects created by the runtime which are kept
        // alive by this LoaderAllocator
        private object[]? m_slots;
        private object[]? m_hashes;
        private int m_nslots;
#pragma warning restore CA1823, 414, 169
#endregion

        private LoaderAllocator(IntPtr native)
        {
            m_scout = new LoaderAllocatorScout(native);
        }
    }
}
