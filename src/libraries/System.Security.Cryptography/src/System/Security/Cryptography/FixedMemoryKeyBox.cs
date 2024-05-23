// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
    internal sealed unsafe class FixedMemoryKeyBox : SafeHandle
    {
        private readonly int _length;

        internal FixedMemoryKeyBox(ReadOnlySpan<byte> key) : base(IntPtr.Zero, ownsHandle: true)
        {
            void* memory = NativeMemory.Alloc((nuint)key.Length);
            key.CopyTo(new Span<byte>(memory, key.Length));
            SetHandle((IntPtr)memory);
            _length = key.Length;
        }

        internal ReadOnlySpan<byte> DangerousKeySpan => new ReadOnlySpan<byte>((void*)handle, _length);

        protected override bool ReleaseHandle()
        {
            CryptographicOperations.ZeroMemory(new Span<byte>((void*)handle, _length));
            NativeMemory.Free((void*)handle);
            return true;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }
}
