using System.Diagnostics;
using System.Security;

namespace System.Numerics
{
    internal static partial class BigIntegerCalculator
    {
        public static uint[] Divide(uint[] left, uint right,
                                    out uint[] remainder)
        {
            Debug.Assert(left != null);
            Debug.Assert(left.Length >= 1);

            // Executes the division for one big and one 32-bit integer.
            // Thus, we've similar code than below, but there is no loop for
            // processing the 32-bit integer, since it's a single element.

            uint[] quotient = new uint[left.Length];

            ulong carry = 0UL;
            for (int i = left.Length - 1; i >= 0; i--)
            {
                ulong value = (carry << 32) | left[i];
                quotient[i] = (uint)(value / right);
                carry = value % right;
            }
            remainder = new uint[] { (uint)carry };

            return quotient;
        }

        public static uint[] Divide(uint[] left, uint right)
        {
            Debug.Assert(left != null);
            Debug.Assert(left.Length >= 1);

            // Same as above, but only computing the quotient.

            uint[] quotient = new uint[left.Length];

            ulong carry = 0UL;
            for (int i = left.Length - 1; i >= 0; i--)
            {
                ulong value = (carry << 32) | left[i];
                quotient[i] = (uint)(value / right);
                carry = value % right;
            }

            return quotient;
        }

        public static uint[] Remainder(uint[] left, uint right)
        {
            Debug.Assert(left != null);
            Debug.Assert(left.Length >= 1);

            // Same as above, but only computing the remainder.

            ulong carry = 0UL;
            for (int i = left.Length - 1; i >= 0; i--)
            {
                ulong value = (carry << 32) | left[i];
                carry = value % right;
            }

            return new uint[] { (uint)carry };
        }

        [SecuritySafeCritical]
        public unsafe static uint[] Divide(uint[] left, uint[] right,
                                           out uint[] remainder)
        {
            Debug.Assert(left != null);
            Debug.Assert(right != null);
            Debug.Assert(left.Length >= 1);
            Debug.Assert(right.Length >= 1);
            Debug.Assert(left.Length >= right.Length);

            // Switching to unsafe pointers helps sparing
            // some nasty index calculations...

            uint[] quotient = new uint[left.Length - right.Length + 1];
            remainder = new uint[right.Length];

            fixed(uint* l = left, r = right, q = quotient, e = remainder)
            {
                Divide(l, left.Length,
                       r, right.Length,
                       q, quotient.Length,
                       e, remainder.Length);
            }

            return quotient;
        }

        [SecuritySafeCritical]
        public unsafe static uint[] Divide(uint[] left, uint[] right)
        {
            Debug.Assert(left != null);
            Debug.Assert(right != null);
            Debug.Assert(left.Length >= 1);
            Debug.Assert(right.Length >= 1);
            Debug.Assert(left.Length >= right.Length);

            // Same as above, but only returning the quotient.

            uint[] quotient = new uint[left.Length - right.Length + 1];

            fixed (uint* l = left, r = right, q = quotient)
            {
                Divide(l, left.Length,
                       r, right.Length,
                       q, quotient.Length,
                       null, 0);
            }

            return quotient;
        }

        [SecuritySafeCritical]
        public unsafe static uint[] Remainder(uint[] left, uint[] right)
        {
            Debug.Assert(left != null);
            Debug.Assert(right != null);
            Debug.Assert(left.Length >= 1);
            Debug.Assert(right.Length >= 1);
            Debug.Assert(left.Length >= right.Length);

            // Same as above, but only returning the remainder.

            uint[] remainder = new uint[right.Length];

            fixed (uint* l = left, r = right, e = remainder)
            {
                Divide(l, left.Length,
                       r, right.Length,
                       null, 0,
                       e, remainder.Length);
            }

            return remainder;
        }

