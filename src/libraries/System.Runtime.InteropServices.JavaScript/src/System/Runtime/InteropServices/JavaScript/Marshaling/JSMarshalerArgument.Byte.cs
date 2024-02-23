// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.JavaScript
{
    public partial struct JSMarshalerArgument
    {
        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public unsafe void ToManaged(out byte value)
        {
            if (slot.Type == MarshalerType.None)
            {
                value = default;
                return;
            }
            value = slot.ByteValue;
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void ToJS(byte value)
        {
            slot.Type = MarshalerType.Byte;
            slot.ByteValue = value;
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public unsafe void ToManaged(out byte? value)
        {
            if (slot.Type == MarshalerType.None)
            {
                value = null;
                return;
            }
            value = slot.ByteValue;
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void ToJS(byte? value)
        {
            if (value.HasValue)
            {
                slot.Type = MarshalerType.Byte;
                slot.ByteValue = value.Value;
            }
            else
            {
                slot.Type = MarshalerType.None;
            }
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
        public unsafe void ToManaged(out byte[]? value)
        {
            if (slot.Type == MarshalerType.None)
            {
                value = null;
                return;
            }
            value = new byte[slot.Length];
            Marshal.Copy(slot.IntPtrValue, value, 0, slot.Length);
            Marshal.FreeHGlobal(slot.IntPtrValue);
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
        public unsafe void ToJS(byte[]? value)
        {
            if (value == null)
            {
                slot.Type = MarshalerType.None;
                return;
            }
            slot.Length = value.Length;
            slot.Type = MarshalerType.Array;
            slot.IntPtrValue = Marshal.AllocHGlobal(value.Length * sizeof(byte));
            slot.ElementType = MarshalerType.Byte;
            Marshal.Copy(value, 0, slot.IntPtrValue, slot.Length);
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
        // this only supports array round-trip, there is no way how to create ArraySegment in JS
        public unsafe void ToManaged(out ArraySegment<byte> value)
        {
            var array = (byte[])((GCHandle)slot.GCHandle).Target!;
            var refPtr = (IntPtr)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(array));
            int byteOffset = (int)(slot.IntPtrValue - (nint)refPtr);
            value = new ArraySegment<byte>(array, byteOffset, slot.Length);
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
        public unsafe void ToJS(ArraySegment<byte> value)
        {
            if (value.Array == null)
            {
                slot.Type = MarshalerType.None;
                return;
            }
            slot.Type = MarshalerType.ArraySegment;
            var ctx = ToJSContext;
            slot.GCHandle = ctx.GetJSOwnedObjectGCHandle(value.Array, GCHandleType.Pinned);
            var refPtr = (IntPtr)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(value.Array));
            slot.IntPtrValue = refPtr + value.Offset;
            slot.Length = value.Count;
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
        public unsafe void ToManaged(out Span<byte> value)
        {
            value = new Span<byte>((void*)slot.IntPtrValue, slot.Length);
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <remarks>caller is responsible for pinning.</remarks>
        /// <param name="value">The value to be marshaled.</param>
        public unsafe void ToJS(Span<byte> value)
        {
            slot.Length = value.Length;
            slot.IntPtrValue = (IntPtr)Unsafe.AsPointer(ref value.GetPinnableReference());
            slot.Type = MarshalerType.Span;
        }
    }
}
