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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ToManaged(out IntPtr value)
        {
            if (slot.Type == MarshalerType.None)
            {
                value = default;
                return;
            }
            value = slot.IntPtrValue;
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ToJS(IntPtr value)
        {
            slot.Type = MarshalerType.IntPtr;
            slot.IntPtrValue = value;
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ToManaged(out IntPtr? value)
        {
            if (slot.Type == MarshalerType.None)
            {
                value = null;
                return;
            }
            value = slot.IntPtrValue;
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ToJS(IntPtr? value)
        {
            if (value.HasValue)
            {
                slot.Type = MarshalerType.IntPtr;
                slot.IntPtrValue = value.Value;
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ToManaged(out void* value)
        {
            if (slot.Type == MarshalerType.None)
            {
                value = default;
                return;
            }
            value = (byte*)slot.IntPtrValue;
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ToJS(void* value)
        {
            slot.Type = MarshalerType.IntPtr;
            slot.IntPtrValue = (IntPtr)value;
        }
    }
}
