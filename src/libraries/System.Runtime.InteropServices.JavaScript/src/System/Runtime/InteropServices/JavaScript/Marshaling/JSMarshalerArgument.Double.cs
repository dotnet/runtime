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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ToManaged(out double value)
        {
            if (slot.Type == MarshalerType.None)
            {
                value = default;
                return;
            }
            value = slot.DoubleValue;
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ToJS(double value)
        {
            slot.Type = MarshalerType.Double;
            slot.DoubleValue = value;
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ToManaged(out double? value)
        {
            if (slot.Type == MarshalerType.None)
            {
                value = null;
                return;
            }
            value = slot.DoubleValue;
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ToJS(double? value)
        {
            if (value.HasValue)
            {
                slot.Type = MarshalerType.Double;
                slot.DoubleValue = value.Value;
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ToManaged(out double[]? value)
        {
            if (slot.Type == MarshalerType.None)
            {
                value = null;
                return;
            }
            value = new double[slot.Length];
            Marshal.Copy(slot.IntPtrValue, value, 0, slot.Length);
            Marshal.FreeHGlobal(slot.IntPtrValue);
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ToJS(double[] value)
        {
            if (value == null)
            {
                slot.Type = MarshalerType.None;
                return;
            }
            slot.Type = MarshalerType.Array;
            slot.IntPtrValue = Marshal.AllocHGlobal(value.Length * sizeof(double));
            slot.Length = value.Length;
            slot.ElementType = MarshalerType.Double;
            Marshal.Copy(value, 0, slot.IntPtrValue, slot.Length);
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
        // this only supports array round-trip
        public unsafe void ToManaged(out ArraySegment<double> value)
        {
            var array = (double[])((GCHandle)slot.GCHandle).Target!;
            var refPtr = (IntPtr)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(array));
            int byteOffset = (int)(slot.IntPtrValue - (nint)refPtr);
            value = new ArraySegment<double>(array, byteOffset / sizeof(double), slot.Length);
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
        public unsafe void ToJS(ArraySegment<double> value)
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
            slot.IntPtrValue = refPtr + (value.Offset * sizeof(double));
            slot.Length = value.Count;
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
        public unsafe void ToManaged(out Span<double> value)
        {
            value = new Span<double>((void*)slot.IntPtrValue, slot.Length);
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <remarks>caller is responsible for pinning.</remarks>
        /// <param name="value">The value to be marshaled.</param>
        public unsafe void ToJS(Span<double> value)
        {
            slot.Length = value.Length;
            slot.IntPtrValue = (IntPtr)Unsafe.AsPointer(ref value.GetPinnableReference());
            slot.Type = MarshalerType.Span;
        }
    }
}
