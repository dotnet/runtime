// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text;

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// Marshaller for UTF-8 strings
    /// </summary>
    [CLSCompliant(false)]
    [CustomTypeMarshaller(typeof(string), BufferSize = 0x100,
        Features = CustomTypeMarshallerFeatures.UnmanagedResources | CustomTypeMarshallerFeatures.TwoStageMarshalling | CustomTypeMarshallerFeatures.CallerAllocatedBuffer)]
    public unsafe ref struct Utf8StringMarshaller
    {
        private byte* _nativeValue;
        private bool _allocated;

        /// <summary>
        /// Initializes a new instance of the <see cref="Utf8StringMarshaller"/>.
        /// </summary>
        /// <param name="str">The string to marshal.</param>
        public Utf8StringMarshaller(string? str)
            : this(str, default)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Utf8StringMarshaller"/>.
        /// </summary>
        /// <param name="str">The string to marshal.</param>
        /// <param name="buffer">Buffer that may be used for marshalling.</param>
        /// <remarks>
        /// The <paramref name="buffer"/> must not be movable - that is, it should not be
        /// on the managed heap or it should be pinned.
        /// <seealso cref="CustomTypeMarshallerFeatures.CallerAllocatedBuffer"/>
        /// </remarks>
        public Utf8StringMarshaller(string? str, Span<byte> buffer)
        {
            _allocated = false;

            if (str is null)
            {
                _nativeValue = null;
                return;
            }

            const int MaxUtf8BytesPerChar = 3;

            // >= for null terminator
            // Use the cast to long to avoid the checked operation
            if ((long)MaxUtf8BytesPerChar * str.Length >= buffer.Length)
            {
                // Calculate accurate byte count when the provided stack-allocated buffer is not sufficient
                int exactByteCount = checked(Encoding.UTF8.GetByteCount(str) + 1); // + 1 for null terminator
                if (exactByteCount > buffer.Length)
                {
                    buffer = new Span<byte>((byte*)Marshal.AllocCoTaskMem(exactByteCount), exactByteCount);
                    _allocated = true;
                }
            }

            _nativeValue = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer));

            int byteCount = Encoding.UTF8.GetBytes(str, buffer);
            buffer[byteCount] = 0; // null-terminate
        }

        /// <summary>
        /// Returns the native value representing the string.
        /// </summary>
        /// <remarks>
        /// <seealso cref="CustomTypeMarshallerFeatures.TwoStageMarshalling"/>
        /// </remarks>
        public byte* ToNativeValue() => _nativeValue;

        /// <summary>
        /// Sets the native value representing the string.
        /// </summary>
        /// <param name="value">The native value.</param>
        /// <remarks>
        /// <seealso cref="CustomTypeMarshallerFeatures.TwoStageMarshalling"/>
        /// </remarks>
        public void FromNativeValue(byte* value)
        {
            _nativeValue = value;
            _allocated = true;
        }

        /// <summary>
        /// Returns the managed string.
        /// </summary>
        /// <remarks>
        /// <seealso cref="CustomTypeMarshallerDirection.Out"/>
        /// </remarks>
        public string? ToManaged() => Marshal.PtrToStringUTF8((IntPtr)_nativeValue);

        /// <summary>
        /// Frees native resources.
        /// </summary>
        /// <remarks>
        /// <seealso cref="CustomTypeMarshallerFeatures.UnmanagedResources"/>
        /// </remarks>
        public void FreeNative()
        {
            if (_allocated)
                Marshal.FreeCoTaskMem((IntPtr)_nativeValue);
        }
    }
}
