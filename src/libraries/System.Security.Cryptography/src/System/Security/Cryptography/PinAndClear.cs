// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
    internal struct PinAndClear : IDisposable
    {
        private byte[] _data;
        private PinnedGCHandle<byte[]> _gcHandle;

        internal static PinAndClear Track(byte[] data)
        {
            return new PinAndClear
            {
                _gcHandle = new PinnedGCHandle<byte[]>(data),
                _data = data,
            };
        }

        internal static PinAndClear CopyAndTrack(ReadOnlySpan<byte> data, out byte[] array)
        {
            byte[] buffer = new byte[data.Length];
            PinnedGCHandle<byte[]> handle = new(buffer);
            data.CopyTo(buffer);
            array = buffer;

            return new PinAndClear
            {
                _gcHandle = handle,
                _data = buffer,
            };
        }

        public void Dispose()
        {
            Array.Clear(_data);
            _gcHandle.Dispose();
        }
    }
}
