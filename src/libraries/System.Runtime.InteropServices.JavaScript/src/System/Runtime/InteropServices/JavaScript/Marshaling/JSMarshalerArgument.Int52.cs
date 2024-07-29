// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.JavaScript
{
    public partial struct JSMarshalerArgument
    {
        private const long I52_MAX_VALUE = ((1L << 53) - 1);
        private const long I52_MIN_VALUE = -I52_MAX_VALUE;

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public unsafe void ToManaged(out long value)
        {
            if (slot.Type == MarshalerType.None)
            {
                value = default;
                return;
            }
            value = (long)slot.DoubleValue;
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void ToJS(long value)
        {
            if (value < I52_MIN_VALUE || value > I52_MAX_VALUE)
            {
                throw new OverflowException(SR.Format(SR.ValueOutOf52BitRange, value, I52_MIN_VALUE, I52_MAX_VALUE));
            }

            slot.Type = MarshalerType.Int52;
            slot.DoubleValue = value;
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public unsafe void ToManaged(out long? value)
        {
            if (slot.Type == MarshalerType.None)
            {
                value = null;
                return;
            }
            value = (long)slot.DoubleValue;
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void ToJS(long? value)
        {
            if (value.HasValue)
            {
                if (value.Value < I52_MIN_VALUE || value.Value > I52_MAX_VALUE)
                {
                    throw new OverflowException(SR.Format(SR.ValueOutOf52BitRange, value, I52_MIN_VALUE, I52_MAX_VALUE));
                }
                slot.Type = MarshalerType.Int52;
                slot.DoubleValue = value.Value;
            }
            else
            {
                slot.Type = MarshalerType.None;
            }
        }
    }
}
