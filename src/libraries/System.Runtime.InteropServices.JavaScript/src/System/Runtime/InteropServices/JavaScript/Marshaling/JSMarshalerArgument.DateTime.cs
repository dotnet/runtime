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
        public unsafe void ToManaged(out DateTimeOffset value)
        {
            if (slot.Type == MarshalerType.None)
            {
                value = default;
                return;
            }
            value = DateTimeOffset.FromUnixTimeMilliseconds((long)slot.DoubleValue);
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void ToJS(DateTimeOffset value)
        {
            slot.Type = MarshalerType.DateTimeOffset;
            slot.DoubleValue = (double)value.ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public unsafe void ToManaged(out DateTimeOffset? value)
        {
            if (slot.Type == MarshalerType.None)
            {
                value = null;
                return;
            }
            value = DateTimeOffset.FromUnixTimeMilliseconds((long)slot.DoubleValue);
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void ToJS(DateTimeOffset? value)
        {
            if (value.HasValue)
            {
                slot.Type = MarshalerType.DateTimeOffset;
                slot.DoubleValue = value.Value.ToUnixTimeMilliseconds(); ;
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
        public unsafe void ToManaged(out DateTime value)
        {
            if (slot.Type == MarshalerType.None)
            {
                value = default;
                return;
            }
            value = DateTimeOffset.FromUnixTimeMilliseconds((long)slot.DoubleValue).UtcDateTime;
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void ToJS(DateTime value)
        {
            slot.Type = MarshalerType.DateTime;
            slot.DoubleValue = new DateTimeOffset(value).ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public unsafe void ToManaged(out DateTime? value)
        {
            if (slot.Type == MarshalerType.None)
            {
                value = null;
                return;
            }
            value = DateTimeOffset.FromUnixTimeMilliseconds((long)slot.DoubleValue).UtcDateTime;
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void ToJS(DateTime? value)
        {
            if (value.HasValue)
            {
                slot.Type = MarshalerType.DateTime;
                slot.DoubleValue = new DateTimeOffset(value.Value).ToUnixTimeMilliseconds();
            }
            else
            {
                slot.Type = MarshalerType.None;
            }
        }
    }
}
