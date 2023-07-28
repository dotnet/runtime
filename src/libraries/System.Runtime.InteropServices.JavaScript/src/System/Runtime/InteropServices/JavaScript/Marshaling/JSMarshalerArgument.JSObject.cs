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
        public unsafe void ToManaged(out JSObject? value)
        {
            if (slot.Type == MarshalerType.None)
            {
                value = null;
                return;
            }
            value = JSHostImplementation.CreateCSOwnedProxy(slot.JSHandle);
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ToJS(JSObject? value)
        {
            if (value == null)
            {
                slot.Type = MarshalerType.None;
            }
            else
            {
#if FEATURE_WASM_THREADS
                JSObject.AssertThreadAffinity(value);
#endif
                ObjectDisposedException.ThrowIf(value.IsDisposed, value);
                slot.Type = MarshalerType.JSObject;
                slot.JSHandle = value.JSHandle;
                ObjectDisposedException.ThrowIf(slot.JSHandle == IntPtr.Zero, value);
            }
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ToManaged(out JSObject?[]? value)
        {
            if (slot.Type == MarshalerType.None)
            {
                value = null;
                return;
            }

            value = new JSObject?[slot.Length];
            JSMarshalerArgument* payload = (JSMarshalerArgument*)slot.IntPtrValue;
            for (int i = 0; i < slot.Length; i++)
            {
                ref JSMarshalerArgument arg = ref payload[i];
                JSObject? val;
                arg.ToManaged(out val);
                value[i] = val;
            }
            Marshal.FreeHGlobal(slot.IntPtrValue);
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ToJS(JSObject?[] value)
        {
            if (value == null)
            {
                slot.Type = MarshalerType.None;
                return;
            }
            slot.Length = value.Length;
            int bytes = value.Length * Marshal.SizeOf(typeof(JSMarshalerArgument));
            slot.Type = MarshalerType.Array;
            slot.ElementType = MarshalerType.JSObject;
            JSMarshalerArgument* payload = (JSMarshalerArgument*)Marshal.AllocHGlobal(bytes);
            Unsafe.InitBlock(payload, 0, (uint)bytes);
            for (int i = 0; i < slot.Length; i++)
            {
                ref JSMarshalerArgument arg = ref payload[i];
                JSObject? val = value[i];
                arg.ToJS(val);
                value[i] = val;
            }
            slot.IntPtrValue = (IntPtr)payload;
        }
    }
}
