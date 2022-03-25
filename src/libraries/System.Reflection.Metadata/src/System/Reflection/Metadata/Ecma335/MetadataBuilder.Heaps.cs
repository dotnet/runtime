// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Internal;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Reflection.Metadata.Ecma335
{
    public sealed partial class MetadataBuilder
    {
        private sealed class HeapBlobBuilder : BlobBuilder
        {
            private int _capacityExpansion;

            public HeapBlobBuilder(int capacity)
                : base(capacity)
            {
            }

            protected override BlobBuilder AllocateChunk(int minimalSize)
            {
                return new HeapBlobBuilder(Math.Max(Math.Max(minimalSize, ChunkCapacity), _capacityExpansion));
            }

            internal void SetCapacity(int capacity)
            {
                _capacityExpansion = Math.Max(0, capacity - Count - FreeBytes);
            }
        }

        // #US heap
        private const int UserStringHeapSizeLimit = 0x01000000;
        private readonly Dictionary<string, UserStringHandle> _userStrings = new Dictionary<string, UserStringHandle>(256);
        private readonly HeapBlobBuilder _userStringBuilder = new HeapBlobBuilder(4 * 1024);
        private readonly int _userStringHeapStartOffset;

        // #String heap
        private readonly Dictionary<string, StringHandle> _strings = new Dictionary<string, StringHandle>(256);
        private readonly int _stringHeapStartOffset;
        private int _stringHeapCapacity = 4 * 1024;

        // #Blob heap
        private readonly Dictionary<ImmutableArray<byte>, BlobHandle> _blobs = new Dictionary<ImmutableArray<byte>, BlobHandle>(1024, ByteSequenceComparer.Instance);
        private readonly int _blobHeapStartOffset;
        private int _blobHeapSize;

        // #GUID heap
        private readonly Dictionary<Guid, GuidHandle> _guids = new Dictionary<Guid, GuidHandle>();
        private readonly HeapBlobBuilder _guidBuilder = new HeapBlobBuilder(16); // full metadata has just a single guid

        /// <summary>
        /// Creates a builder for metadata tables and heaps.
        /// </summary>
        /// <param name="userStringHeapStartOffset">
        /// Start offset of the User String heap.
        /// The cumulative size of User String heaps of all previous EnC generations. Should be 0 unless the metadata is EnC delta metadata.
        /// </param>
        /// <param name="stringHeapStartOffset">
        /// Start offset of the String heap.
        /// The cumulative size of String heaps of all previous EnC generations. Should be 0 unless the metadata is EnC delta metadata.
        /// </param>
        /// <param name="blobHeapStartOffset">
        /// Start offset of the Blob heap.
        /// The cumulative size of Blob heaps of all previous EnC generations. Should be 0 unless the metadata is EnC delta metadata.
        /// </param>
        /// <param name="guidHeapStartOffset">
        /// Start offset of the Guid heap.
        /// The cumulative size of Guid heaps of all previous EnC generations. Should be 0 unless the metadata is EnC delta metadata.
        /// </param>
        /// <exception cref="ImageFormatLimitationException">Offset is too big.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Offset is negative.</exception>
        /// <exception cref="ArgumentException"><paramref name="guidHeapStartOffset"/> is not a multiple of size of GUID.</exception>
        public MetadataBuilder(
            int userStringHeapStartOffset = 0,
            int stringHeapStartOffset = 0,
            int blobHeapStartOffset = 0,
            int guidHeapStartOffset = 0)
        {
            // -1 for the 0 we always write at the beginning of the heap:
            if (userStringHeapStartOffset >= UserStringHeapSizeLimit - 1)
            {
                Throw.HeapSizeLimitExceeded(HeapIndex.UserString);
            }

            if (userStringHeapStartOffset < 0)
            {
                Throw.ArgumentOutOfRange(nameof(userStringHeapStartOffset));
            }

            if (stringHeapStartOffset < 0)
            {
                Throw.ArgumentOutOfRange(nameof(stringHeapStartOffset));
            }

            if (blobHeapStartOffset < 0)
            {
                Throw.ArgumentOutOfRange(nameof(blobHeapStartOffset));
            }

            if (guidHeapStartOffset < 0)
            {
                Throw.ArgumentOutOfRange(nameof(guidHeapStartOffset));
            }

            if (guidHeapStartOffset % BlobUtilities.SizeOfGuid != 0)
            {
                throw new ArgumentException(SR.Format(SR.ValueMustBeMultiple, BlobUtilities.SizeOfGuid), nameof(guidHeapStartOffset));
            }

            // Add zero-th entry to all heaps, even in EnC delta.
            // We don't want generation-relative handles to ever be IsNil.
            // In both full and delta metadata all nil heap handles should have zero value.
            // There should be no blob handle that references the 0 byte added at the
            // beginning of the delta blob.
            _userStringBuilder.WriteByte(0);

            _blobs.Add(ImmutableArray<byte>.Empty, default(BlobHandle));
            _blobHeapSize = 1;

            // When EnC delta is applied #US, #String and #Blob heaps are appended.
            // Thus indices of strings and blobs added to this generation are offset
            // by the sum of respective heap sizes of all previous generations.
            _userStringHeapStartOffset = userStringHeapStartOffset;
            _stringHeapStartOffset = stringHeapStartOffset;
            _blobHeapStartOffset = blobHeapStartOffset;

            // Unlike other heaps, #Guid heap in EnC delta is zero-padded.
            _guidBuilder.WriteBytes(0, guidHeapStartOffset);
        }

        /// <summary>
        /// Sets the capacity of the specified table.
        /// </summary>
        /// <param name="heap">Heap index.</param>
        /// <param name="byteCount">Number of bytes.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="heap"/> is not a valid heap index.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="byteCount"/> is negative.</exception>
        /// <remarks>
        /// Use to reduce allocations if the approximate number of bytes is known ahead of time.
        /// </remarks>
        public void SetCapacity(HeapIndex heap, int byteCount)
        {
            if (byteCount < 0)
            {
                Throw.ArgumentOutOfRange(nameof(byteCount));
            }

            switch (heap)
            {
                case HeapIndex.Blob:
                    // Not useful to set capacity.
                    // By the time the blob heap is serialized we know the exact size we need.
                    break;

                case HeapIndex.Guid:
                    _guidBuilder.SetCapacity(byteCount);
                    break;

                case HeapIndex.String:
                    _stringHeapCapacity = byteCount;
                    break;

                case HeapIndex.UserString:
                    _userStringBuilder.SetCapacity(byteCount);
                    break;

                default:
                    Throw.ArgumentOutOfRange(nameof(heap));
                    break;
            }
        }

        // internal for testing
        internal static int SerializeHandle(ImmutableArray<int> map, StringHandle handle) => map[handle.GetWriterVirtualIndex()];
        internal static int SerializeHandle(BlobHandle handle) => handle.GetHeapOffset();
        internal static int SerializeHandle(GuidHandle handle) => handle.Index;
        internal static int SerializeHandle(UserStringHandle handle) => handle.GetHeapOffset();

        /// <summary>
        /// Adds specified blob to Blob heap, if it's not there already.
        /// </summary>
        /// <param name="value"><see cref="BlobBuilder"/> containing the blob.</param>
        /// <returns>Handle to the added or existing blob.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
        public BlobHandle GetOrAddBlob(BlobBuilder value!!)
        {
            // TODO: avoid making a copy if the blob exists in the index
            return GetOrAddBlob(value.ToImmutableArray());
        }

        /// <summary>
        /// Adds specified blob to Blob heap, if it's not there already.
        /// </summary>
        /// <param name="value">Array containing the blob.</param>
        /// <returns>Handle to the added or existing blob.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
        public BlobHandle GetOrAddBlob(byte[] value!!)
        {
            // TODO: avoid making a copy if the blob exists in the index
            return GetOrAddBlob(ImmutableArray.Create(value));
        }

        /// <summary>
        /// Adds specified blob to Blob heap, if it's not there already.
        /// </summary>
        /// <param name="value">Array containing the blob.</param>
        /// <returns>Handle to the added or existing blob.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
        public BlobHandle GetOrAddBlob(ImmutableArray<byte> value)
        {
            if (value.IsDefault)
            {
                Throw.ArgumentNull(nameof(value));
            }

            BlobHandle handle;
            if (!_blobs.TryGetValue(value, out handle))
            {
                handle = BlobHandle.FromOffset(_blobHeapStartOffset + _blobHeapSize);
                _blobs.Add(value, handle);

                _blobHeapSize += BlobWriterImpl.GetCompressedIntegerSize(value.Length) + value.Length;
            }

            return handle;
        }

        /// <summary>
        /// Encodes a constant value to a blob and adds it to the Blob heap, if it's not there already.
        /// Uses UTF16 to encode string constants.
        /// </summary>
        /// <param name="value">Constant value.</param>
        /// <returns>Handle to the added or existing blob.</returns>
        public unsafe BlobHandle GetOrAddConstantBlob(object? value)
        {
            if (value is string str)
            {
                return GetOrAddBlobUTF16(str);
            }

            var builder = PooledBlobBuilder.GetInstance();
            builder.WriteConstant(value);
            var result = GetOrAddBlob(builder);
            builder.Free();
            return result;
        }

        /// <summary>
        /// Encodes a string using UTF16 encoding to a blob and adds it to the Blob heap, if it's not there already.
        /// </summary>
        /// <param name="value">String.</param>
        /// <returns>Handle to the added or existing blob.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
        public BlobHandle GetOrAddBlobUTF16(string value)
        {
            var builder = PooledBlobBuilder.GetInstance();
            builder.WriteUTF16(value);
            var handle = GetOrAddBlob(builder);
            builder.Free();
            return handle;
        }

        /// <summary>
        /// Encodes a string using UTF8 encoding to a blob and adds it to the Blob heap, if it's not there already.
        /// </summary>
        /// <param name="value">Constant value.</param>
        /// <param name="allowUnpairedSurrogates">
        /// True to encode unpaired surrogates as specified, otherwise replace them with U+FFFD character.
        /// </param>
        /// <returns>Handle to the added or existing blob.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
        public BlobHandle GetOrAddBlobUTF8(string value, bool allowUnpairedSurrogates = true)
        {
            var builder = PooledBlobBuilder.GetInstance();
            builder.WriteUTF8(value, allowUnpairedSurrogates);
            var handle = GetOrAddBlob(builder);
            builder.Free();
            return handle;
        }

        /// <summary>
        /// Encodes a debug document name and adds it to the Blob heap, if it's not there already.
        /// </summary>
        /// <param name="value">Document name.</param>
        /// <returns>
        /// Handle to the added or existing document name blob
        /// (see https://github.com/dotnet/corefx/blob/master/src/System.Reflection.Metadata/specs/PortablePdb-Metadata.md#DocumentNameBlob).
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
        public BlobHandle GetOrAddDocumentName(string value!!)
        {
            char separator = ChooseSeparator(value);

            var resultBuilder = PooledBlobBuilder.GetInstance();
            resultBuilder.WriteByte((byte)separator);

            var partBuilder = PooledBlobBuilder.GetInstance();

            int i = 0;
            while (true)
            {
                int next = value.IndexOf(separator, i);

                partBuilder.WriteUTF8(value, i, (next >= 0 ? next : value.Length) - i, allowUnpairedSurrogates: true, prependSize: false);
                resultBuilder.WriteCompressedInteger(GetOrAddBlob(partBuilder).GetHeapOffset());

                if (next == -1)
                {
                    break;
                }

                if (next == value.Length - 1)
                {
                    // trailing separator:
                    resultBuilder.WriteByte(0);
                    break;
                }

                partBuilder.Clear();
                i = next + 1;
            }

            partBuilder.Free();

            var resultHandle = GetOrAddBlob(resultBuilder);
            resultBuilder.Free();
            return resultHandle;
        }

        private static char ChooseSeparator(string str)
        {
            const char s1 = '/';
            const char s2 = '\\';

            int count1 = 0, count2 = 0;
            foreach (var c in str)
            {
                if (c == s1)
                {
                    count1++;
                }
                else if (c == s2)
                {
                    count2++;
                }
            }

            return (count1 >= count2) ? s1 : s2;
        }

        /// <summary>
        /// Adds specified Guid to Guid heap, if it's not there already.
        /// </summary>
        /// <param name="guid">Guid to add.</param>
        /// <returns>Handle to the added or existing Guid.</returns>
        public GuidHandle GetOrAddGuid(Guid guid)
        {
            if (guid == Guid.Empty)
            {
                return default(GuidHandle);
            }

            GuidHandle result;
            if (_guids.TryGetValue(guid, out result))
            {
                return result;
            }

            result = GetNewGuidHandle();
            _guids.Add(guid, result);
            _guidBuilder.WriteGuid(guid);
            return result;
        }

        /// <summary>
        /// Reserves space on the Guid heap for a GUID.
        /// </summary>
        /// <returns>
        /// Handle to the reserved Guid and a <see cref="Blob"/> representing the GUID blob as stored on the heap.
        /// </returns>
        /// <exception cref="ImageFormatLimitationException">The remaining space on the heap is too small to fit the string.</exception>
        public ReservedBlob<GuidHandle> ReserveGuid()
        {
            var handle = GetNewGuidHandle();
            var content = _guidBuilder.ReserveBytes(BlobUtilities.SizeOfGuid);
            return new ReservedBlob<GuidHandle>(handle, content);
        }

        private GuidHandle GetNewGuidHandle()
        {
            // Unlike #Blob, #String and #US streams delta #GUID stream is padded to the
            // size of the previous generation #GUID stream before new GUIDs are added.
            // The first GUID added in a delta will thus have an index that equals the number
            // of GUIDs in all previous generations + 1.

            // Metadata Spec:
            // The Guid heap is an array of GUIDs, each 16 bytes wide.
            // Its first element is numbered 1, its second 2, and so on.
            return GuidHandle.FromIndex((_guidBuilder.Count >> 4) + 1);
        }

        /// <summary>
        /// Adds specified string to String heap, if it's not there already.
        /// </summary>
        /// <param name="value">Array containing the blob.</param>
        /// <returns>Handle to the added or existing blob.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
        public StringHandle GetOrAddString(string value!!)
        {
            StringHandle handle;
            if (value.Length == 0)
            {
                handle = default(StringHandle);
            }
            else if (!_strings.TryGetValue(value, out handle))
            {
                handle = StringHandle.FromWriterVirtualIndex(_strings.Count + 1); // idx 0 is reserved for empty string
                _strings.Add(value, handle);
            }

            return handle;
        }

        /// <summary>
        /// Reserves space on the User String heap for a string of specified length.
        /// </summary>
        /// <param name="length">The number of characters to reserve.</param>
        /// <returns>
        /// Handle to the reserved User String and a <see cref="Blob"/> representing the entire User String blob (including its length and terminal character).
        ///
        /// Handle may be used in <see cref="InstructionEncoder.LoadString(UserStringHandle)"/>.
        /// Use <see cref="BlobWriter.WriteUserString(string)"/> to fill in the blob content.
        /// </returns>
        /// <exception cref="ImageFormatLimitationException">The remaining space on the heap is too small to fit the string.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is negative.</exception>
        public ReservedBlob<UserStringHandle> ReserveUserString(int length)
        {
            if (length < 0)
            {
                Throw.ArgumentOutOfRange(nameof(length));
            }

            var handle = GetNewUserStringHandle();
            int encodedLength = BlobUtilities.GetUserStringByteLength(length);
            var reservedUserString = _userStringBuilder.ReserveBytes(BlobWriterImpl.GetCompressedIntegerSize(encodedLength) + encodedLength);
            return new ReservedBlob<UserStringHandle>(handle, reservedUserString);
        }

        /// <summary>
        /// Adds specified string to User String heap, if it's not there already.
        /// </summary>
        /// <param name="value">String to add.</param>
        /// <returns>
        /// Handle to the added or existing string.
        /// May be used in <see cref="InstructionEncoder.LoadString(UserStringHandle)"/>.
        /// </returns>
        /// <exception cref="ImageFormatLimitationException">The remaining space on the heap is too small to fit the string.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
        public UserStringHandle GetOrAddUserString(string value!!)
        {
            UserStringHandle handle;
            if (!_userStrings.TryGetValue(value, out handle))
            {
                handle = GetNewUserStringHandle();

                _userStrings.Add(value, handle);
                _userStringBuilder.WriteUserString(value);
            }

            return handle;
        }

        private UserStringHandle GetNewUserStringHandle()
        {
            int offset = _userStringHeapStartOffset + _userStringBuilder.Count;

            // Native metadata emitter allows strings to exceed the heap size limit as long
            // as the index is within the limits (see https://github.com/dotnet/roslyn/issues/9852)
            if (offset >= UserStringHeapSizeLimit)
            {
                Throw.HeapSizeLimitExceeded(HeapIndex.UserString);
            }

            return UserStringHandle.FromOffset(offset);
        }

        /// <summary>
        /// Fills in stringIndexMap with data from stringIndex and write to stringWriter.
        /// Releases stringIndex as the stringTable is sealed after this point.
        /// </summary>
        private static ImmutableArray<int> SerializeStringHeap(
            BlobBuilder heapBuilder,
            Dictionary<string, StringHandle> strings,
            int stringHeapStartOffset)
        {
            // Sort by suffix and remove stringIndex
            var sorted = new List<KeyValuePair<string, StringHandle>>(strings);
            sorted.Sort(SuffixSort.Instance);

            // Create VirtIdx to Idx map and add entry for empty string
            int totalCount = sorted.Count + 1;
            var stringVirtualIndexToHeapOffsetMap = ImmutableArray.CreateBuilder<int>(totalCount);
            stringVirtualIndexToHeapOffsetMap.Count = totalCount;

            stringVirtualIndexToHeapOffsetMap[0] = 0;
            heapBuilder.WriteByte(0);

            // Find strings that can be folded
            string prev = string.Empty;
            foreach (KeyValuePair<string, StringHandle> entry in sorted)
            {
                int position = stringHeapStartOffset + heapBuilder.Count;

                // It is important to use ordinal comparison otherwise we'll use the current culture!
                if (prev.EndsWith(entry.Key, StringComparison.Ordinal) && !BlobUtilities.IsLowSurrogateChar(entry.Key[0]))
                {
                    // Map over the tail of prev string. Watch for null-terminator of prev string.
                    stringVirtualIndexToHeapOffsetMap[entry.Value.GetWriterVirtualIndex()] = position - (BlobUtilities.GetUTF8ByteCount(entry.Key) + 1);
                }
                else
                {
                    stringVirtualIndexToHeapOffsetMap[entry.Value.GetWriterVirtualIndex()] = position;
                    heapBuilder.WriteUTF8(entry.Key, allowUnpairedSurrogates: false);
                    heapBuilder.WriteByte(0);
                }

                prev = entry.Key;
            }

            return stringVirtualIndexToHeapOffsetMap.MoveToImmutable();
        }

        /// <summary>
        /// Sorts strings such that a string is followed immediately by all strings
        /// that are a suffix of it.
        /// </summary>
        private sealed class SuffixSort : IComparer<KeyValuePair<string, StringHandle>>
        {
            internal static SuffixSort Instance = new SuffixSort();

            public int Compare(KeyValuePair<string, StringHandle> xPair, KeyValuePair<string, StringHandle> yPair)
            {
                string x = xPair.Key;
                string y = yPair.Key;

                for (int i = x.Length - 1, j = y.Length - 1; i >= 0 & j >= 0; i--, j--)
                {
                    if (x[i] < y[j])
                    {
                        return -1;
                    }

                    if (x[i] > y[j])
                    {
                        return +1;
                    }
                }

                return y.Length.CompareTo(x.Length);
            }
        }

        internal void WriteHeapsTo(BlobBuilder builder, BlobBuilder stringHeap)
        {
            WriteAligned(stringHeap, builder);
            WriteAligned(_userStringBuilder, builder);
            WriteAligned(_guidBuilder, builder);
            WriteAlignedBlobHeap(builder);
        }

        private void WriteAlignedBlobHeap(BlobBuilder builder)
        {
            int alignment = BitArithmetic.Align(_blobHeapSize, 4) - _blobHeapSize;

            var writer = new BlobWriter(builder.ReserveBytes(_blobHeapSize + alignment));

            // Perf consideration: With large heap the following loop may cause a lot of cache misses
            // since the order of entries in _blobs dictionary depends on the hash of the array values,
            // which is not correlated to the heap index. If we observe such issue we should order
            // the entries by heap position before running this loop.

            int startOffset = _blobHeapStartOffset;
            foreach (var entry in _blobs)
            {
                int heapOffset = entry.Value.GetHeapOffset();
                var blob = entry.Key;

                writer.Offset = (heapOffset == 0) ? 0 : heapOffset - startOffset;
                writer.WriteCompressedInteger(blob.Length);
                writer.WriteBytes(blob);
            }

            writer.Offset = _blobHeapSize;
            writer.WriteBytes(0, alignment);
        }

        private static void WriteAligned(BlobBuilder source, BlobBuilder target)
        {
            int length = source.Count;
            target.LinkSuffix(source);
            target.WriteBytes(0, BitArithmetic.Align(length, 4) - length);
        }
    }
}
