// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// Represents a marshaller for BSTR strings.
    /// </summary>
    [CLSCompliant(false)]
    [CustomMarshaller(typeof(string), MarshalMode.Default, typeof(BStrStringMarshaller))]
    [CustomMarshaller(typeof(string), MarshalMode.ManagedToUnmanagedIn, typeof(ManagedToUnmanagedIn))]
    public static unsafe class BStrStringMarshaller
    {
        /// <summary>
        /// Converts a string to an unmanaged version.
        /// </summary>
        /// <param name="managed">A managed string to convert.</param>
        /// <returns>The converted unmanaged string.</returns>
        public static ushort* ConvertToUnmanaged(string? managed)
            => (ushort*)Marshal.StringToBSTR(managed);

        /// <summary>
        /// Converts an unmanaged string to a managed version.
        /// </summary>
        /// <param name="unmanaged">An unmanaged string to convert.</param>
        /// <returns>The converted managed string.</returns>
        public static string? ConvertToManaged(ushort* unmanaged)
        {
            if (unmanaged is null)
                return null;

            return Marshal.PtrToStringBSTR((IntPtr)unmanaged);
        }

        /// <summary>
        /// Frees the memory for the unmanaged string.
        /// </summary>
        /// <param name="unmanaged">The memory allocated for the unmanaged string.</param>
        public static void Free(ushort* unmanaged)
            => Marshal.FreeBSTR((IntPtr)unmanaged);

        /// <summary>
        /// Custom marshaller to marshal a managed string as a ANSI unmanaged string.
        /// </summary>
        public ref struct ManagedToUnmanagedIn
        {
            /// <summary>
            /// Gets the requested buffer size for optimized marshalling.
            /// </summary>
            public static int BufferSize => 0x100;

            private ushort* _ptrToFirstChar;
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
                    _ptrToFirstChar = null;
                    return;
                }

                ushort* ptrToFirstChar;
                int lengthInBytes = checked(sizeof(char) * managed.Length);

                // A caller provided buffer must be at least (lengthInBytes + 6) bytes
                // in order to be constructed manually. The 6 extra bytes are 4 for byte length and 2 for wide null.
                int manualBstrNeeds = checked(lengthInBytes + 6);
                if (manualBstrNeeds > buffer.Length)
                {
                    // Use precise byte count when the provided stack-allocated buffer is not sufficient
                    ptrToFirstChar = (ushort*)Marshal.AllocBSTRByteLen((uint)lengthInBytes);
                    _allocated = true;
                }
                else
                {
                    // Set length and update buffer target
                    // Unsafe.AsPointer is safe since buffer must be pinned
                    byte* pBuffer = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer));
                    *((uint*)pBuffer) = (uint)lengthInBytes;
                    ptrToFirstChar = (ushort*)(pBuffer + sizeof(uint));
                }

                // Confirm the size is properly set for the allocated BSTR.
                Debug.Assert(lengthInBytes == Marshal.SysStringByteLen((IntPtr)ptrToFirstChar));

                // Copy characters from the managed string
                managed.CopyTo(new Span<char>(ptrToFirstChar, managed.Length));
                ptrToFirstChar[managed.Length] = '\0'; // null-terminate
                _ptrToFirstChar = ptrToFirstChar;
            }

            /// <summary>
            /// Converts the current managed string to an unmanaged string.
            /// </summary>
            /// <returns>The converted unmanaged string.</returns>
            public ushort* ToUnmanaged() => _ptrToFirstChar;

            /// <summary>
            /// Frees any allocated unmanaged string memory.
            /// </summary>
            public void Free()
            {
                if (_allocated)
                    BStrStringMarshaller.Free(_ptrToFirstChar);
            }
        }
    }
}
