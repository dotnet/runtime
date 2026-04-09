// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace System.Reflection.Internal
{
    /// <summary>
    /// Provides helpers to decode strings from unmanaged memory to System.String while avoiding
    /// intermediate allocation.
    /// </summary>
    internal static unsafe class EncodingHelper
    {
        // Size of pooled buffers. The vast majority of metadata strings
        // are quite small so we don't need to waste memory with large buffers.
        public const int PooledBufferSize = 200;

        // Use AcquireBuffer(int) and ReleaseBuffer(byte[])
        // instead of the pool directly to implement the size check.
        private static readonly ObjectPool<byte[]> s_pool = new ObjectPool<byte[]>(() => new byte[PooledBufferSize]);

        public static string DecodeUtf8(byte* bytes, int byteCount, byte[] prefix, MetadataStringDecoder utf8Decoder)
        {
            Debug.Assert(utf8Decoder != null);

            if (prefix != null)
            {
                return DecodeUtf8Prefixed(bytes, byteCount, prefix, utf8Decoder);
            }

            if (byteCount == 0)
            {
                return string.Empty;
            }

            return utf8Decoder.GetString(bytes, byteCount);
        }

        private static string DecodeUtf8Prefixed(byte* bytes, int byteCount, byte[] prefix, MetadataStringDecoder utf8Decoder)
        {
            Debug.Assert(utf8Decoder != null);

            int prefixedByteCount = byteCount + prefix.Length;

            if (prefixedByteCount == 0)
            {
                return string.Empty;
            }

            byte[] buffer = AcquireBuffer(prefixedByteCount);

            prefix.CopyTo(buffer, 0);
            Marshal.Copy((IntPtr)bytes, buffer, prefix.Length, byteCount);

            string result;
            fixed (byte* prefixedBytes = &buffer[0])
            {
                result = utf8Decoder.GetString(prefixedBytes, prefixedByteCount);
            }

            ReleaseBuffer(buffer);
            return result;
        }

        private static byte[] AcquireBuffer(int byteCount)
        {
            if (byteCount > PooledBufferSize)
            {
                return new byte[byteCount];
            }

            return s_pool.Allocate();
        }

        private static void ReleaseBuffer(byte[] buffer)
        {
            if (buffer.Length == PooledBufferSize)
            {
                s_pool.Free(buffer);
            }
        }
    }
}
