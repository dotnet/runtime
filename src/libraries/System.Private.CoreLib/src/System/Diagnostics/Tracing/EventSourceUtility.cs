// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Text;

namespace System.Diagnostics.Tracing
{
    internal static class EventSourceUtility
    {
        internal static Guid GenerateGuidFromName(string name)
        {
            ReadOnlySpan<byte> namespaceBytes =
            [
                0x48, 0x2C, 0x2D, 0xB2, 0xC3, 0x90, 0x47, 0xC8,
                0x87, 0xF8, 0x1A, 0x15, 0xBF, 0xC1, 0x30, 0xFB,
            ];

            byte[] bytes = Encoding.BigEndianUnicode.GetBytes(name);
            Sha1ForNonSecretPurposes hash = default;
            hash.Start();
            hash.Append(namespaceBytes);
            hash.Append(bytes);
            Array.Resize(ref bytes, 16);
            hash.Finish(bytes);

            bytes[7] = unchecked((byte)((bytes[7] & 0x0F) | 0x50));    // Set high 4 bits of octet 7 to 5, as per RFC 4122
            return new Guid(bytes);
        }
    }
}
