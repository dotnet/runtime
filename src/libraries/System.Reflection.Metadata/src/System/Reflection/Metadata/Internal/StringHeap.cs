// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.Internal;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace System.Reflection.Metadata.Ecma335
{
    internal struct StringHeap
    {
        private static string[]? s_virtualValues;

        internal readonly MemoryBlock Block;
        private VirtualHeap? _lazyVirtualHeap;

        internal StringHeap(MemoryBlock block, MetadataKind metadataKind)
        {
            _lazyVirtualHeap = null;

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
            for (int i = 0; i < s_virtualValues!.Length; i++)
            {
                Debug.Assert(s_virtualValues[i] != null, $"Missing virtual value for StringHandle.VirtualIndex.{(StringHandle.VirtualIndex)i}");
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

        internal string GetString(StringHandle handle, MetadataStringDecoder utf8Decoder)
        {
            return handle.IsVirtual ? GetVirtualHandleString(handle, utf8Decoder) : GetNonVirtualString(handle, utf8Decoder, prefixOpt: null);
        }

        internal MemoryBlock GetMemoryBlock(StringHandle handle)
        {
            return handle.IsVirtual ? GetVirtualHandleMemoryBlock(handle) : GetNonVirtualStringMemoryBlock(handle);
        }

        internal static string GetVirtualString(StringHandle.VirtualIndex index)
        {
            return s_virtualValues![(int)index];
        }

        private string GetNonVirtualString(StringHandle handle, MetadataStringDecoder utf8Decoder, byte[]? prefixOpt)
        {
            Debug.Assert(handle.StringKind != StringKind.Virtual);

            int bytesRead;
            char otherTerminator = handle.StringKind == StringKind.DotTerminated ? '.' : '\0';
            return Block.PeekUtf8NullTerminated(handle.GetHeapOffset(), prefixOpt, utf8Decoder, out bytesRead, otherTerminator);
        }

        private unsafe MemoryBlock GetNonVirtualStringMemoryBlock(StringHandle handle)
        {
            Debug.Assert(handle.StringKind != StringKind.Virtual);

            int bytesRead;
            char otherTerminator = handle.StringKind == StringKind.DotTerminated ? '.' : '\0';
            int offset = handle.GetHeapOffset();
            int length = Block.GetUtf8NullTerminatedLength(offset, out bytesRead, otherTerminator);

            return new MemoryBlock(Block.Pointer + offset, length);
        }

        private unsafe byte[] GetNonVirtualStringBytes(StringHandle handle, byte[] prefix)
        {
            Debug.Assert(handle.StringKind != StringKind.Virtual);

            var block = GetNonVirtualStringMemoryBlock(handle);
            var bytes = new byte[prefix.Length + block.Length];
            Buffer.BlockCopy(prefix, 0, bytes, 0, prefix.Length);
            Marshal.Copy((IntPtr)block.Pointer, bytes, prefix.Length, block.Length);
            return bytes;
        }

        private string GetVirtualHandleString(StringHandle handle, MetadataStringDecoder utf8Decoder)
        {
            Debug.Assert(handle.IsVirtual);

            return handle.StringKind switch
            {
                StringKind.Virtual => GetVirtualString(handle.GetVirtualIndex()),
                StringKind.WinRTPrefixed => GetNonVirtualString(handle, utf8Decoder, MetadataReader.WinRTPrefix),
                _ => throw ExceptionUtilities.UnexpectedValue(handle.StringKind),
            };
        }

        private MemoryBlock GetVirtualHandleMemoryBlock(StringHandle handle)
        {
            Debug.Assert(handle.IsVirtual);
            var heap = VirtualHeap.GetOrCreateVirtualHeap(ref _lazyVirtualHeap);

            lock (heap)
            {
                if (!heap.TryGetMemoryBlock(handle.RawValue, out var block))
                {
                    byte[] bytes = handle.StringKind switch
                    {
                        StringKind.Virtual => Encoding.UTF8.GetBytes(GetVirtualString(handle.GetVirtualIndex())),
                        StringKind.WinRTPrefixed => GetNonVirtualStringBytes(handle, MetadataReader.WinRTPrefix),
                        _ => throw ExceptionUtilities.UnexpectedValue(handle.StringKind),
                    };
                    block = heap.AddBlob(handle.RawValue, bytes);
                }

                return block;
            }
        }

        internal BlobReader GetBlobReader(StringHandle handle)
        {
            return new BlobReader(GetMemoryBlock(handle));
        }

        internal StringHandle GetNextHandle(StringHandle handle)
        {
            if (handle.IsVirtual)
            {
                return default(StringHandle);
            }

            int terminator = this.Block.IndexOf(0, handle.GetHeapOffset());
            if (terminator == -1 || terminator == Block.Length - 1)
            {
                return default(StringHandle);
            }

            return StringHandle.FromOffset(terminator + 1);
        }

        internal bool Equals(StringHandle handle, string value, MetadataStringDecoder utf8Decoder, bool ignoreCase)
        {
            Debug.Assert(value != null);

            if (handle.IsVirtual)
            {
                // TODO: This can allocate unnecessarily for <WinRT> prefixed handles.
                return string.Equals(GetString(handle, utf8Decoder), value, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
            }

            if (handle.IsNil)
            {
                return value.Length == 0;
            }

            char otherTerminator = handle.StringKind == StringKind.DotTerminated ? '.' : '\0';
            return this.Block.Utf8NullTerminatedEquals(handle.GetHeapOffset(), value, utf8Decoder, otherTerminator, ignoreCase);
        }

        internal bool StartsWith(StringHandle handle, string value, MetadataStringDecoder utf8Decoder, bool ignoreCase)
        {
            Debug.Assert(value != null);

            if (handle.IsVirtual)
            {
                // TODO: This can allocate unnecessarily for <WinRT> prefixed handles.
                return GetString(handle, utf8Decoder).StartsWith(value, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
            }

            if (handle.IsNil)
            {
                return value.Length == 0;
            }

            char otherTerminator = handle.StringKind == StringKind.DotTerminated ? '.' : '\0';
            return this.Block.Utf8NullTerminatedStartsWith(handle.GetHeapOffset(), value, utf8Decoder, otherTerminator, ignoreCase);
        }

        /// <summary>
        /// Returns true if the given raw (non-virtual) handle represents the same string as given ASCII string.
        /// </summary>
        internal bool EqualsRaw(StringHandle rawHandle, string asciiString)
        {
            Debug.Assert(!rawHandle.IsVirtual);
            Debug.Assert(rawHandle.StringKind != StringKind.DotTerminated, "Not supported");
            return this.Block.CompareUtf8NullTerminatedStringWithAsciiString(rawHandle.GetHeapOffset(), asciiString) == 0;
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
            return this.Block.Utf8NullTerminatedStringStartsWithAsciiPrefix(rawHandle.GetHeapOffset(), asciiPrefix);
        }

        /// <summary>
        /// Equivalent to Array.BinarySearch, searches for given raw (non-virtual) handle in given array of ASCII strings.
        /// </summary>
        internal int BinarySearchRaw(string[] asciiKeys, StringHandle rawHandle)
        {
            Debug.Assert(!rawHandle.IsVirtual);
            Debug.Assert(rawHandle.StringKind != StringKind.DotTerminated, "Not supported");
            return this.Block.BinarySearch(asciiKeys, rawHandle.GetHeapOffset());
        }
    }
}
