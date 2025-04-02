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

        internal TRet UseKey<TState, TRet>(
            TState state,
            Func<TState, ReadOnlySpan<byte>, TRet> func)
        {
            bool addedRef = false;

            try
            {
                DangerousAddRef(ref addedRef);
                return func(state, DangerousKeySpan);
            }
            finally
            {
                if (addedRef)
                {
                    DangerousRelease();
                }
            }
        }

        internal TRet UseKey<TState, TRet>(
            ReadOnlySpan<byte> state1,
            TState state2,
            Func<ReadOnlySpan<byte>, TState, ReadOnlySpan<byte>, TRet> func)
        {
            bool addedRef = false;

            try
            {
                DangerousAddRef(ref addedRef);
                return func(state1, state2, DangerousKeySpan);
            }
            finally
            {
                if (addedRef)
                {
                    DangerousRelease();
                }
            }
        }
    }
}
