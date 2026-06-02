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
        public void ToManaged(out float value)
        {
            if (slot.Type == MarshalerType.None)
            {
                value = default;
                return;
            }
            value = slot.SingleValue;
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void ToJS(float value)
        {
            slot.Type = MarshalerType.Single;
            slot.SingleValue = value;
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void ToManaged(out float? value)
        {
            if (slot.Type == MarshalerType.None)
            {
                value = null;
                return;
            }
            value = slot.SingleValue;
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void ToJS(float? value)
        {
            if (value.HasValue)
            {
                slot.Type = MarshalerType.Single;
                slot.SingleValue = value.Value;
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
#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public unsafe void ToManaged(out float[]? value)
        {
            if (slot.Type == MarshalerType.None)
            {
                value = null;
                return;
            }
            value = new float[slot.Length];
            Marshal.Copy(slot.IntPtrValue, value, 0, slot.Length);
            NativeMemory.Free((void*)slot.IntPtrValue);
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public unsafe void ToJS(float[] value)
        {
            if (value == null)
            {
                slot.Type = MarshalerType.None;
                return;
            }
            slot.Type = MarshalerType.Array;
            slot.IntPtrValue = (IntPtr)NativeMemory.Alloc((nuint)(value.Length * sizeof(float)));
            slot.Length = value.Length;
            slot.ElementType = MarshalerType.Single;
            Marshal.Copy(value, 0, slot.IntPtrValue, slot.Length);
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
        // this only supports array round-trip
        public unsafe void ToManaged(out ArraySegment<float> value)
        {
            var array = (float[])((GCHandle)slot.GCHandle).Target!;
            var refPtr = (IntPtr)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(array));
            int byteOffset = (int)(slot.IntPtrValue - (nint)refPtr);
            value = new ArraySegment<float>(array, byteOffset / sizeof(float), slot.Length);
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
        public unsafe void ToJS(ArraySegment<float> value)
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
            slot.IntPtrValue = refPtr + (value.Offset * sizeof(float));
            slot.Length = value.Count;
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
        public unsafe void ToManaged(out Span<float> value)
        {
            value = new Span<float>((void*)slot.IntPtrValue, slot.Length);
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <remarks>caller is responsible for pinning.</remarks>
        /// <param name="value">The value to be marshaled.</param>
        public unsafe void ToJS(Span<float> value)
        {
            slot.Length = value.Length;
            slot.IntPtrValue = (IntPtr)Unsafe.AsPointer(ref value.GetPinnableReference());
            slot.Type = MarshalerType.Span;
        }
    }
}
