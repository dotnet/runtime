// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace PrimitiveVT
{

    public struct VT1A
    {
        public int m;
    }

    public struct VT1B
    {
        public int m;
        public VT1B(int x) { m = x; }
        public static implicit operator int (VT1B x) { return x.m; }
        public static implicit operator VT1B(int x) { VT1B y; y.m = x; return y; }
        public static explicit operator VT1A(VT1B x) { VT1A y; y.m = x.m; return y; }
        public static explicit operator VT1B(VT1A x) { VT1B y; y.m = x.m; return y; }
        public static int operator +(VT1B x, VT1B y) { return x.m + y.m; }
        public static VT1B operator +(VT1A x, VT1B y) { VT1B z; z.m = x.m + y.m; return z; }
        public static int operator -(VT1B x, VT1B y) { return x.m - y.m; }
        public static VT1B operator -(VT1A x, VT1B y) { VT1B z; z.m = x.m - y.m; return z; }
        public static int operator *(VT1B x, VT1A y) { return x.m * y.m; }
        public static int operator *(VT1B x, VT1B y) { return x.m * y.m; }
        public static VT1B operator *(VT1A x, VT1B y) { VT1B z; z.m = x.m * y.m; return z; }
        public static float operator /(VT1A x, VT1B y) { return x.m / y.m; }
        public static VT1B operator ++(VT1B x) { return ++x.m; }
    }

    public struct VT2A
    {
        public uint m;
    }

    public struct VT2B
    {
        public uint m;
        public VT2B(uint x) { m = x; }
        public static implicit operator uint (VT2B x) { return x.m; }
        public static implicit operator VT2B(uint x) { VT2B y; y.m = x; return y; }
        public static explicit operator VT2A(VT2B x) { VT2A y; y.m = x.m; return y; }
        public static explicit operator VT2B(VT2A x) { VT2B y; y.m = x.m; return y; }
        public static uint operator +(VT2B x, VT2B y) { return x.m + y.m; }
        public static VT2B operator +(VT2A x, VT2B y) { VT2B z; z.m = x.m + y.m; return z; }
        public static uint operator -(VT2B x, VT2B y) { return x.m - y.m; }
        public static VT2B operator -(VT2A x, VT2B y) { VT2B z; z.m = x.m - y.m; return z; }
        public static uint operator *(VT2B x, VT2A y) { return x.m * y.m; }
        public static uint operator *(VT2B x, VT2B y) { return x.m * y.m; }
        public static VT2B operator *(VT2A x, VT2B y) { VT2B z; z.m = x.m * y.m; return z; }
        public static float operator /(VT2A x, VT2B y) { return x.m / y.m; }
        public static VT2B operator --(VT2B x) { return --x.m; }
        public static VT2B operator ++(VT2B x) { return x.m + 1; }
    }
}
