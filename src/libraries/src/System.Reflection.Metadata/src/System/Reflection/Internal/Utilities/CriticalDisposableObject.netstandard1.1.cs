// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Reflection.Internal
{
    // HACK: CriticalFinalizerObject is not available in netstandard 1.x
    // Use CriticalHandle instead -- we don't actually use the handle,
    // just the fact that CriticalHandle derives from CriticalFinalizerObject to ensure critical finalizer.
    internal abstract class CriticalDisposableObject : CriticalHandle
    {
        public CriticalDisposableObject()
            : base(IntPtr.Zero)
        {
        }

        public sealed override bool IsInvalid => true;

        protected sealed override bool ReleaseHandle() =>
            throw ExceptionUtilities.Unreachable;

        protected new void SetHandle(IntPtr handle) =>
            throw ExceptionUtilities.Unreachable;

        protected sealed override void Dispose(bool disposing)
        {
            // do not call base dispose
            Release();

            if (disposing)
            {
                GC.SuppressFinalize(this);
            }
        }

        protected abstract void Release();
    }
}