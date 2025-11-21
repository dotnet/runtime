// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text;

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// Marshaller for ReadOnlySpan&lt;char&gt; to UTF-8 strings.
    /// </summary>
    [CustomMarshaller(typeof(ReadOnlySpan<char>), MarshalMode.ManagedToUnmanagedIn, typeof(ManagedToUnmanagedIn))]
    internal static unsafe class SpanOfCharAsUtf8StringMarshaller
    {
        /// <summary>
        /// Custom marshaller to marshal a ReadOnlySpan&lt;char&gt; as a UTF-8 unmanaged string.
        /// </summary>
        internal ref struct ManagedToUnmanagedIn
        {
            /// <summary>
            /// Gets the requested buffer size for optimized marshalling.
            /// </summary>
            public static int BufferSize => 0x100;

            private byte* _unmanagedValue;
            private bool _allocated;

            /// <summary>
            /// Initializes the marshaller with a ReadOnlySpan&lt;char&gt; and requested buffer.
            /// </summary>
            /// <param name="managed">The managed ReadOnlySpan&lt;char&gt; with which to initialize the marshaller.</param>
            /// <param name="buffer">The request buffer whose size is at least <see cref="BufferSize"/>.</param>
            public void FromManaged(ReadOnlySpan<char> managed, Span<byte> buffer)
            {
                _allocated = false;

                const int MaxUtf8BytesPerChar = 3;

                // >= for null terminator
                // Use the cast to long to avoid the checked operation
                if ((long)MaxUtf8BytesPerChar * managed.Length >= buffer.Length)
                {
                    // Calculate accurate byte count when the provided stack-allocated buffer is not sufficient
                    int exactByteCount = checked(Encoding.UTF8.GetByteCount(managed) + 1); // + 1 for null terminator
                    if (exactByteCount > buffer.Length)
                    {
                        buffer = new Span<byte>((byte*)NativeMemory.Alloc((nuint)exactByteCount), exactByteCount);
                        _allocated = true;
                    }
                }

                // Unsafe.AsPointer is safe since buffer must be pinned
                _unmanagedValue = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer));

                int byteCount = Encoding.UTF8.GetBytes(managed, buffer);
                buffer[byteCount] = 0; // null-terminate
            }

            /// <summary>
            /// Converts the current managed ReadOnlySpan&lt;char&gt; to an unmanaged string.
            /// </summary>
            /// <returns>An unmanaged string.</returns>
            public byte* ToUnmanaged() => _unmanagedValue;

            /// <summary>
            /// Frees any allocated unmanaged memory.
            /// </summary>
            public void Free()
            {
                if (_allocated)
                    NativeMemory.Free(_unmanagedValue);
            }
        }
    }
}
