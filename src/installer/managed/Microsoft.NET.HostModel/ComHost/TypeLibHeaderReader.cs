// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Buffers.Binary;

namespace Microsoft.NET.HostModel.ComHost
{
    class TypeLibHeaderReader
    {
        private byte[] tlbBytes;
        private byte[] _tlbFileBytes;

        public TypeLibHeaderReader(byte[] tlbBytes)
        {
            this.tlbBytes = tlbBytes;
        }

        private bool SkipString(ref ReadOnlySpan<byte> span)
        {
            if (!ReadShort(ref span, out ushort strLen))
            {
                return false;
            }
            if (span.Length < strLen)
            {
                return false;
            }
            span = span.Slice(strLen);
            return true;
        }

        private bool ReadInt(ref ReadOnlySpan<byte> span, out int value)
        {
            if (!BinaryPrimitives.TryReadInt32LittleEndian(span, out value))
            {
                return false;
            }
            span = span.Slice(sizeof(int));
            return true;
        }

        private bool ReadShort(ref ReadOnlySpan<byte> span, out ushort value)
        {
            if (!BinaryPrimitives.TryReadUInt16LittleEndian(span, out value))
            {
                return false;
            }
            span = span.Slice(sizeof(int));
            return true;
        }

        private bool ReadGuid(ref ReadOnlySpan<byte> span, out Guid value)
        {
            const int GuidSize = 16;
            if (span.Length < GuidSize)
            {
                return false;
            }
            value = new Guid(span.Slice(0, 16).ToArray());
            span = span.Slice(16);
            return true;
        }

        public bool TryReadTypeLibNameAndVersion(out Guid typelibId, out Version version)
        {
            typelibId = default;
            version = default;
            var span = new ReadOnlySpan<byte>(tlbBytes);
            // Skip the first 12-byte header chunk
            span = span.Slice(12);
            if (!SkipString(ref span)) // Skip default TYPEINFO typeid
            {
                return false;
            }
            if (!SkipString(ref span)) // Skip docstring.
            {
                return false;
            }
            if (!SkipString(ref span)) // Skip helpfile name. We don't know the right way to map this for the manifest so drop it.
            {
                return false;
            }
            // Begin reading chunk 2
            if (!ReadInt(ref span, out _)) // help context
            {
                return false;
            }
            if (!ReadShort(ref span, out _)) // syskind
            {
                return false;
            }
            if (!ReadInt(ref span, out _)) // lcid
            {
                return false;
            }
            if (!ReadShort(ref span, out _)) // Tmp
            {
                return false;
            }
            if (!ReadShort(ref span, out _)) // skip the libFlags since there's no documentation on how to represent multiple in the manifest.
            {
                return false;
            }
            if (!ReadShort(ref span, out ushort majorVer))
            {
                return false;
            }
            if (!ReadShort(ref span, out ushort minorVer))
            {
                return false;
            }
            if (!ReadGuid(ref span , out typelibId))
            {
                return false;
            }
            version = new Version(majorVer, minorVer);
            return true;
        }
    }
}
