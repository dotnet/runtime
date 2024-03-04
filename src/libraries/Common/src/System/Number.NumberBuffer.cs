// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System
{
    internal static partial class Number
    {
        // We need 1 additional byte, per length, for the terminating null
        internal const int DecimalNumberBufferLength = 29 + 1 + 1;  // 29 for the longest input + 1 for rounding
        internal const int DoubleNumberBufferLength = 767 + 1 + 1;  // 767 for the longest input + 1 for rounding: 4.9406564584124654E-324
        internal const int Int32NumberBufferLength = 10 + 1;    // 10 for the longest input: 2,147,483,647
        internal const int Int64NumberBufferLength = 19 + 1;    // 19 for the longest input: 9,223,372,036,854,775,807
        internal const int Int128NumberBufferLength = 39 + 1;    // 39 for the longest input: 170,141,183,460,469,231,731,687,303,715,884,105,727
        internal const int SingleNumberBufferLength = 112 + 1 + 1;  // 112 for the longest input + 1 for rounding: 1.40129846E-45
        internal const int HalfNumberBufferLength = 21; // 19 for the longest input + 1 for rounding (+1 for the null terminator)
        internal const int UInt32NumberBufferLength = 10 + 1;   // 10 for the longest input: 4,294,967,295
        internal const int UInt64NumberBufferLength = 20 + 1;   // 20 for the longest input: 18,446,744,073,709,551,615
        internal const int UInt128NumberBufferLength = 39 + 1; // 39 for the longest input: 340,282,366,920,938,463,463,374,607,431,768,211,455

        internal unsafe ref struct NumberBuffer
        {
            public int DigitsCount;
            public int Scale;
            public bool IsNegative;
            public bool HasNonZeroTail;
            public NumberBufferKind Kind;
            public byte* DigitsPtr;
            public int DigitsLength;
            public readonly Span<byte> Digits => new Span<byte>(DigitsPtr, DigitsLength);

            public NumberBuffer(NumberBufferKind kind, byte* digits, int digitsLength) : this(kind, new Span<byte>(digits, digitsLength))
            {
                Debug.Assert(digits != null);
            }

            /// <summary>Initializes the NumberBuffer.</summary>
            /// <param name="kind">The kind of the buffer.</param>
            /// <param name="digits">The digits scratch space. The referenced memory must not be moveable, e.g. stack memory, pinned array, etc.</param>
            public NumberBuffer(NumberBufferKind kind, Span<byte> digits)
            {
                Debug.Assert(!digits.IsEmpty);

                DigitsCount = 0;
                Scale = 0;
                IsNegative = false;
                HasNonZeroTail = false;
                Kind = kind;
                DigitsPtr = Unsafe.AsPointer(ref MemoryMarshal.GetReference(digits)); // Safe since memory must be fixed
                DigitsLength = digits.Length;
#if DEBUG
                Digits.Fill(0xCC);
#endif
                Digits[0] = (byte)'\0';
                CheckConsistency();
            }

#pragma warning disable CA1822
            [Conditional("DEBUG")]
            public void CheckConsistency()
            {
#if DEBUG
                Debug.Assert((Kind == NumberBufferKind.Integer) || (Kind == NumberBufferKind.Decimal) || (Kind == NumberBufferKind.FloatingPoint));
                Debug.Assert(Digits[0] != '0', "Leading zeros should never be stored in a Number");

                int numDigits;
                for (numDigits = 0; numDigits < Digits.Length; numDigits++)
                {
                    byte digit = Digits[numDigits];

                    if (digit == 0)
                    {
                        break;
                    }

                    Debug.Assert(char.IsAsciiDigit((char)digit), $"Unexpected character found in Number: {digit}");
                }

                Debug.Assert(numDigits == DigitsCount, "Null terminator found in unexpected location in Number");
                Debug.Assert(numDigits < Digits.Length, "Null terminator not found in Number");
#endif // DEBUG
            }
#pragma warning restore CA1822

            //
            // Code coverage note: This only exists so that Number displays nicely in the VS watch window. So yes, I know it works.
            //
            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();

                sb.Append('[');
                sb.Append('"');

                for (int i = 0; i < Digits.Length; i++)
                {
                    byte digit = Digits[i];

                    if (digit == 0)
                    {
                        break;
                    }

                    sb.Append((char)(digit));
                }

                sb.Append('"');
                sb.Append(", Length = ").Append(DigitsCount);
                sb.Append(", Scale = ").Append(Scale);
                sb.Append(", IsNegative = ").Append(IsNegative);
                sb.Append(", HasNonZeroTail = ").Append(HasNonZeroTail);
                sb.Append(", Kind = ").Append(Kind);
                sb.Append(']');

                return sb.ToString();
            }
        }

        internal enum NumberBufferKind : byte
        {
            Unknown = 0,
            Integer = 1,
            Decimal = 2,
            FloatingPoint = 3,
        }
    }
}
