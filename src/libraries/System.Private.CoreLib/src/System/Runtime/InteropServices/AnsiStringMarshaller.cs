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
        private byte* allocated;
        private Span<byte> span;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnsiStringMarshaller"/>.
        /// </summary>
        /// <param name="str">The string to marshal.</param>
        public AnsiStringMarshaller(string str)
            : this(str, default(Span<byte>))
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AnsiStringMarshaller"/>.
        /// </summary>
        /// <param name="str">The string to marshal.</param>
        /// <param name="buffer">Buffer that may be used for marshalling.</param>
        /// <remarks>
        /// <seealso cref="CustomTypeMarshallerFeatures.CallerAllocatedBuffer"/>
        /// </remarks>
        public AnsiStringMarshaller(string str, Span<byte> buffer)
        {
            allocated = null;
            span = default;
            if (str is null)
                return;

            // +1 for null terminator
            // + 1 for the null character from the user.  + 1 for the null character we put in.
            int maxLength = (str.Length + 1) * Marshal.SystemMaxDBCSCharSize + 1;
            if (buffer.Length >= maxLength)
            {
                int length = Marshal.StringToAnsiString(str, (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer)), buffer.Length);
                span = buffer.Slice(0, length);
            }
            else
            {
                allocated = (byte*)Marshal.StringToCoTaskMemAnsi(str);
            }
        }

        /// <summary>
        /// Returns the native value representing the string.
        /// </summary>
        /// <remarks>
        /// <seealso cref="CustomTypeMarshallerFeatures.TwoStageMarshalling"/>
        /// </remarks>
        public byte* ToNativeValue() => allocated != null ? allocated : (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(span));

        /// <summary>
        /// Sets the native value representing the string.
        /// </summary>
        /// <param name="value">The native value.</param>
        /// <remarks>
        /// <seealso cref="CustomTypeMarshallerFeatures.TwoStageMarshalling"/>
        /// </remarks>
        public void FromNativeValue(byte* value) => allocated = value;

        /// <summary>
        /// Returns the managed string.
        /// </summary>
        /// <remarks>
        /// <seealso cref="CustomTypeMarshallerDirection.Out"/>
        /// </remarks>
        public string? ToManaged() => allocated == null ? null : new string((sbyte*)allocated);

        /// <summary>
        /// Frees native resources.
        /// </summary>
        /// <remarks>
        /// <seealso cref="CustomTypeMarshallerFeatures.UnmanagedResources"/>
        /// </remarks>
        public void FreeNative() => Marshal.FreeCoTaskMem((IntPtr)allocated);
    }
}
