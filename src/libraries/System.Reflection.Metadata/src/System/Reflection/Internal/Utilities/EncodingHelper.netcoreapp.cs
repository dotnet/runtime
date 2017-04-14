// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Reflection.Internal
{
    /// <summary>
    /// Provides helpers to decode strings from unmanaged memory to System.String while avoiding
    /// intermediate allocation.
    /// </summary>
    internal static unsafe class EncodingHelper
    {
        public static string DecodeUtf8(byte* bytes, int byteCount, byte[] prefix, MetadataStringDecoder utf8Decoder)
        {
            Debug.Assert(utf8Decoder != null);

            if (prefix != null)
            {
                return DecodeUtf8Prefixed(bytes, byteCount, prefix, utf8Decoder);
            }

            if (byteCount == 0)
            {
                return String.Empty;
            }

            return utf8Decoder.GetString(bytes, byteCount);
        }

        private static string DecodeUtf8Prefixed(byte* bytes, int byteCount, byte[] prefix, MetadataStringDecoder utf8Decoder)
        {
            Debug.Assert(utf8Decoder != null);

            int prefixedByteCount = byteCount + prefix.Length;

            if (prefixedByteCount == 0)
            {
                return String.Empty;
            }

            byte[] buffer = ArrayPool<byte>.Shared.Rent(prefixedByteCount);

            prefix.CopyTo(buffer, 0);
            Marshal.Copy((IntPtr)bytes, buffer, prefix.Length, byteCount);

            string result;
            fixed (byte* prefixedBytes = &buffer[0])
            {
                result = utf8Decoder.GetString(prefixedBytes, prefixedByteCount);
            }

            ArrayPool<byte>.Shared.Return(buffer);
            return result;
        }

        // Test hook to force portable implementation and ensure light is functioning.
        internal static bool TestOnly_LightUpEnabled
        {
            get { return true; }
            set { }
        }
    }
}
