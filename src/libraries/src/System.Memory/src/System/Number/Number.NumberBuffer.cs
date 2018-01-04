// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;

#if !netstandard
using Internal.Runtime.CompilerServices;
#else
using System.Runtime.CompilerServices;
#endif

namespace System
{
    //
    // This is a port of the Number/NumberBuffer structure from CoreRT (which in turn was a C#-ized port of the NUMBER struct inside CoreCLR.)
    // We use this as the more heavyweight data types such as Decimal and floats. That is, we use Number as a common representation of these types
    // and thus share the formatting/parsing routines among them.
    //
    // This structure can only be stack-allocated as it returns a reference to a fixed-length array inside.
    //
    [StructLayout(LayoutKind.Sequential)]
    internal ref struct NumberBuffer
    {
        // The Scale is the index of the implied decimal point. Can be negative or beyond the end of the NUL terminator. Examples:
        //
        //    123.45        => "12345", Scale = 3
        //    0.005         => "5", Scale = -2
        //    1000.00       => "1", Scale = 4 (though it's not guaranteed that it won't be "1000, Scale=0" instead.)
        //    0             => "", Scale = 0
        //    3m            => "3", Scale = 1
        //    3.00m         => "300", Scale = 1 (this is important: trailing zeroes actually matter in Decimal)
        //
        public int Scale;
        public bool IsNegative;

        public unsafe Span<byte> Digits => new Span<byte>(Unsafe.AsPointer(ref _b0), BufferSize);

        public unsafe byte* UnsafeDigits => (byte*)Unsafe.AsPointer(ref _b0);

        public int NumDigits => Digits.IndexOf<byte>(0);

        [Conditional("DEBUG")]
        public void CheckConsistency()
        {
#if DEBUG
            Span<byte> digits = Digits;

            Debug.Assert(digits[0] != '0', "Leading zeros should never be stored in a Number");

            int numDigits;
            for (numDigits = 0; numDigits < BufferSize; numDigits++)
            {
                byte digit = digits[numDigits];
                if (digit == 0)
                    break;

                Debug.Assert(digit >= '0' && digit <= '9', "Unexpected character found in Number");
            }

            Debug.Assert(numDigits < BufferSize, "NUL terminator not found in Number");
#endif // DEBUG
        }

        //
        // Code coverage note: This only exists so that Number displays nicely in the VS watch window. So yes, I know it works.
        //
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('[');
            sb.Append('"');
            Span<byte> digits = Digits;
            for (int i = 0; i < BufferSize; i++)
            {
                byte digit = digits[i];
                if (digit == 0)
                    break;
                sb.Append((char)digit);
            }
            sb.Append('"');
            sb.Append(", Scale = " + Scale);
            sb.Append(", IsNegative   = " + IsNegative);
            sb.Append(']');
            return sb.ToString();
        }

        public const int BufferSize = 50 + 1; // Matches https://github.com/dotnet/coreclr/blob/097e68658c5249eaefff33bd92b044e9ba22c819/src/classlibnative/bcltype/number.h#L15

        //
        // 50+1 bytes of ASCII digits ('0'..'9'). (Not using "fixed byte[]" as this breaks the VS debugging experience.) 
        // That's enough room to store the worst case Decimal and double.
        //
        // A NUL terminator (not to be confused with '0') marks the end of the digits. 
        //
        // Leading zeroes are never stored, even if the entire number is zero.
        //
        // Trailing zeroes after the decimal point *are* stored. This is important for System.Decimal
        // as trailing zeroes are significant in Decimal:
        //
        //    decimal d1 = 1m;      => d1.ToString("G") emits "1"
        //    decimal d2 = 1.00m;   => d1.ToStirng("G") emits "1.00"
        //
        private byte _b0;
        private byte _b1;
        private byte _b2;
        private byte _b3;
        private byte _b4;
        private byte _b5;
        private byte _b6;
        private byte _b7;
        private byte _b8;
        private byte _b9;
        private byte _b10;
        private byte _b11;
        private byte _b12;
        private byte _b13;
        private byte _b14;
        private byte _b15;
        private byte _b16;
        private byte _b17;
        private byte _b18;
        private byte _b19;
        private byte _b20;
        private byte _b21;
        private byte _b22;
        private byte _b23;
        private byte _b24;
        private byte _b25;
        private byte _b26;
        private byte _b27;
        private byte _b28;
        private byte _b29;
        private byte _b30;
        private byte _b31;
        private byte _b32;
        private byte _b33;
        private byte _b34;
        private byte _b35;
        private byte _b36;
        private byte _b37;
        private byte _b38;
        private byte _b39;
        private byte _b40;
        private byte _b41;
        private byte _b42;
        private byte _b43;
        private byte _b44;
        private byte _b45;
        private byte _b46;
        private byte _b47;
        private byte _b48;
        private byte _b49;
        private byte _b50;
    }
}
