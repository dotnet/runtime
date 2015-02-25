// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335
{
    internal struct StringStreamReader
    {
        private static string[] s_virtualValues;

        internal readonly MemoryBlock Block;

        internal StringStreamReader(MemoryBlock block, MetadataKind metadataKind)
        {
            if (s_virtualValues == null && metadataKind != MetadataKind.Ecma335)
            {
                // Note:
                // Virtual values shall not contain surrogates, otherwise StartsWith might be inconsistent 
                // when comparing to a text that ends with a high surrogate.

                var values = new string[(int)StringHandle.VirtualIndex.Count];
                values[(int)StringHandle.VirtualIndex.System_Runtime_WindowsRuntime] = "System.Runtime.WindowsRuntime";
                values[(int)StringHandle.VirtualIndex.System_Runtime] = "System.Runtime";
                values[(int)StringHandle.VirtualIndex.System_ObjectModel] = "System.ObjectModel";
                values[(int)StringHandle.VirtualIndex.System_Runtime_WindowsRuntime_UI_Xaml] = "System.Runtime.WindowsRuntime.UI.Xaml";
                values[(int)StringHandle.VirtualIndex.System_Runtime_InteropServices_WindowsRuntime] = "System.Runtime.InteropServices.WindowsRuntime";
                values[(int)StringHandle.VirtualIndex.System_Numerics_Vectors] = "System.Numerics.Vectors";

                values[(int)StringHandle.VirtualIndex.Dispose] = "Dispose";

                values[(int)StringHandle.VirtualIndex.AttributeTargets] = "AttributeTargets";
                values[(int)StringHandle.VirtualIndex.AttributeUsageAttribute] = "AttributeUsageAttribute";
                values[(int)StringHandle.VirtualIndex.Color] = "Color";
                values[(int)StringHandle.VirtualIndex.CornerRadius] = "CornerRadius";
                values[(int)StringHandle.VirtualIndex.DateTimeOffset] = "DateTimeOffset";
                values[(int)StringHandle.VirtualIndex.Duration] = "Duration";
                values[(int)StringHandle.VirtualIndex.DurationType] = "DurationType";
                values[(int)StringHandle.VirtualIndex.EventHandler1] = "EventHandler`1";
                values[(int)StringHandle.VirtualIndex.EventRegistrationToken] = "EventRegistrationToken";
                values[(int)StringHandle.VirtualIndex.Exception] = "Exception";
                values[(int)StringHandle.VirtualIndex.GeneratorPosition] = "GeneratorPosition";
                values[(int)StringHandle.VirtualIndex.GridLength] = "GridLength";
                values[(int)StringHandle.VirtualIndex.GridUnitType] = "GridUnitType";
                values[(int)StringHandle.VirtualIndex.ICommand] = "ICommand";
                values[(int)StringHandle.VirtualIndex.IDictionary2] = "IDictionary`2";
                values[(int)StringHandle.VirtualIndex.IDisposable] = "IDisposable";
                values[(int)StringHandle.VirtualIndex.IEnumerable] = "IEnumerable";
                values[(int)StringHandle.VirtualIndex.IEnumerable1] = "IEnumerable`1";
                values[(int)StringHandle.VirtualIndex.IList] = "IList";
                values[(int)StringHandle.VirtualIndex.IList1] = "IList`1";
                values[(int)StringHandle.VirtualIndex.INotifyCollectionChanged] = "INotifyCollectionChanged";
                values[(int)StringHandle.VirtualIndex.INotifyPropertyChanged] = "INotifyPropertyChanged";
                values[(int)StringHandle.VirtualIndex.IReadOnlyDictionary2] = "IReadOnlyDictionary`2";
                values[(int)StringHandle.VirtualIndex.IReadOnlyList1] = "IReadOnlyList`1";
                values[(int)StringHandle.VirtualIndex.KeyTime] = "KeyTime";
                values[(int)StringHandle.VirtualIndex.KeyValuePair2] = "KeyValuePair`2";
                values[(int)StringHandle.VirtualIndex.Matrix] = "Matrix";
                values[(int)StringHandle.VirtualIndex.Matrix3D] = "Matrix3D";
                values[(int)StringHandle.VirtualIndex.Matrix3x2] = "Matrix3x2";
                values[(int)StringHandle.VirtualIndex.Matrix4x4] = "Matrix4x4";
                values[(int)StringHandle.VirtualIndex.NotifyCollectionChangedAction] = "NotifyCollectionChangedAction";
                values[(int)StringHandle.VirtualIndex.NotifyCollectionChangedEventArgs] = "NotifyCollectionChangedEventArgs";
                values[(int)StringHandle.VirtualIndex.NotifyCollectionChangedEventHandler] = "NotifyCollectionChangedEventHandler";
                values[(int)StringHandle.VirtualIndex.Nullable1] = "Nullable`1";
                values[(int)StringHandle.VirtualIndex.Plane] = "Plane";
                values[(int)StringHandle.VirtualIndex.Point] = "Point";
                values[(int)StringHandle.VirtualIndex.PropertyChangedEventArgs] = "PropertyChangedEventArgs";
                values[(int)StringHandle.VirtualIndex.PropertyChangedEventHandler] = "PropertyChangedEventHandler";
                values[(int)StringHandle.VirtualIndex.Quaternion] = "Quaternion";
                values[(int)StringHandle.VirtualIndex.Rect] = "Rect";
                values[(int)StringHandle.VirtualIndex.RepeatBehavior] = "RepeatBehavior";
                values[(int)StringHandle.VirtualIndex.RepeatBehaviorType] = "RepeatBehaviorType";
                values[(int)StringHandle.VirtualIndex.Size] = "Size";
                values[(int)StringHandle.VirtualIndex.System] = "System";
                values[(int)StringHandle.VirtualIndex.System_Collections] = "System.Collections";
                values[(int)StringHandle.VirtualIndex.System_Collections_Generic] = "System.Collections.Generic";
                values[(int)StringHandle.VirtualIndex.System_Collections_Specialized] = "System.Collections.Specialized";
                values[(int)StringHandle.VirtualIndex.System_ComponentModel] = "System.ComponentModel";
                values[(int)StringHandle.VirtualIndex.System_Numerics] = "System.Numerics";
                values[(int)StringHandle.VirtualIndex.System_Windows_Input] = "System.Windows.Input";
                values[(int)StringHandle.VirtualIndex.Thickness] = "Thickness";
                values[(int)StringHandle.VirtualIndex.TimeSpan] = "TimeSpan";
                values[(int)StringHandle.VirtualIndex.Type] = "Type";
                values[(int)StringHandle.VirtualIndex.Uri] = "Uri";
                values[(int)StringHandle.VirtualIndex.Vector2] = "Vector2";
                values[(int)StringHandle.VirtualIndex.Vector3] = "Vector3";
                values[(int)StringHandle.VirtualIndex.Vector4] = "Vector4";
                values[(int)StringHandle.VirtualIndex.Windows_Foundation] = "Windows.Foundation";
                values[(int)StringHandle.VirtualIndex.Windows_UI] = "Windows.UI";
                values[(int)StringHandle.VirtualIndex.Windows_UI_Xaml] = "Windows.UI.Xaml";
                values[(int)StringHandle.VirtualIndex.Windows_UI_Xaml_Controls_Primitives] = "Windows.UI.Xaml.Controls.Primitives";
                values[(int)StringHandle.VirtualIndex.Windows_UI_Xaml_Media] = "Windows.UI.Xaml.Media";
                values[(int)StringHandle.VirtualIndex.Windows_UI_Xaml_Media_Animation] = "Windows.UI.Xaml.Media.Animation";
                values[(int)StringHandle.VirtualIndex.Windows_UI_Xaml_Media_Media3D] = "Windows.UI.Xaml.Media.Media3D";

                s_virtualValues = values;
                AssertFilled();
            }

            this.Block = TrimEnd(block);
        }

        [Conditional("DEBUG")]
        private static void AssertFilled()
        {
            for (int i = 0; i < s_virtualValues.Length; i++)
            {
                Debug.Assert(s_virtualValues[i] != null, "Missing virtual value for StringHandle.VirtualIndex." + (StringHandle.VirtualIndex)i);
            }
        }

        // Trims the alignment padding of the heap.
        // See StgStringPool::InitOnMem in ndp\clr\src\Utilcode\StgPool.cpp.

        // This is especially important for EnC.
        private static MemoryBlock TrimEnd(MemoryBlock block)
        {
            if (block.Length == 0)
            {
                return block;
            }

            int i = block.Length - 1;
            while (i >= 0 && block.PeekByte(i) == 0)
            {
                i--;
            }

            // this shouldn't happen in valid metadata:
            if (i == block.Length - 1)
            {
                return block;
            }

            // +1 for terminating \0
            return block.GetMemoryBlockAt(0, i + 2);
        }


        internal string GetVirtualValue(StringHandle.VirtualIndex index)
        {
            return s_virtualValues[(int)index];
        }

        internal string GetString(StringHandle handle, MetadataStringDecoder utf8Decoder)
        {
            int index = handle.Index;
            byte[] prefix;

            if (handle.IsVirtual)
            {
                switch (handle.StringKind)
                {
                    case StringKind.Plain:
                        return s_virtualValues[index];

                    case StringKind.WinRTPrefixed:
                        prefix = MetadataReader.WinRTPrefix;
                        break;

                    default:
                        Debug.Assert(false, "We should not get here");
                        return null;
                }
            }
            else
            {
                prefix = null;
            }

            int bytesRead;
            char otherTerminator = handle.StringKind == StringKind.DotTerminated ? '.' : '\0';
            return this.Block.PeekUtf8NullTerminated(index, prefix, utf8Decoder, out bytesRead, otherTerminator);
        }

        internal StringHandle GetNextHandle(StringHandle handle)
        {
            if (handle.IsVirtual)
            {
                return default(StringHandle);
            }

            int terminator = this.Block.IndexOf(0, handle.Index);
            if (terminator == -1 || terminator == Block.Length - 1)
            {
                return default(StringHandle);
            }

            return StringHandle.FromIndex((uint)(terminator + 1));
        }

        internal bool Equals(StringHandle handle, string value, MetadataStringDecoder utf8Decoder)
        {
            Debug.Assert(value != null);

            if (handle.IsVirtual)
            {
                // TODO:This can allocate unnecessarily for <WinRT> prefixed handles.
                return GetString(handle, utf8Decoder) == value;
            }

            if (handle.IsNil)
            {
                return value.Length == 0;
            }

            char otherTerminator = handle.StringKind == StringKind.DotTerminated ? '.' : '\0';
            return this.Block.Utf8NullTerminatedEquals(handle.Index, value, utf8Decoder, otherTerminator);
        }

        internal bool StartsWith(StringHandle handle, string value, MetadataStringDecoder utf8Decoder)
        {
            Debug.Assert(value != null);

            if (handle.IsVirtual)
            {
                // TODO:This can allocate unnecessarily for <WinRT> prefixed handles. 
                return GetString(handle, utf8Decoder).StartsWith(value, StringComparison.Ordinal);
            }

            if (handle.IsNil)
            {
                return value.Length == 0;
            }

            char otherTerminator = handle.StringKind == StringKind.DotTerminated ? '.' : '\0';
            return this.Block.Utf8NullTerminatedStartsWith(handle.Index, value, utf8Decoder, otherTerminator);
        }

        /// <summary>
        /// Returns true if the given raw (non-virtual) handle represents the same string as given ASCII string.
        /// </summary>
        internal bool EqualsRaw(StringHandle rawHandle, string asciiString)
        {
            Debug.Assert(!rawHandle.IsVirtual);
            Debug.Assert(rawHandle.StringKind != StringKind.DotTerminated, "Not supported");
            return this.Block.CompareUtf8NullTerminatedStringWithAsciiString(rawHandle.Index, asciiString) == 0;
        }

        /// <summary>
        /// Returns the heap index of the given ASCII character or -1 if not found prior null terminator or end of heap.
        /// </summary>
        internal int IndexOfRaw(int startIndex, char asciiChar)
        {
            Debug.Assert(asciiChar != 0 && asciiChar <= 0x7f);
            return this.Block.Utf8NullTerminatedOffsetOfAsciiChar(startIndex, asciiChar);
        }

        /// <summary>
        /// Returns true if the given raw (non-virtual) handle represents a string that starts with given ASCII prefix.
        /// </summary>
        internal bool StartsWithRaw(StringHandle rawHandle, string asciiPrefix)
        {
            Debug.Assert(!rawHandle.IsVirtual);
            Debug.Assert(rawHandle.StringKind != StringKind.DotTerminated, "Not supported");
            return this.Block.Utf8NullTerminatedStringStartsWithAsciiPrefix(rawHandle.Index, asciiPrefix);
        }

        /// <summary>
        /// Equivalent to Array.BinarySearch, searches for given raw (non-virtual) handle in given array of ASCII strings.
        /// </summary>
        internal int BinarySearchRaw(string[] asciiKeys, StringHandle rawHandle)
        {
            Debug.Assert(!rawHandle.IsVirtual);
            Debug.Assert(rawHandle.StringKind != StringKind.DotTerminated, "Not supported");
            return this.Block.BinarySearch(asciiKeys, rawHandle.Index);
        }
    }

    internal unsafe struct BlobStreamReader
    {
        private struct VirtualHeapBlob
        {
            public readonly GCHandle Pinned;
            public readonly byte[] Array;

            public VirtualHeapBlob(byte[] array)
            {
                Pinned = GCHandle.Alloc(array, GCHandleType.Pinned);
                Array = array;
            }
        }

        // Container for virtual heap blobs that unpins handles on finalization.
        // This is not handled via dispose because the only resource is managed memory.
        private sealed class VirtualHeapBlobTable
        {
            public readonly Dictionary<BlobHandle, VirtualHeapBlob> Table;

            public VirtualHeapBlobTable()
            {
                Table = new Dictionary<BlobHandle, VirtualHeapBlob>();
            }

            ~VirtualHeapBlobTable()
            {
                if (Table != null)
                {
                    foreach (var blob in Table.Values)
                    {
                        blob.Pinned.Free();
                    }
                }
            }
        }

        // Since the number of virtual blobs we need is small (the number of attribute classes in .winmd files)
        // we can create a pinned handle for each of them.
        // If we needed many more blobs we could create and pin a single byte[] and allocate blobs there.
        private VirtualHeapBlobTable _lazyVirtualHeapBlobs;
        private static byte[][] s_virtualHeapBlobs;

        internal readonly MemoryBlock Block;

        internal BlobStreamReader(MemoryBlock block, MetadataKind metadataKind)
        {
            _lazyVirtualHeapBlobs = null;
            this.Block = block;

            if (s_virtualHeapBlobs == null && metadataKind != MetadataKind.Ecma335)
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

                s_virtualHeapBlobs = blobs;
            }
        }

        internal byte[] GetBytes(BlobHandle handle)
        {
            if (handle.IsVirtual)
            {
                // consider: if we returned an ImmutableArray we wouldn't need to copy
                return GetVirtualBlobArray(handle, unique: true);
            }

            int offset = handle.Index;
            int bytesRead;
            int numberOfBytes = this.Block.PeekCompressedInteger(offset, out bytesRead);
            if (numberOfBytes == BlobReader.InvalidCompressedInteger)
            {
                return EmptyArray<byte>.Instance;
            }

            return this.Block.PeekBytes(offset + bytesRead, numberOfBytes);
        }

        internal BlobReader GetBlobReader(BlobHandle handle)
        {
            if (handle.IsVirtual)
            {
                if (_lazyVirtualHeapBlobs == null)
                {
                    Interlocked.CompareExchange(ref _lazyVirtualHeapBlobs, new VirtualHeapBlobTable(), null);
                }

                int index = (int)handle.GetVirtualIndex();
                int length = s_virtualHeapBlobs[index].Length;

                VirtualHeapBlob virtualBlob;
                lock (_lazyVirtualHeapBlobs)
                {
                    if (!_lazyVirtualHeapBlobs.Table.TryGetValue(handle, out virtualBlob))
                    {
                        virtualBlob = new VirtualHeapBlob(GetVirtualBlobArray(handle, unique: false));
                        _lazyVirtualHeapBlobs.Table.Add(handle, virtualBlob);
                    }
                }

                return new BlobReader(new MemoryBlock((byte*)virtualBlob.Pinned.AddrOfPinnedObject(), length));
            }

            int offset, size;
            Block.PeekHeapValueOffsetAndSize(handle.Index, out offset, out size);
            return new BlobReader(this.Block.GetMemoryBlockAt(offset, size));
        }

        internal BlobHandle GetNextHandle(BlobHandle handle)
        {
            if (handle.IsVirtual)
            {
                return default(BlobHandle);
            }

            int offset, size;
            if (!Block.PeekHeapValueOffsetAndSize(handle.Index, out offset, out size))
            {
                return default(BlobHandle);
            }

            int nextIndex = offset + size;
            if (nextIndex >= Block.Length)
            {
                return default(BlobHandle);
            }

            return BlobHandle.FromIndex((uint)nextIndex);
        }

        internal byte[] GetVirtualBlobArray(BlobHandle handle, bool unique)
        {
            BlobHandle.VirtualIndex index = handle.GetVirtualIndex();
            byte[] result = s_virtualHeapBlobs[(int)index];

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
    }

    internal struct GuidStreamReader
    {
        internal readonly MemoryBlock Block;
        internal const int GuidSize = 16;

        public GuidStreamReader(MemoryBlock block)
        {
            this.Block = block;
        }

        internal Guid GetGuid(GuidHandle handle)
        {
            if (handle.IsNil)
            {
                return default(Guid);
            }

            // Metadata Spec: The Guid heap is an array of GUIDs, each 16 bytes wide. 
            // Its first element is numbered 1, its second 2, and so on.
            return this.Block.PeekGuid((handle.Index - 1) * GuidSize);
        }
    }

    internal struct UserStringStreamReader
    {
        internal readonly MemoryBlock Block;

        public UserStringStreamReader(MemoryBlock block)
        {
            this.Block = block;
        }

        internal string GetString(UserStringHandle handle)
        {
            int offset, size;
            if (!Block.PeekHeapValueOffsetAndSize(handle.Index, out offset, out size))
            {
                return string.Empty;
            }

            // Spec: Furthermore, there is an additional terminal byte (so all byte counts are odd, not even). 
            // The size in the blob header is the length of the string in bytes + 1.
            return this.Block.PeekUtf16(offset, size & ~1);
        }

        internal UserStringHandle GetNextHandle(UserStringHandle handle)
        {
            int offset, size;
            if (!Block.PeekHeapValueOffsetAndSize(handle.Index, out offset, out size))
            {
                return default(UserStringHandle);
            }

            int nextIndex = offset + size;
            if (nextIndex >= Block.Length)
            {
                return default(UserStringHandle);
            }

            return UserStringHandle.FromIndex((uint)nextIndex);
        }
    }
}
