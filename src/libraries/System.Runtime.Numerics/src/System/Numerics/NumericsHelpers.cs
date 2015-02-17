// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Numerics
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct DoubleUlong
    {
        [FieldOffset(0)]
        public double dbl;
        [FieldOffset(0)]
        public ulong uu;
    }


    internal static class NumericsHelpers
    {
        private const int kcbitUint = 32;

        public static void GetDoubleParts(double dbl, out int sign, out int exp, out ulong man, out bool fFinite)
        {
            Contract.Ensures(Contract.ValueAtReturn(out sign) == +1 || Contract.ValueAtReturn(out sign) == -1);

            DoubleUlong du;
            du.uu = 0;
            du.dbl = dbl;

            sign = 1 - ((int)(du.uu >> 62) & 2);
            man = du.uu & 0x000FFFFFFFFFFFFF;
            exp = (int)(du.uu >> 52) & 0x7FF;
            if (exp == 0)
            {
                // Denormalized number.
                fFinite = true;
                if (man != 0)
                    exp = -1074;
            }
            else if (exp == 0x7FF)
            {
                // NaN or Inifite.
                fFinite = false;
                exp = int.MaxValue;
            }
            else
            {
                fFinite = true;
                man |= 0x0010000000000000;
                exp -= 1075;
            }
        }

        public static double GetDoubleFromParts(int sign, int exp, ulong man)
        {
            DoubleUlong du;
            du.dbl = 0;

            if (man == 0)
                du.uu = 0;
            else
            {
                // Normalize so that 0x0010 0000 0000 0000 is the highest bit set.
                int cbitShift = CbitHighZero(man) - 11;
                if (cbitShift < 0)
                    man >>= -cbitShift;
                else
                    man <<= cbitShift;
                exp -= cbitShift;
                Contract.Assert((man & 0xFFF0000000000000) == 0x0010000000000000);

                // Move the point to just behind the leading 1: 0x001.0 0000 0000 0000
                // (52 bits) and skew the exponent (by 0x3FF == 1023).
                exp += 1075;

                if (exp >= 0x7FF)
                {
                    // Infinity.
                    du.uu = 0x7FF0000000000000;
                }
                else if (exp <= 0)
                {
                    // Denormalized.
                    exp--;
                    if (exp < -52)
                    {
                        // Underflow to zero.
                        du.uu = 0;
                    }
                    else
                    {
                        du.uu = man >> -exp;
                        Contract.Assert(du.uu != 0);
                    }
                }
                else
                {
                    // Mask off the implicit high bit.
                    du.uu = (man & 0x000FFFFFFFFFFFFF) | ((ulong)exp << 52);
                }
            }

            if (sign < 0)
                du.uu |= 0x8000000000000000;

            return du.dbl;
        }



        // Do an in-place twos complement of d and also return the result.
        // "Dangerous" because it causes a mutation and needs to be used
        // with care for immutable types
        public static uint[] DangerousMakeTwosComplement(uint[] d)
        {
            // first do complement and +1 as long as carry is needed
            int i = 0;
            uint v = 0;
            for (; i < d.Length; i++)
            {
                v = ~d[i] + 1;
                d[i] = v;
                if (v != 0) { i++; break; }
            }
            if (v != 0)
            {
                // now ones complement is sufficient
                for (; i < d.Length; i++)
                {
                    d[i] = ~d[i];
                }
            }
            else
            {
                //??? this is weird
                d = resize(d, d.Length + 1);
                d[d.Length - 1] = 1;
            }
            return d;
        }

        public static uint[] resize(uint[] v, int len)
        {
            if (v.Length == len) return v;
            uint[] ret = new uint[len];
            int n = System.Math.Min(v.Length, len);
            for (int i = 0; i < n; i++)
            {
                ret[i] = v[i];
            }
            return ret;
        }

        public static void Swap<T>(ref T a, ref T b)
        {
            T tmp = a;
            a = b;
            b = tmp;
        }

        public static uint GCD(uint u1, uint u2)
        {
            const int cvMax = 32;
            if (u1 < u2)
                goto LOther;
            LTop:
            Contract.Assert(u2 <= u1);
            if (u2 == 0)
                return u1;
            for (int cv = cvMax; ;)
            {
                u1 -= u2;
                if (u1 < u2)
                    break;
                if (--cv == 0)
                {
                    u1 %= u2;
                    break;
                }
            }
        LOther:
            Contract.Assert(u1 < u2);
            if (u1 == 0)
                return u2;
            for (int cv = cvMax; ;)
            {
                u2 -= u1;
                if (u2 < u1)
                    break;
                if (--cv == 0)
                {
                    u2 %= u1;
                    break;
                }
            }
            goto LTop;
        }

        public static ulong GCD(ulong uu1, ulong uu2)
        {
            const int cvMax = 32;
            if (uu1 < uu2)
                goto LOther;
            LTop:
            Contract.Assert(uu2 <= uu1);
            if (uu1 <= uint.MaxValue)
                goto LSmall;
            if (uu2 == 0)
                return uu1;
            for (int cv = cvMax; ;)
            {
                uu1 -= uu2;
                if (uu1 < uu2)
                    break;
                if (--cv == 0)
                {
                    uu1 %= uu2;
                    break;
                }
            }
        LOther:
            Contract.Assert(uu1 < uu2);
            if (uu2 <= uint.MaxValue)
                goto LSmall;
            if (uu1 == 0)
                return uu2;
            for (int cv = cvMax; ;)
            {
                uu2 -= uu1;
                if (uu2 < uu1)
                    break;
                if (--cv == 0)
                {
                    uu2 %= uu1;
                    break;
                }
            }
            goto LTop;

        LSmall:
            uint u1 = (uint)uu1;
            uint u2 = (uint)uu2;
            if (u1 < u2)
                goto LOtherSmall;
            LTopSmall:
            Contract.Assert(u2 <= u1);
            if (u2 == 0)
                return u1;
            for (int cv = cvMax; ;)
            {
                u1 -= u2;
                if (u1 < u2)
                    break;
                if (--cv == 0)
                {
                    u1 %= u2;
                    break;
                }
            }
        LOtherSmall:
            Contract.Assert(u1 < u2);
            if (u1 == 0)
                return u2;
            for (int cv = cvMax; ;)
            {
                u2 -= u1;
                if (u2 < u1)
                    break;
                if (--cv == 0)
                {
                    u2 %= u1;
                    break;
                }
            }
            goto LTopSmall;
        }

        public static ulong MakeUlong(uint uHi, uint uLo)
        {
            return ((ulong)uHi << kcbitUint) | uLo;
        }

        public static uint GetLo(ulong uu)
        {
            return (uint)uu;
        }

        public static uint GetHi(ulong uu)
        {
            return (uint)(uu >> kcbitUint);
        }

        public static uint Abs(int a)
        {
            uint mask = (uint)(a >> 31);
            return ((uint)a ^ mask) - mask;
        }

        //    public static ulong Abs(long a) {
        //      ulong mask = (ulong)(a >> 63);
        //      return ((ulong)a ^ mask) - mask;
        //    }

        public static uint CombineHash(uint u1, uint u2)
        {
            return ((u1 << 7) | (u1 >> 25)) ^ u2;
        }

        public static int CombineHash(int n1, int n2)
        {
            return (int)CombineHash((uint)n1, (uint)n2);
        }
        public static int CbitHighZero(uint u)
        {
            if (u == 0)
                return 32;

            int cbit = 0;
            if ((u & 0xFFFF0000) == 0)
            {
                cbit += 16;
                u <<= 16;
            }
            if ((u & 0xFF000000) == 0)
            {
                cbit += 8;
                u <<= 8;
            }
            if ((u & 0xF0000000) == 0)
            {
                cbit += 4;
                u <<= 4;
            }
            if ((u & 0xC0000000) == 0)
            {
                cbit += 2;
                u <<= 2;
            }
            if ((u & 0x80000000) == 0)
                cbit += 1;
            return cbit;
        }

        public static int CbitLowZero(uint u)
        {
            if (u == 0)
                return 32;

            int cbit = 0;
            if ((u & 0x0000FFFF) == 0)
            {
                cbit += 16;
                u >>= 16;
            }
            if ((u & 0x000000FF) == 0)
            {
                cbit += 8;
                u >>= 8;
            }
            if ((u & 0x0000000F) == 0)
            {
                cbit += 4;
                u >>= 4;
            }
            if ((u & 0x00000003) == 0)
            {
                cbit += 2;
                u >>= 2;
            }
            if ((u & 0x00000001) == 0)
                cbit += 1;
            return cbit;
        }

        public static int CbitHighZero(ulong uu)
        {
            if ((uu & 0xFFFFFFFF00000000) == 0)
                return 32 + CbitHighZero((uint)uu);
            return CbitHighZero((uint)(uu >> 32));
        }
    }
}
