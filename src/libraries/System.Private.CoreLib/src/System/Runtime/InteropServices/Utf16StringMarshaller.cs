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

        /// <summary>
        /// Initializes a new instance of the <see cref="Utf16StringMarshaller"/>.
        /// </summary>
        /// <param name="str">The string to marshal.</param>
        public Utf16StringMarshaller(string str)
            : this(str, default(Span<ushort>))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Utf16StringMarshaller"/>.
        /// </summary>
        /// <param name="str">The string to marshal.</param>
        /// <param name="buffer">Buffer that may be used for marshalling.</param>
        /// <remarks>
        /// <seealso cref="CustomTypeMarshallerFeatures.CallerAllocatedBuffer"/>
        /// </remarks>
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

        /// <summary>
        /// Returns a reference to the marshalled string.
        /// </summary>
        public ref ushort GetPinnableReference()
        {
            if (allocated != null)
                return ref Unsafe.AsRef<ushort>(allocated);

            return ref span.GetPinnableReference();
        }

        /// <summary>
        /// Returns the native value representing the string.
        /// </summary>
        /// <remarks>
        /// <seealso cref="CustomTypeMarshallerFeatures.TwoStageMarshalling"/>
        /// </remarks>
        public ushort* ToNativeValue() => allocated != null ? allocated : (ushort*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(span));

        /// <summary>
        /// Sets the native value representing the string.
        /// </summary>
        /// <param name="value">The native value.</param>
        /// <remarks>
        /// <seealso cref="CustomTypeMarshallerFeatures.TwoStageMarshalling"/>
        /// </remarks>
        public void FromNativeValue(ushort* value)
        {
            allocated = value;
            isNullString = value == null;
        }

        /// <summary>
        /// Returns the managed string.
        /// </summary>
        /// <remarks>
        /// <seealso cref="CustomTypeMarshallerDirection.Out"/>
        /// </remarks>
        public string? ToManaged()
        {
            if (isNullString)
                return null;

            if (allocated != null)
                return new string((char*)allocated);

            return MemoryMarshal.Cast<ushort, char>(span).ToString();
        }

        /// <summary>
        /// Frees native resources.
        /// </summary>
        /// <remarks>
        /// <seealso cref="CustomTypeMarshallerFeatures.UnmanagedResources"/>
        /// </remarks>
        public void FreeNative()
        {
            if (allocated != null)
                Marshal.FreeCoTaskMem((IntPtr)allocated);
        }
    }
}
