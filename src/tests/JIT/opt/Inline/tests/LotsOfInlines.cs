// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Thanks to Alexander Speshilov (spechuric @ github).

using System;
using Xunit;

namespace Repro
{
    public class Program
    {
        [Fact]
        public static int TestEntryPoint()
        {
            Tst a = new Tst();
            a.f30();
            long expected = 1 << 30;
            Console.WriteLine("result = {0} expected = {1}", a.i, expected);
            return (a.i == expected ? 100 : -1);
        }
    }

    public class Tst
    {
        public long i = 0;

        // NOTE: this method executes 2^N times
        public void f00() { i++; }

        public void f01() { f00(); f00(); }
        public void f02() { f01(); f01(); }
        public void f03() { f02(); f02(); }
        public void f04() { f03(); f03(); }
        public void f05() { f04(); f04(); }
        public void f06() { f05(); f05(); }
        public void f07() { f06(); f06(); }
        public void f08() { f07(); f07(); }
        public void f09() { f08(); f08(); }
        public void f10() { f09(); f09(); }

        public void f11() { f10(); f10(); }
        public void f12() { f11(); f11(); }
        public void f13() { f12(); f12(); }
        public void f14() { f13(); f13(); }
        public void f15() { f14(); f14(); }
        public void f16() { f15(); f15(); }
        public void f17() { f16(); f16(); }
        public void f18() { f17(); f17(); }
        public void f19() { f18(); f18(); }
        public void f20() { f19(); f19(); }

        public void f21() { f20(); f20(); }
        public void f22() { f21(); f21(); }
        public void f23() { f22(); f22(); }
        public void f24() { f23(); f23(); }
        public void f25() { f24(); f24(); }
        public void f26() { f25(); f25(); }
        public void f27() { f26(); f26(); }
        public void f28() { f27(); f27(); }
        public void f29() { f28(); f28(); }
        public void f30() { f29(); f29(); }
    }
}
