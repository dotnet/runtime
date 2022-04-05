// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Marshaller for UTF-16 strings
    /// </summary>
    [CLSCompliant(false)]
    [CustomTypeMarshaller(typeof(string), BufferSize = 0x100,
        Features = CustomTypeMarshallerFeatures.UnmanagedResources | CustomTypeMarshallerFeatures.TwoStageMarshalling | CustomTypeMarshallerFeatures.CallerAllocatedBuffer)]
    public unsafe ref struct Utf16StringMarshaller
    {
        private ushort* allocated;
        private Span<ushort> span;
        private bool isNullString;

        public Utf16StringMarshaller(string str)
            : this(str, default(Span<ushort>))
        {
        }

        public Utf16StringMarshaller(string str, Span<ushort> buffer)
        {
            isNullString = false;
            span = default;
            if (str is null)
            {
                allocated = null;
                isNullString = true;
            }
            else if ((str.Length + 1) < buffer.Length)
            {
                span = buffer;
                str.AsSpan().CopyTo(MemoryMarshal.Cast<ushort, char>(buffer));
                // Supplied memory is in an undefined state so ensure
                // there is a trailing null in the buffer.
                span[str.Length] = '\0';
                allocated = null;
            }
            else
            {
                allocated = (ushort*)Marshal.StringToCoTaskMemUni(str);
            }
        }

        public ref ushort GetPinnableReference()
        {
            if (allocated != null)
                return ref Unsafe.AsRef<ushort>(allocated);

            return ref span.GetPinnableReference();
        }

        public ushort* ToNativeValue() => allocated != null ? allocated : (ushort*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(span));

        public void FromNativeValue(ushort* value)
        {
            allocated = value;
            isNullString = value == null;
        }

        public string? ToManaged()
        {
            if (isNullString)
                return null;

            if (allocated != null)
                return new string((char*)allocated);

            return MemoryMarshal.Cast<ushort, char>(span).ToString();
        }

        public void FreeNative()
        {
            if (allocated != null)
                Marshal.FreeCoTaskMem((IntPtr)allocated);
        }
    }
}
