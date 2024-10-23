// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;

namespace Microsoft.NET.HostModel.MachO.CodeSign.Blobs
{
    internal static class CmsWrapperBlob
    {
        internal static byte[] Create(
            HashType[] hashTypes,
            byte[][] cdHashes)
        {
            if (hashTypes.Length != cdHashes.Length)
                throw new ArgumentException($"Length of hashType ({hashTypes.Length} is different from length of cdHashes ({cdHashes.Length})");

            var adhocBlobBuffer = new byte[8];
            BinaryPrimitives.WriteUInt32BigEndian(adhocBlobBuffer.AsSpan(0, 4), (uint)BlobMagic.CmsWrapper);
            BinaryPrimitives.WriteUInt32BigEndian(adhocBlobBuffer.AsSpan(4, 4), (uint)adhocBlobBuffer.Length);
            return adhocBlobBuffer;
        }
    }
}
