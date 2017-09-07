// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Test for tail merging/duplication of search loops returning constants.

using System.Runtime.CompilerServices;

namespace N
{
    public static class C
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool HasPrimeUnderTwenty(int first, int last)
        {
            for (int n = first; n <= last; ++n)
            {
                if (n == 2) return true;
                if (n == 3) return true;
                if (n == 5) return true;
                if (n == 7) return true;
                if (n == 11) return true;
                if (n == 13) return true;
                if (n == 17) return true;
                if (n == 19) return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Distance_abCD(string s)
        {
            int c_index = 0;
            bool sawA = false;
            bool sawB = false;
            int index = 0;

            foreach (char c in s)
            {
                if (c == 'A') sawA = true;
                if (c == 'B')
                {
                    if (!sawA) return -1;
                    sawB = true;
                }
                if (c == 'C')
                {
                    if (!sawB) return -1;
                    c_index = index;
                }
                if (c == 'D')
                {
                    if (c_index == 0) return -1;
                    return (index - c_index);
                }
                ++index;
            }

            return -1;
        }

        public static int Main(string[] args)
        {
            if (HasPrimeUnderTwenty(22, 36) || !HasPrimeUnderTwenty(-1, 4))
            {
                return -1;
            }

            if ((Distance_abCD("xxxxAyyyyBzyzyzC1234D") != 5) || (Distance_abCD("nnABmmDC") != -1))
            {
                return -1;
            }

            return 100;
        }
    }
}