        [SecuritySafeCritical]
        private unsafe static void Divide(uint* left, int leftLength,
                                          uint* right, int rightLength,
                                          uint* quotient, int quotientLength,
                                          uint* remainder, int remainderLength)
        {
            Debug.Assert(leftLength >= 1);
            Debug.Assert(rightLength >= 1);
            Debug.Assert(leftLength >= rightLength);
            Debug.Assert(quotientLength == leftLength - rightLength + 1
                || quotientLength == 0);
            Debug.Assert(remainderLength == rightLength
                || remainderLength == 0);

            // Executes the "grammar-school" algorithm for computing q = a / b.
            // Before calculating q_i, we get more bits into the highest bit
            // block of the divisor. Thus, guessing digits of the quotient
            // will be more precise. Additionally we'll get r = a % b.

            int dividendLength = leftLength + 1;
            int divisorLength = rightLength;
            fixed (uint* dividend = new uint[dividendLength],
                         divisor = new uint[divisorLength],
                         guess = new uint[divisorLength])
            {
                // This will create private copies of left and right, so we can
                // modify the actual values of the dividend during computation
                // without breaking immutability of the calling structure!
                int shift = LeadingZeros(right[rightLength - 1]);
                LeftShift(left, leftLength, dividend, dividendLength, shift);
                LeftShift(right, rightLength, divisor, divisorLength, shift);

                // Measure dividend again; maybe there aren't any additional
                // bits resulting of our shift above to the left?
                if (dividend[dividendLength - 1] == 0)
                    --dividendLength;

                // These values will come in handy
                uint divHi = divisor[divisorLength - 1];
                uint divLo = divisorLength > 1 ? divisor[divisorLength - 2] : 0;
                int guessLength = 0;
                int delta = 0;

                // First, we subtract the divisor until our dividend is smaller,
                // if we shift the divisor so they have equal length. This will
                // ensure that the highest digit of the dividend is smaller or
                // equal to the highest digit of the divisor...
                do
                {
                    int n = dividendLength - divisorLength;
                    delta = Compare(dividend + n, divisorLength,
                                    divisor, divisorLength);
                    if (delta >= 0)
                    {
                        if (quotientLength != 0)
                            ++quotient[n];
                        SubtractSelf(dividend + n, divisorLength,
                                     divisor, divisorLength);
                    }
                }
                while (delta > 0);

                // Then, we divide the rest of the bits as we would do it using
                // pen and paper: guessing the next digit, subtracting, ...
                for (int i = dividendLength - 1; i >= divisorLength; i--)
                {
                    int n = i - divisorLength;

                    // First guess for the current digit of the quotient,
                    // which naturally must have only 32 bits...
                    ulong valHi = ((ulong)dividend[i] << 32) | dividend[i - 1];
                    ulong digit = valHi / divHi;
                    if (digit > 0xFFFFFFFF)
                        digit = 0xFFFFFFFF;

                    // Our first guess may be a little bit to big
                    ulong check = divHi * digit + ((divLo * digit) >> 32);
                    if (check > valHi)
                        --digit;

                    // Our guess may be still a little bit to big
                    do
                    {
                        MultiplyDivisor(divisor, divisorLength, digit, guess);
                        guessLength = guess[divisorLength] == 0
                                    ? divisorLength : divisorLength + 1;
                        delta = Compare(dividend + n, guessLength,
                                        guess, guessLength);
                        if (delta < 0)
                            --digit;
                    }
                    while (delta < 0);

                    // We have the digit!
                    SubtractSelf(dividend + n, guessLength,
                                 guess, guessLength);
                    if (quotientLength != 0)
                        quotient[n] = (uint)digit;
                }

                if (remainderLength != 0)
                {
                    // Repairing the remaining dividend gets the remainder
                    RightShift(dividend, divisorLength,
                               remainder, remainderLength,
                               shift);
                }
            }
        }

        [SecuritySafeCritical]
        private unsafe static void LeftShift(uint* value, int valueLength,
                                             uint* target, int targetLength,
                                             int shift)
        {
            Debug.Assert(valueLength >= 1);
            Debug.Assert(targetLength == valueLength ||
                         targetLength == valueLength + 1);
            Debug.Assert(shift >= 0 && shift < 32);

            if (shift > 0)
            {
                int backShift = 32 - shift;
                target[0] = value[0] << shift;
                for (int i = 1; i < valueLength; i++)
                {
                    target[i] = (value[i] << shift)
                        | (value[i - 1] >> backShift);
                }
                if (targetLength > valueLength)
                {
                    target[valueLength] =
                        value[valueLength - 1] >> backShift;
                }
            }
            else
            {
                for (int i = 0; i < valueLength; i++)
                    target[i] = value[i];
            }
        }

        [SecuritySafeCritical]
        private unsafe static void RightShift(uint* value, int valueLength,
                                              uint* target, int targetLength,
                                              int shift)
        {
            Debug.Assert(valueLength >= 1);
            Debug.Assert(targetLength == valueLength);
            Debug.Assert(shift >= 0 && shift < 32);

            if (shift > 0)
            {
                int backShift = 32 - shift;
                for (int i = 0; i < valueLength - 1; i++)
                {
                    target[i] = (value[i] >> shift)
                        | (value[i + 1] << backShift);
                }
                target[valueLength - 1] =
                    (value[valueLength - 1] >> shift);
            }
            else
            {
                for (int i = 0; i < valueLength; i++)
                    target[i] = value[i];
            }
        }

        [SecuritySafeCritical]
        private unsafe static void MultiplyDivisor(uint* left, int leftLength,
                                                   ulong right, uint* bits)
        {
            Debug.Assert(leftLength >= 1);
            Debug.Assert(right <= 0xFFFFFFFF);

            ulong carry = 0UL;
            for (int i = 0; i < leftLength; i++)
            {
                ulong digits = left[i] * right + carry;
                bits[i] = (uint)digits;
                carry = digits >> 32;
            }
            bits[leftLength] = (uint)carry;
        }

        private static int LeadingZeros(uint value)
        {
            if (value == 0)
                return 32;

            var count = 0;
            if ((value & 0xFFFF0000) == 0)
            {
                count += 16;
                value = value << 16;
            }
            if ((value & 0xFF000000) == 0)
            {
                count += 8;
                value = value << 8;
            }
            if ((value & 0xF0000000) == 0)
            {
                count += 4;
                value = value << 4;
            }
            if ((value & 0xC0000000) == 0)
            {
                count += 2;
                value = value << 2;
            }
            if ((value & 0x80000000) == 0)
            {
                count += 1;
            }

            return count;
        }
    }
}
