// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Marshaller for ANSI strings
    /// </summary>
    [CLSCompliant(false)]
    [CustomTypeMarshaller(typeof(string), BufferSize = 0x100,
        Features = CustomTypeMarshallerFeatures.UnmanagedResources | CustomTypeMarshallerFeatures.TwoStageMarshalling | CustomTypeMarshallerFeatures.CallerAllocatedBuffer)]
    public unsafe ref struct AnsiStringMarshaller
    {
        private byte* _allocated;
        private readonly Span<byte> _span;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnsiStringMarshaller"/>.
        /// </summary>
        /// <param name="str">The string to marshal.</param>
        public AnsiStringMarshaller(string? str)
            : this(str, default)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AnsiStringMarshaller"/>.
        /// </summary>
        /// <param name="str">The string to marshal.</param>
        /// <param name="buffer">Buffer that may be used for marshalling.</param>
        /// <remarks>
        /// The <paramref name="buffer"/> must not be movable - that is, it should not be
        /// on the managed heap or it should be pinned.
        /// <seealso cref="CustomTypeMarshallerFeatures.CallerAllocatedBuffer"/>
        /// </remarks>
        public AnsiStringMarshaller(string? str, Span<byte> buffer)
        {
            _allocated = null;
            if (str is null)
            {
                _span = default;
                return;
            }

            // + 1 for null terminator
            int maxByteCount = (str.Length + 1) * Marshal.SystemMaxDBCSCharSize + 1;
            if (buffer.Length >= maxByteCount)
            {
                Marshal.StringToAnsiString(str, (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer)), buffer.Length);
                _span = buffer;
            }
            else
            {
                _allocated = (byte*)Marshal.StringToCoTaskMemAnsi(str);
                _span = default;
            }
        }

        /// <summary>
        /// Returns the native value representing the string.
        /// </summary>
        /// <remarks>
        /// <seealso cref="CustomTypeMarshallerFeatures.TwoStageMarshalling"/>
        /// </remarks>
        public byte* ToNativeValue() => _allocated != null ? _allocated : (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(_span));

        /// <summary>
        /// Sets the native value representing the string.
        /// </summary>
        /// <param name="value">The native value.</param>
        /// <remarks>
        /// <seealso cref="CustomTypeMarshallerFeatures.TwoStageMarshalling"/>
        /// </remarks>
        public void FromNativeValue(byte* value) => _allocated = value;

        /// <summary>
        /// Returns the managed string.
        /// </summary>
        /// <remarks>
        /// <seealso cref="CustomTypeMarshallerDirection.Out"/>
        /// </remarks>
        public string? ToManaged() => _allocated == null ? null : new string((sbyte*)_allocated);

        /// <summary>
        /// Frees native resources.
        /// </summary>
        /// <remarks>
        /// <seealso cref="CustomTypeMarshallerFeatures.UnmanagedResources"/>
        /// </remarks>
        public void FreeNative()
        {
            if (_allocated != null)
                Marshal.FreeCoTaskMem((IntPtr)_allocated);
        }
    }
}
