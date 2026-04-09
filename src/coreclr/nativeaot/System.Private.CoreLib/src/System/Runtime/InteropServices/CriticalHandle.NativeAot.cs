// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.ConstrainedExecution;

namespace System.Runtime.InteropServices
{
    public abstract partial class CriticalHandle : CriticalFinalizerObject, IDisposable
    {
        internal void SetHandleInternal(IntPtr handle)
        {
            SetHandle(handle);
        }

        internal IntPtr GetHandleInternal()
        {
            return this.handle;
        }
    }
}
