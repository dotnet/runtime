// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.Internal;
using System.Threading;

namespace System.Reflection.Metadata.Ecma335
{
    internal struct BlobHeap
    {
        private static byte[][] s_virtualValues;

        internal readonly MemoryBlock Block;
        private VirtualHeap _lazyVirtualHeap;

        internal BlobHeap(MemoryBlock block, MetadataKind metadataKind)
        {
            _lazyVirtualHeap = null;
            Block = block;

            if (s_virtualValues == null && metadataKind != MetadataKind.Ecma335)
            {
                var blobs = new byte[(int)BlobHandle.VirtualIndex.Count][];

                blobs[(int)BlobHandle.VirtualIndex.ContractPublicKeyToken] = new byte[]
                {
                    0xB0, 0x3F, 0x5F, 0x7F, 0x11, 0xD5, 0x0A, 0x3A
                };

                blobs[(int)BlobHandle.VirtualIndex.ContractPublicKey] = new byte[]
                {
                    0x00, 0x24, 0x00, 0x00, 0x04, 0x80, 0x00, 0x00, 0x94, 0x00, 0x00, 0x00, 0x06, 0x02, 0x00, 0x00,
                    0x00, 0x24, 0x00, 0x00, 0x52, 0x53, 0x41, 0x31, 0x00, 0x04, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00,
                    0x07, 0xD1, 0xFA, 0x57, 0xC4, 0xAE, 0xD9, 0xF0, 0xA3, 0x2E, 0x84, 0xAA, 0x0F, 0xAE, 0xFD, 0x0D,
                    0xE9, 0xE8, 0xFD, 0x6A, 0xEC, 0x8F, 0x87, 0xFB, 0x03, 0x76, 0x6C, 0x83, 0x4C, 0x99, 0x92, 0x1E,
                    0xB2, 0x3B, 0xE7, 0x9A, 0xD9, 0xD5, 0xDC, 0xC1, 0xDD, 0x9A, 0xD2, 0x36, 0x13, 0x21, 0x02, 0x90,
                    0x0B, 0x72, 0x3C, 0xF9, 0x80, 0x95, 0x7F, 0xC4, 0xE1, 0x77, 0x10, 0x8F, 0xC6, 0x07, 0x77, 0x4F,
                    0x29, 0xE8, 0x32, 0x0E, 0x92, 0xEA, 0x05, 0xEC, 0xE4, 0xE8, 0x21, 0xC0, 0xA5, 0xEF, 0xE8, 0xF1,
                    0x64, 0x5C, 0x4C, 0x0C, 0x93, 0xC1, 0xAB, 0x99, 0x28, 0x5D, 0x62, 0x2C, 0xAA, 0x65, 0x2C, 0x1D,
                    0xFA, 0xD6, 0x3D, 0x74, 0x5D, 0x6F, 0x2D, 0xE5, 0xF1, 0x7E, 0x5E, 0xAF, 0x0F, 0xC4, 0x96, 0x3D,
                    0x26, 0x1C, 0x8A, 0x12, 0x43, 0x65, 0x18, 0x20, 0x6D, 0xC0, 0x93, 0x34, 0x4D, 0x5A, 0xD2, 0x93
                };

                blobs[(int)BlobHandle.VirtualIndex.AttributeUsage_AllowSingle] = new byte[]
                {
                    // preamble:
                    0x01, 0x00,
                    // target (template parameter):
                    0x00, 0x00, 0x00, 0x00,
                    // named arg count:
                    0x01, 0x00,
                    // SERIALIZATION_TYPE_PROPERTY
                    0x54,
                    // ELEMENT_TYPE_BOOLEAN
                    0x02,
                    // "AllowMultiple".Length
                    0x0D,
                    // "AllowMultiple"
                    0x41, 0x6C, 0x6C, 0x6F, 0x77, 0x4D, 0x75, 0x6C, 0x74, 0x69, 0x70, 0x6C, 0x65,
                    // false
                    0x00
                };

                blobs[(int)BlobHandle.VirtualIndex.AttributeUsage_AllowMultiple] = new byte[]
                {
                    // preamble:
                    0x01, 0x00,
                    // target (template parameter):
                    0x00, 0x00, 0x00, 0x00,
                    // named arg count:
                    0x01, 0x00,
                    // SERIALIZATION_TYPE_PROPERTY
                    0x54,
                    // ELEMENT_TYPE_BOOLEAN
                    0x02,
                    // "AllowMultiple".Length
                    0x0D,
                    // "AllowMultiple"
                    0x41, 0x6C, 0x6C, 0x6F, 0x77, 0x4D, 0x75, 0x6C, 0x74, 0x69, 0x70, 0x6C, 0x65,
                    // true
                    0x01
                };

                s_virtualValues = blobs;
            }
        }

        internal byte[] GetBytes(BlobHandle handle)
        {
            if (handle.IsVirtual)
            {
                // consider: if we returned an ImmutableArray we wouldn't need to copy
                return GetVirtualBlobBytes(handle, unique: true);
            }

            int offset = handle.GetHeapOffset();
            int bytesRead;
            int numberOfBytes = Block.PeekCompressedInteger(offset, out bytesRead);
            if (numberOfBytes == BlobReader.InvalidCompressedInteger)
            {
                return EmptyArray<byte>.Instance;
            }

            return Block.PeekBytes(offset + bytesRead, numberOfBytes);
        }

