// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System
{
    public partial class WeakReference<T>
    {
        private GCHandle handle;
        private bool trackResurrection;

        private T? Target
        {
            get
            {
                GCHandle h = handle;
                return h.IsAllocated ? (T)h.Target : null;
            }
        }

        ~WeakReference()
        {
            handle.Free();
        }

        private void Create(T target, bool trackResurrection)
        {
            if (trackResurrection)
            {
                this.trackResurrection = true;
                handle = GCHandle.Alloc(target, GCHandleType.WeakTrackResurrection);
            }
            else
            {
                handle = GCHandle.Alloc(target, GCHandleType.Weak);
            }
        }

        public void SetTarget(T target)
        {
            handle.Target = target;
        }

        private bool IsTrackResurrection() => trackResurrection;
    }
}
