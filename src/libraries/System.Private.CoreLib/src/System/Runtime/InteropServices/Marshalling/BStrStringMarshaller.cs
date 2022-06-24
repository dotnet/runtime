// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// Marshaller for BSTR strings
    /// </summary>
    [CLSCompliant(false)]
    [CustomTypeMarshaller(typeof(string), BufferSize = 0x100,
        Features = CustomTypeMarshallerFeatures.UnmanagedResources | CustomTypeMarshallerFeatures.TwoStageMarshalling | CustomTypeMarshallerFeatures.CallerAllocatedBuffer)]
    public unsafe ref struct BStrStringMarshaller
    {
        private void* _ptrToFirstChar;
        private bool _allocated;

        /// <summary>
        /// Initializes a new instance of the <see cref="BStrStringMarshaller"/>.
        /// </summary>
        /// <param name="str">The string to marshal.</param>
        public BStrStringMarshaller(string? str)
            : this(str, default)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="BStrStringMarshaller"/>.
        /// </summary>
        /// <param name="str">The string to marshal.</param>
        /// <param name="buffer">Buffer that may be used for marshalling.</param>
        /// <remarks>
        /// The <paramref name="buffer"/> must not be movable - that is, it should not be
        /// on the managed heap or it should be pinned.
        /// <seealso cref="CustomTypeMarshallerFeatures.CallerAllocatedBuffer"/>
        /// </remarks>
        public BStrStringMarshaller(string? str, Span<ushort> buffer)
        {
            _allocated = false;

            if (str is null)
            {
                _ptrToFirstChar = null;
                return;
            }

            ushort* ptrToFirstChar;
            int lengthInBytes = checked(sizeof(char) * str.Length);

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
                byte* pBuffer = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer));
                *((uint*)pBuffer) = (uint)lengthInBytes;
                ptrToFirstChar = (ushort*)(pBuffer + sizeof(uint));
            }

            // Confirm the size is properly set for the allocated BSTR.
            Debug.Assert(lengthInBytes == Marshal.SysStringByteLen((IntPtr)ptrToFirstChar));

            // Copy characters from the managed string
            str.CopyTo(new Span<char>(ptrToFirstChar, str.Length));
            ptrToFirstChar[str.Length] = '\0'; // null-terminate
            _ptrToFirstChar = ptrToFirstChar;
        }

        /// <summary>
        /// Returns the native value representing the string.
        /// </summary>
        /// <remarks>
        /// <seealso cref="CustomTypeMarshallerFeatures.TwoStageMarshalling"/>
        /// </remarks>
        public void* ToNativeValue() => _ptrToFirstChar;

        /// <summary>
        /// Sets the native value representing the string.
        /// </summary>
        /// <param name="value">The native value.</param>
        /// <remarks>
        /// <seealso cref="CustomTypeMarshallerFeatures.TwoStageMarshalling"/>
        /// </remarks>
        public void FromNativeValue(void* value)
        {
            _ptrToFirstChar = value;
            _allocated = true;
        }

        /// <summary>
        /// Returns the managed string.
        /// </summary>
        /// <remarks>
        /// <seealso cref="CustomTypeMarshallerDirection.Out"/>
        /// </remarks>
        public string? ToManaged()
        {
            if (_ptrToFirstChar is null)
                return null;

            return Marshal.PtrToStringBSTR((IntPtr)_ptrToFirstChar);
        }

        /// <summary>
        /// Frees native resources.
        /// </summary>
        /// <remarks>
        /// <seealso cref="CustomTypeMarshallerFeatures.UnmanagedResources"/>
        /// </remarks>
        public void FreeNative()
        {
            if (_allocated)
                Marshal.FreeBSTR((IntPtr)_ptrToFirstChar);
        }
    }
}
