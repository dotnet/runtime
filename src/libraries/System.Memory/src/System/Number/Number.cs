// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// This code is copied almost verbatim from the same-named file in CoreRT with mechanical changes to Span-ify it.
//

namespace System
{
    internal static partial class Number
    {
        internal const int DECIMAL_PRECISION = 29;

        //
        // This method is copied directly from CoreRT (which is in turn a C#-ized version of the CoreCLR C++ code.)
        //
        public static void RoundNumber(ref NumberBuffer number, int pos)
        {
            number.CheckConsistency();

            Span<byte> digits = number.Digits;

            int i = 0;
            while (i < pos && digits[i] != 0)
                i++;

            if (i == pos && digits[i] >= (byte)'5')
            {
                while (i > 0 && digits[i - 1] == (byte)'9')
                    i--;

                if (i > 0)
                {
                    digits[i - 1]++;
                }
                else
                {
                    number.Scale++;
                    digits[0] = (byte)'1';
                    i = 1;
                }
            }
            else
            {
                while (i > 0 && digits[i - 1] == (byte)'0')
                    i--;
            }
            if (i == 0)
            {
                number.Scale = 0;
                number.IsNegative = false;
            }
            digits[i] = 0;

            number.CheckConsistency();
        }
    }
}
