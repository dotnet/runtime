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
        public unsafe void ToManaged(out string? value)
        {
            if (slot.Type == MarshalerType.None)
            {
                value = null;
                return;
            }
#if ENABLE_JS_INTEROP_BY_VALUE
            value = Marshal.PtrToStringUni(slot.IntPtrValue, slot.Length);
            Marshal.FreeHGlobal(slot.IntPtrValue);
#else
            fixed (void* argAsRoot = &slot.IntPtrValue)
            {
                value = Unsafe.AsRef<string>(argAsRoot);
            }
#endif
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ToJS(string? value)
        {
            if (value == null)
            {
                slot.Type = MarshalerType.None;
            }
            else
            {
                slot.Type = MarshalerType.String;
#if ENABLE_JS_INTEROP_BY_VALUE
                slot.IntPtrValue = Marshal.StringToHGlobalUni(value); // alloc, JS side will free
                slot.Length = value.Length;
#else
                // here we treat JSMarshalerArgument.IntPtrValue as root, because it's allocated on stack
                // or we register the buffer with JSFunctionBinding._RegisterGCRoot
                // We assume that GC would keep updating on GC move
                // On JS side we wrap it with WasmExternalRoot
                fixed (IntPtr* argAsRoot = &slot.IntPtrValue)
                {
                    string cpy = value;
                    var currentRoot = (IntPtr*)Unsafe.AsPointer(ref cpy);
                    argAsRoot[0] = currentRoot[0];
                }
#endif
            }
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ToManaged(out string?[]? value)
        {
            if (slot.Type == MarshalerType.None)
            {
                value = null;
                return;
            }

            value = new string?[slot.Length];
            JSMarshalerArgument* payload = (JSMarshalerArgument*)slot.IntPtrValue;
            for (int i = 0; i < slot.Length; i++)
            {
                ref JSMarshalerArgument arg = ref payload[i];
                string? val;
                arg.ToManaged(out val);
                value[i] = val;
            }
#if !ENABLE_JS_INTEROP_BY_VALUE
            Interop.Runtime.DeregisterGCRoot(slot.IntPtrValue);
#endif
            Marshal.FreeHGlobal(slot.IntPtrValue);
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ToJS(string?[] value)
        {
            if (value == null)
            {
                slot.Type = MarshalerType.None;
                return;
            }
            slot.Length = value.Length;
            int bytes = value.Length * Marshal.SizeOf(typeof(JSMarshalerArgument));
            slot.Type = MarshalerType.Array;
            JSMarshalerArgument* payload = (JSMarshalerArgument*)Marshal.AllocHGlobal(bytes);
            Unsafe.InitBlock(payload, 0, (uint)bytes);
#if !ENABLE_JS_INTEROP_BY_VALUE
            Interop.Runtime.RegisterGCRoot(payload, bytes, IntPtr.Zero);
#endif
            for (int i = 0; i < slot.Length; i++)
            {
                ref JSMarshalerArgument arg = ref payload[i];
                string? val = value[i];
                arg.ToJS(val);
                value[i] = val;
            }
            slot.IntPtrValue = (IntPtr)payload;
            slot.ElementType = MarshalerType.String;
        }
    }
}
