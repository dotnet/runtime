﻿// Licensed to the .NET Foundation under one or more agreements.
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

            fixed (void* argAsRoot = &slot.IntPtrValue)
            {
                value = Unsafe.AsRef<string>(argAsRoot);
            }
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
            Interop.Runtime.DeregisterGCRoot(slot.IntPtrValue);
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
            Interop.Runtime.RegisterGCRoot((IntPtr)payload, bytes, IntPtr.Zero);
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
