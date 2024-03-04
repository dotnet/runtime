// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// Represents a marshaller for ANSI strings.
    /// </summary>
    [CLSCompliant(false)]
    [CustomMarshaller(typeof(string), MarshalMode.Default, typeof(AnsiStringMarshaller))]
    [CustomMarshaller(typeof(string), MarshalMode.ManagedToUnmanagedIn, typeof(ManagedToUnmanagedIn))]
    public static unsafe class AnsiStringMarshaller
    {
        /// <summary>
        /// Converts a string to an unmanaged version.
        /// </summary>
        /// <param name="managed">The managed string to convert.</param>
        /// <returns>An unmanaged string.</returns>
        public static byte* ConvertToUnmanaged(string? managed)
        {
            if (managed is null)
                return null;

            int exactByteCount = Marshal.GetAnsiStringByteCount(managed); // Includes null terminator
            byte* mem = (byte*)Marshal.AllocCoTaskMem(exactByteCount);
            Span<byte> buffer = new(mem, exactByteCount);

            Marshal.GetAnsiStringBytes(managed, buffer); // Includes null terminator
            return mem;
        }

        /// <summary>
        /// Converts an unmanaged string to a managed version.
        /// </summary>
        /// <param name="unmanaged">The unmanaged string to convert.</param>
        /// <returns>A managed string.</returns>
        public static string? ConvertToManaged(byte* unmanaged)
            => Marshal.PtrToStringAnsi((IntPtr)unmanaged);

        /// <summary>
        /// Frees the memory for the unmanaged string.
        /// </summary>
        /// <param name="unmanaged">The memory allocated for the unmanaged string.</param>
        public static void Free(byte* unmanaged)
            => Marshal.FreeCoTaskMem((IntPtr)unmanaged);

        /// <summary>
        /// Custom marshaller to marshal a managed string as a ANSI unmanaged string.
        /// </summary>
        public ref struct ManagedToUnmanagedIn
        {
            /// <summary>
            /// Gets the requested buffer size for optimized marshalling.
            /// </summary>
            public static int BufferSize => 0x100;

            private byte* _unmanagedValue;
            private bool _allocated;

            /// <summary>
            /// Initializes the marshaller with a managed string and requested buffer.
            /// </summary>
            /// <param name="managed">The managed string to initialize the marshaller with.</param>
            /// <param name="buffer">A request buffer of at least size <see cref="BufferSize"/>.</param>
            public void FromManaged(string? managed, Span<byte> buffer)
            {
                _allocated = false;

                if (managed is null)
                {
                    _unmanagedValue = null;
                    return;
                }

                // >= for null terminator
                // Use the cast to long to avoid the checked operation
                if ((long)Marshal.SystemMaxDBCSCharSize * managed.Length >= buffer.Length)
                {
                    // Calculate accurate byte count when the provided stack-allocated buffer is not sufficient
                    int exactByteCount = Marshal.GetAnsiStringByteCount(managed); // Includes null terminator
                    if (exactByteCount > buffer.Length)
                    {
                        buffer = new Span<byte>((byte*)NativeMemory.Alloc((nuint)exactByteCount), exactByteCount);
                        _allocated = true;
                    }
                }

                // Unsafe.AsPointer is safe since buffer must be pinned
                _unmanagedValue = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer));

                Marshal.GetAnsiStringBytes(managed, buffer); // Includes null terminator
            }

            /// <summary>
            /// Converts the current managed string to an unmanaged string.
            /// </summary>
            /// <returns>The converted unmanaged string.</returns>
            public byte* ToUnmanaged() => _unmanagedValue;

            /// <summary>
            /// Frees any allocated unmanaged string memory.
            /// </summary>
            public void Free()
            {
                if (_allocated)
                    NativeMemory.Free(_unmanagedValue);
            }
        }
    }
}
