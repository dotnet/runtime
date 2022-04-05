// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Marshaller for UTF-8 strings
    /// </summary>
    [CLSCompliant(false)]
    [CustomTypeMarshaller(typeof(string), BufferSize = 0x100,
        Features = CustomTypeMarshallerFeatures.UnmanagedResources | CustomTypeMarshallerFeatures.TwoStageMarshalling | CustomTypeMarshallerFeatures.CallerAllocatedBuffer)]
    public unsafe ref struct Utf8StringMarshaller
    {
        private byte* allocated;
        private Span<byte> span;

        // Conversion from a 2-byte 'char' in UTF-16 to bytes in UTF-8 has a maximum of 3 bytes per 'char'
        // Two bytes ('char') in UTF-16 can be either:
        //   - Code point in the Basic Multilingual Plane: all 16 bits are that of the code point
        //   - Part of a pair for a code point in the Supplementary Planes: 10 bits are that of the code point
        // In UTF-8, 3 bytes are need to represent the code point in first and 4 bytes in the second. Thus, the
        // maximum number of bytes per 'char' is 3.
        private const int MaxByteCountPerChar = 3;

        public Utf8StringMarshaller(string str)
            : this(str, default(Span<byte>))
        { }

        public Utf8StringMarshaller(string str, Span<byte> buffer)
        {
            allocated = null;
            span = default;
            if (str is null)
                return;

            // + 1 for number of characters in case left over high surrogate is ?
            // * <MaxByteCountPerChar> (3 for UTF-8)
            // +1 for null terminator
            if (buffer.Length >= (str.Length + 1) * MaxByteCountPerChar + 1)
            {
                int byteCount = Encoding.UTF8.GetBytes(str, buffer);
                buffer[byteCount] = 0; // null-terminate
                span = buffer;
            }
            else
            {
                allocated = (byte*)Marshal.StringToCoTaskMemUTF8(str);
            }
        }

        public byte* ToNativeValue() => allocated != null ? allocated : (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(span));

        public void FromNativeValue(byte* value) => allocated = value;

        public string? ToManaged() => allocated == null ? null : Marshal.PtrToStringUTF8((IntPtr)allocated);

        public void FreeNative()
        {
            if (allocated != null)
                Marshal.FreeCoTaskMem((IntPtr)allocated);
        }
    }
}
