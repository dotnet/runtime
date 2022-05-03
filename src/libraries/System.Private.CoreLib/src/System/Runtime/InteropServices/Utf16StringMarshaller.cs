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
        private ushort* _allocated;
        private readonly Span<ushort> _span;

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
            _allocated = null;
            if (str is null)
            {
                _span = default;
                return;
            }

            // + 1 for null terminator
            if (buffer.Length >= str.Length + 1)
            {
                _span = buffer;
                str.CopyTo(MemoryMarshal.Cast<ushort, char>(buffer));
                _span[str.Length] = '\0'; // null-terminate
            }
            else
            {
                _allocated = (ushort*)Marshal.StringToCoTaskMemUni(str);
                _span = default;
            }
        }

        /// <summary>
        /// Returns a reference to the marshalled string.
        /// </summary>
        public ref ushort GetPinnableReference()
        {
            if (_allocated != null)
                return ref Unsafe.AsRef<ushort>(_allocated);

            return ref _span.GetPinnableReference();
        }

        /// <summary>
        /// Returns the native value representing the string.
        /// </summary>
        /// <remarks>
        /// <seealso cref="CustomTypeMarshallerFeatures.TwoStageMarshalling"/>
        /// </remarks>
        public ushort* ToNativeValue() => _allocated != null ? _allocated : (ushort*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(_span));

        /// <summary>
        /// Sets the native value representing the string.
        /// </summary>
        /// <param name="value">The native value.</param>
        /// <remarks>
        /// <seealso cref="CustomTypeMarshallerFeatures.TwoStageMarshalling"/>
        /// </remarks>
        public void FromNativeValue(ushort* value) => _allocated = value;

        /// <summary>
        /// Returns the managed string.
        /// </summary>
        /// <remarks>
        /// <seealso cref="CustomTypeMarshallerDirection.Out"/>
        /// </remarks>
        public string? ToManaged() => _allocated == null ? null : new string((char*)_allocated);

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
