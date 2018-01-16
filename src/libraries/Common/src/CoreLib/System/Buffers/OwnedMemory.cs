// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
    public abstract class OwnedMemory<T> : IDisposable, IRetainable
    {
        public abstract int Length { get; }

        public abstract Span<T> Span { get; }

        public Memory<T> Memory
        {
            get 
            {
                if (IsDisposed) 
                {
                    ThrowHelper.ThrowObjectDisposedException(nameof(OwnedMemory<T>), ExceptionResource.Memory_ThrowIfDisposed);
                }
                return new Memory<T>(owner: this, 0, Length);
            }
        }

        public abstract MemoryHandle Pin();

        protected internal abstract bool TryGetArray(out ArraySegment<T> arraySegment);

        public void Dispose()
        {
            if (IsRetained) 
            {
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.Memory_OutstandingReferences);
            }
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);

        protected abstract bool IsRetained { get; }

        public abstract bool IsDisposed { get; }

        public abstract void Retain();

        public abstract bool Release();

    }
}
