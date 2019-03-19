// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Buffers
{
    public static partial class BoundedMemory
    {
        public static System.Buffers.BoundedMemory<T> Allocate<T>(int elementCount, System.Buffers.PoisonPagePlacement placement = System.Buffers.PoisonPagePlacement.After) where T : unmanaged { throw null; }
        public static System.Buffers.BoundedMemory<T> AllocateFromExistingData<T>(System.ReadOnlySpan<T> data, System.Buffers.PoisonPagePlacement placement = System.Buffers.PoisonPagePlacement.After) where T : unmanaged { throw null; }
        public static System.Buffers.BoundedMemory<T> AllocateFromExistingData<T>(T[] data, System.Buffers.PoisonPagePlacement placement = System.Buffers.PoisonPagePlacement.After) where T : unmanaged { throw null; }
    }
    public abstract partial class BoundedMemory<T> : IDisposable where T : unmanaged
    {
        public abstract bool IsReadonly { get; }
        public abstract System.Memory<T> Memory { get; }
        public abstract System.Span<T> Span { get; }
        public abstract void Dispose();
        public abstract void MakeReadonly();
        public abstract void MakeWriteable();
    }
    public enum PoisonPagePlacement
    {
        After = 0,
        Before = 1,
    }
}
