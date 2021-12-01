// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System
{
    public partial class WeakReference
    {
        private bool trackResurrection;
        private GCHandle handle;

        public virtual bool IsAlive => Target != null;

        public virtual object? Target
        {
            get
            {
                if (!handle.IsAllocated)
                    return null;
                return handle.Target;
            }
            set
            {
                handle.Target = value;
            }
        }

        public virtual bool TrackResurrection => IsTrackResurrection();

        ~WeakReference()
        {
            handle.Free();
        }

        private void Create(object? target, bool trackResurrection)
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

        private bool IsTrackResurrection() => trackResurrection;
    }
}
