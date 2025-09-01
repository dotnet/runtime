// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class NetSecurityNative
    {
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct GssBuffer : IDisposable
        {
            internal ulong _length;
            internal byte* _data;

            internal int Copy(byte[] destination, int offset)
            {
                Debug.Assert(destination != null, "target destination cannot be null");
                Debug.Assert((offset >= 0 && offset < destination.Length) || destination.Length == 0, $"invalid offset {offset}");

                if (_data == null || _length == 0)
                {
                    return 0;
                }

                // Using Convert.ToInt32 to throw an exception in the unlikely event of too large value of _length
                int sourceLength = Convert.ToInt32(_length);
                int destinationAvailable = destination.Length - offset;  // amount of space in the given buffer
                if (sourceLength > destinationAvailable)
                {
                    throw new NetSecurityNative.GssApiException(SR.Format(SR.net_context_buffer_too_small, sourceLength, destinationAvailable));
                }

                Span.CopyTo(destination.AsSpan(offset, sourceLength));
                return sourceLength;
            }

            internal byte[] ToByteArray()
            {
                if (_data == null || _length == 0)
                {
                    return Array.Empty<byte>();
                }

                int destinationLength = Convert.ToInt32(_length);
                byte[] destination = new byte[destinationLength];
                Span.CopyTo(destination);
                return destination;
            }

            internal ReadOnlySpan<byte> Span => (_data != null && _length != 0) ?
                new ReadOnlySpan<byte>(_data, checked((int)_length)) :
                default;

            public void Dispose()
            {
                if (_data != null)
                {
                    Interop.NetSecurityNative.ReleaseGssBuffer(_data, _length);
                    _data = null;
                }

                _length = 0;
            }

#if DEBUG
            static GssBuffer()
            {
                // Verify managed size on both 32-bit and 64-bit matches the PAL_GssBuffer
                // native struct size, which is also padded on 32-bit.
                Debug.Assert(sizeof(GssBuffer) == 16);
            }
#endif
        }
    }
}
