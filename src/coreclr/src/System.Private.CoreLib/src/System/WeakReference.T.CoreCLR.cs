// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Runtime.CompilerServices;

namespace System
{
    public sealed partial class WeakReference<T> : ISerializable
        where T : class?
    {
        // This field is not a regular GC handle. It can have a special values that are used to prevent a race condition between setting the target and finalization.
        internal IntPtr m_handle;

        public void SetTarget(T target)
        {
            this.Target = target;
        }

        // This is property for better debugging experience (VS debugger shows values of properties when you hover over the variables)
        [MaybeNull]
        private extern T Target
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
            [MethodImpl(MethodImplOptions.InternalCall)]
            set;
        }

        // Free all system resources associated with this reference.
        //
        // Note: The WeakReference<T> finalizer is not usually run, but
        // treated specially in gc.cpp's ScanForFinalization
        // This is needed for subclasses deriving from WeakReference<T>, however.
        // Additionally, there may be some cases during shutdown when we run this finalizer.
        [MethodImpl(MethodImplOptions.InternalCall)]
        extern ~WeakReference();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void Create(T target, bool trackResurrection);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern bool IsTrackResurrection();
    }
}
