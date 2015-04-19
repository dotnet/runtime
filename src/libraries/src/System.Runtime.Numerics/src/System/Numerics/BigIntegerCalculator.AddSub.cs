using System.Diagnostics;
using System.Security;

namespace System.Numerics
{
    internal static partial class BigIntegerCalculator
    {
        [SecuritySafeCritical]
        private unsafe static void Add(uint* left, int leftLength,
                                       uint* right, int rightLength,
                                       uint* bits, int bitsLength)
        {
            Debug.Assert(leftLength >= 0);
            Debug.Assert(rightLength >= 0);
            Debug.Assert(leftLength >= rightLength);
            Debug.Assert(bitsLength == leftLength + 1);

            // Executes the "grammar-school" algorithm for computing z = a + b.
            // While calculating z_i = a_i + b_i we take care of overflow:
            // Since a_i + b_i + c <= 2(2^32 - 1) + 1 = 2^33 - 1, our carry c
            // has always the value 1 or 0; hence, we're safe here.

            int i = 0;
            long carry = 0L;

            // adds the bits
            for (; i < rightLength; i++)
            {
                long digit = (left[i] + carry) + right[i];
                bits[i] = (uint)digit;
                carry = digit >> 32;
            }
            for (; i < leftLength; i++)
            {
                long digit = left[i] + carry;
                bits[i] = (uint)digit;
                carry = digit >> 32;
            }
            bits[i] = (uint)carry;
        }

        [SecuritySafeCritical]
        private unsafe static void AddSelf(uint* left, int leftLength,
                                           uint* right, int rightLength)
        {
            Debug.Assert(leftLength >= 0);
            Debug.Assert(rightLength >= 0);
            Debug.Assert(leftLength >= rightLength);

            // Executes the "grammar-school" algorithm for computing z = a + b.
            // Same as above, but we're writing the result directly to a and
            // stop execution, if we're out of b and c is already 0.

            int i = 0;
            long carry = 0L;

            // adds the bits
            for (; i < rightLength; i++)
            {
                long digit = (left[i] + carry) + right[i];
                left[i] = (uint)digit;
                carry = digit >> 32;
            }
            for (; carry != 0 && i < leftLength; i++)
            {
                long digit = left[i] + carry;
                left[i] = (uint)digit;
                carry = digit >> 32;
            }
        }
    }
}
