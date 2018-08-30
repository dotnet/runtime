// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Internal.Runtime.CompilerServices;

namespace System
{
    internal static partial class Number
    {
        private const int NumberMaxDigits = 50; // needs to == NUMBER_MAXDIGITS in coreclr's src/classlibnative/bcltype/number.h.

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal unsafe ref struct NumberBuffer // needs to match layout of NUMBER in coreclr's src/classlibnative/bcltype/number.h
        {
            public int precision;                       //  0
            public int scale;                           //  4
            private int _sign;                          //  8
            private NumberBufferKind _kind;             // 12
            private char* _allDigits;                   // 16
            private DigitsAndNullTerminator _digits;    // 20 or 24

            public bool sign { get => _sign != 0; set => _sign = value ? 1 : 0; }
            public char* digits => (char*)Unsafe.AsPointer(ref _digits);
            public NumberBufferKind kind { get => _kind; set => _kind = value; }

            [StructLayout(LayoutKind.Sequential, Size = (NumberMaxDigits + 1) * sizeof(char))]
            private struct DigitsAndNullTerminator { }
        }

        internal enum NumberBufferKind // needs to match NUMBER_KIND in coreclr's src/classlibnative/bcltype/number.h
        {
            Unknown = 0,
            Integer = 1,
            Decimal = 2,
            Double = 3
        }
    }
}
