// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;

namespace Melanzana.CodeSign.Blobs
{
    public class CmsWrapperBlob
    {
        public static byte[] Create(
            byte[] dataToSign,
            HashType[] hashTypes,
            byte[][] cdHashes)
        {
            if (dataToSign == null)
                throw new ArgumentNullException(nameof(dataToSign));
            if (hashTypes == null)
                throw new ArgumentNullException(nameof(hashTypes));
            if (cdHashes == null)
                throw new ArgumentNullException(nameof(cdHashes));
            if (hashTypes.Length != cdHashes.Length)
                throw new ArgumentException($"Length of hashType ({hashTypes.Length} is different from length of cdHashes ({cdHashes.Length})");

            var adhocBlobBuffer = new byte[8];
            BinaryPrimitives.WriteUInt32BigEndian(adhocBlobBuffer.AsSpan(0, 4), (uint)BlobMagic.CmsWrapper);
            BinaryPrimitives.WriteUInt32BigEndian(adhocBlobBuffer.AsSpan(4, 4), (uint)adhocBlobBuffer.Length);
            return adhocBlobBuffer;
        }
    }
}
