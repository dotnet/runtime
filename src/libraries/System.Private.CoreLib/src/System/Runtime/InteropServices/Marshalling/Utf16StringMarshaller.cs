// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// Marshaller for UTF-16 strings
    /// </summary>
    [CLSCompliant(false)]
    [CustomTypeMarshaller(typeof(string), BufferSize = 0x100,
        Features = CustomTypeMarshallerFeatures.UnmanagedResources | CustomTypeMarshallerFeatures.TwoStageMarshalling | CustomTypeMarshallerFeatures.CallerAllocatedBuffer)]
    public unsafe ref struct Utf16StringMarshaller
    {
        private ushort* _nativeValue;
        private bool _allocated;

        /// <summary>
        /// Initializes a new instance of the <see cref="Utf16StringMarshaller"/>.
        /// </summary>
        /// <param name="str">The string to marshal.</param>
        public Utf16StringMarshaller(string? str)
            : this(str, default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Utf16StringMarshaller"/>.
        /// </summary>
        /// <param name="str">The string to marshal.</param>
        /// <param name="buffer">Buffer that may be used for marshalling.</param>
        /// <remarks>
        /// The <paramref name="buffer"/> must not be movable - that is, it should not be
        /// on the managed heap or it should be pinned.
        /// <seealso cref="CustomTypeMarshallerFeatures.CallerAllocatedBuffer"/>
        /// </remarks>
        public Utf16StringMarshaller(string? str, Span<ushort> buffer)
        {
            _allocated = false;

            if (str is null)
            {
                _nativeValue = null;
                return;
            }

            // + 1 for null terminator
            int required = str.Length + 1;
            if (required > buffer.Length)
            {
                buffer = new Span<ushort>((ushort*)Marshal.AllocCoTaskMem(required * sizeof(ushort)), required);
                _allocated = true;
            }

            _nativeValue = (ushort*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer));

            str.CopyTo(MemoryMarshal.Cast<ushort, char>(buffer));
            buffer[str.Length] = '\0'; // null-terminate
        }

        /// <summary>
        /// Returns the native value representing the string.
        /// </summary>
        /// <remarks>
        /// <seealso cref="CustomTypeMarshallerFeatures.TwoStageMarshalling"/>
        /// </remarks>
        public ushort* ToNativeValue() => _nativeValue;

        /// <summary>
        /// Sets the native value representing the string.
        /// </summary>
        /// <param name="value">The native value.</param>
        /// <remarks>
        /// <seealso cref="CustomTypeMarshallerFeatures.TwoStageMarshalling"/>
        /// </remarks>
        public void FromNativeValue(ushort* value)
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
        public string? ToManaged() => Marshal.PtrToStringUni((IntPtr)_nativeValue);

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
