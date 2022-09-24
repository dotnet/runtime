// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System
{
    public partial class WeakReference
    {
        // Free all system resources associated with this reference.
        ~WeakReference()
        {
            IntPtr handle = Handle;
            if (handle != default(IntPtr))
            {
                GCHandle.InternalFree(handle);

                // keep the bit that indicates whether this reference was tracking resurrection
                m_handleAndKind &= TracksResurrectionBit;
            }
        }
    }
}
