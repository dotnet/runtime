// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace TestCompareZero
{
    public class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Fact]
        public static int CheckCompareZero()
        {
            bool fail = false;

            if (AddGtZero(-3, 4) != 1) fail = true;
            if (AddGtZero(3, -3) != 0) fail = true;
            if (AddGtZero(-5, -10) != 0) fail = true;
            if (AddGtZero(int.MaxValue, 1) != 0) fail = true;
            if (AddGtZero(int.MinValue, -1) != 1) fail = true;

            if (AddGeZero(1, 1) != 1) fail = true;
            if (AddGeZero(0, 0) != 1) fail = true;
            if (AddGeZero(-1, -1) != 0) fail = true;
            if (AddGeZero(int.MaxValue, 1) != 0) fail = true;
            if (AddGeZero(int.MinValue, -1) != 1) fail = true;

            if (AddLtZero(1, 1) != 0) fail = true;
            if (AddLtZero(0, 0) != 0) fail = true;
            if (AddLtZero(-1, -1) != 1) fail = true;
            if (AddLtZero(int.MaxValue, 1) != 1) fail = true;
            if (AddLtZero(int.MinValue, -1) != 0) fail = true;

            if (AddLeZero(1, 1) != 0) fail = true;
            if (AddLeZero(0, 0) != 1) fail = true;
            if (AddLeZero(-1, -1) != 1) fail = true;
            if (AddLeZero(int.MaxValue, 1) != 1) fail = true;
            if (AddLeZero(int.MinValue, -1) != 0) fail = true;

            if (SubGtZero(5, 3) != 1) fail = true;
            if (SubGtZero(3, 3) != 0) fail = true;
            if (SubGtZero(2, 4) != 0) fail = true;
            if (SubGtZero(int.MinValue, 1) != 1) fail = true;
            if (SubGtZero(int.MaxValue, -1) != 0) fail = true;

            if (SubGeZero(5, 3) != 1) fail = true;
            if (SubGeZero(3, 3) != 1) fail = true;
            if (SubGeZero(2, 4) != 0) fail = true;
            if (SubGeZero(int.MinValue, 1) != 1) fail = true;
            if (SubGeZero(int.MaxValue, -1) != 0) fail = true;

            if (SubLtZero(5, 3) != 0) fail = true;
            if (SubLtZero(3, 3) != 0) fail = true;
            if (SubLtZero(2, 4) != 1) fail = true;
            if (SubLtZero(int.MinValue, 1) != 0) fail = true;
            if (SubLtZero(int.MaxValue, -1) != 1) fail = true;

            if (SubLeZero(5, 3) != 0) fail = true;
            if (SubLeZero(3, 3) != 1) fail = true;
            if (SubLeZero(2, 4) != 1) fail = true;
            if (SubLeZero(int.MinValue, 1) != 0) fail = true;
            if (SubLeZero(int.MaxValue, -1) != 1) fail = true;

            if (fail)
            {
                return 101;
            }
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AddGtZero(int a, int b) {
            if (a + b > 0) {
                return 1;
            }
            return 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AddGeZero(int a, int b) {
            if (a + b >= 0) {
                return 1;
            }
            return 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AddLtZero(int a, int b) {
            if (a + b < 0) {
                return 1;
            }
            return 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AddLeZero(int a, int b) {
            if (a + b <= 0) {
                return 1;
            }
            return 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int SubGtZero(int a, int b) {
            if (a - b > 0) {
                return 1;
            }
            return 0;
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        static int SubGeZero(int a, int b) {
            if (a - b >= 0) {
                return 1;
            }
            return 0;
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        static int SubLtZero(int a, int b) {
            if (a - b < 0) {
                return 1;
            }
            return 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int SubLeZero(int a, int b) {
            if (a - b <= 0) {
                return 1;
            }
            return 0;
        }
    }
}
