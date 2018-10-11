// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Internal.Runtime.CompilerServices;

namespace System
{
    internal static partial class Number
    {
        private const int NumberMaxDigits = 50;

        private const double Log10V2 = 0.30102999566398119521373889472449;

        // DriftFactor = 1 - Log10V2 - epsilon (a small number account for drift of floating point multiplication)
        private const double DriftFactor = 0.69;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal unsafe ref struct NumberBuffer
        {
            public int precision;
            public int scale;
            public bool sign;
            public NumberBufferKind kind;
            public fixed char digits[NumberMaxDigits + 1];

            public char* GetDigitsPointer()
            {
                // This is safe to do since we are a ref struct
                return (char*)(Unsafe.AsPointer(ref digits[0]));
            }
        }

        internal enum NumberBufferKind : byte
        {
            Unknown = 0,
            Integer = 1,
            Decimal = 2,
            Double = 3
        }
    }
}