        internal MemoryBlock GetMemoryBlock(BlobHandle handle)
        {
            if (handle.IsVirtual)
            {
                return GetVirtualHandleMemoryBlock(handle);
            }

            int offset, size;
            Block.PeekHeapValueOffsetAndSize(handle.GetHeapOffset(), out offset, out size);
            return Block.GetMemoryBlockAt(offset, size);
        }

        private MemoryBlock GetVirtualHandleMemoryBlock(BlobHandle handle)
        {
            var heap = VirtualHeap.GetOrCreateVirtualHeap(ref _lazyVirtualHeap);

            lock (heap)
            {
                if (!heap.TryGetMemoryBlock(handle.RawValue, out var block))
                {
                    block = heap.AddBlob(handle.RawValue, GetVirtualBlobBytes(handle, unique: false));
                }

                return block;
            }
        }

        internal BlobReader GetBlobReader(BlobHandle handle)
        {
            return new BlobReader(GetMemoryBlock(handle));
        }

        internal BlobHandle GetNextHandle(BlobHandle handle)
        {
            if (handle.IsVirtual)
            {
                return default(BlobHandle);
            }

            int offset, size;
            if (!Block.PeekHeapValueOffsetAndSize(handle.GetHeapOffset(), out offset, out size))
            {
                return default(BlobHandle);
            }

            int nextIndex = offset + size;
            if (nextIndex >= Block.Length)
            {
                return default(BlobHandle);
            }

            return BlobHandle.FromOffset(nextIndex);
        }

        internal byte[] GetVirtualBlobBytes(BlobHandle handle, bool unique)
        {
            BlobHandle.VirtualIndex index = handle.GetVirtualIndex();
            byte[] result = s_virtualValues[(int)index];

            switch (index)
            {
                case BlobHandle.VirtualIndex.AttributeUsage_AllowMultiple:
                case BlobHandle.VirtualIndex.AttributeUsage_AllowSingle:
                    result = (byte[])result.Clone();
                    handle.SubstituteTemplateParameters(result);
                    break;

                default:
                    if (unique)
                    {
                        result = (byte[])result.Clone();
                    }
                    break;
            }

            return result;
        }

        public string GetDocumentName(DocumentNameBlobHandle handle)
        {
            var blobReader = GetBlobReader(handle);

            // Spec: separator is an ASCII encoded character in range [0x01, 0x7F], or byte 0 to represent an empty separator.
            int separator = blobReader.ReadByte();
            if (separator > 0x7f)
            {
                throw new BadImageFormatException(SR.Format(SR.InvalidDocumentName, separator));
            }

            var pooledBuilder = PooledStringBuilder.GetInstance();
            var builder = pooledBuilder.Builder;
            bool isFirstPart = true;
            while (blobReader.RemainingBytes > 0)
            {
                if (separator != 0 && !isFirstPart)
                {
                    builder.Append((char)separator);
                }

                var partReader = GetBlobReader(blobReader.ReadBlobHandle());

                // TODO: avoid allocating temp string (https://github.com/dotnet/corefx/issues/2102)
                builder.Append(partReader.ReadUTF8(partReader.Length));
                isFirstPart = false;
            }

            return pooledBuilder.ToStringAndFree();
        }

        internal bool DocumentNameEquals(DocumentNameBlobHandle handle, string other, bool ignoreCase)
        {
            var blobReader = GetBlobReader(handle);

            // Spec: separator is an ASCII encoded character in range [0x01, 0x7F], or byte 0 to represent an empty separator.
            int separator = blobReader.ReadByte();
            if (separator > 0x7f)
            {
                return false;
            }

            int ignoreCaseMask = StringUtils.IgnoreCaseMask(ignoreCase);
            int otherIndex = 0;
            bool isFirstPart = true;
            while (blobReader.RemainingBytes > 0)
            {
                if (separator != 0 && !isFirstPart)
                {
                    if (otherIndex == other.Length || !StringUtils.IsEqualAscii(other[otherIndex], separator, ignoreCaseMask))
                    {
                        return false;
                    }

                    otherIndex++;
                }

                var partBlock = GetMemoryBlock(blobReader.ReadBlobHandle());

                int firstDifferenceIndex;
                var result = partBlock.Utf8NullTerminatedFastCompare(0, other, otherIndex, out firstDifferenceIndex, terminator: '\0', ignoreCase: ignoreCase);
                if (result == MemoryBlock.FastComparisonResult.Inconclusive)
                {
                    return GetDocumentName(handle).Equals(other, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
                }

                if (result == MemoryBlock.FastComparisonResult.Unequal ||
                    firstDifferenceIndex - otherIndex != partBlock.Length)
                {
                    return false;
                }

                otherIndex = firstDifferenceIndex;
                isFirstPart = false;
            }

            return otherIndex == other.Length;
        }
    }
}
