// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace JitInliningTest
{
    public class DeepCall
    {
        private static long f1() { return 1; }
        private static long f2() { return f1() + 1; }
        private static long f3() { return f2() + 1; }
        private static long f4() { return f3() + 1; }
        private static long f5() { return f4() + 1; }
        private static long f6() { return f5() + 1; }
        private static long f7() { return f6() + 1; }
        private static long f8() { return f7() + 1; }
        private static long f9() { return f8() + 1; }
        private static long f10() { return f9() + 1; }
        private static long f11() { return f10() + 1; }
        private static long f12() { return f11() + 1; }
        private static long f13() { return f12() + 1; }
        private static long f14() { return f13() + 1; }
        private static long f15() { return f14() + 1; }
        private static long f16() { return f15() + 1; }
        private static long f17() { return f16() + 1; }
        private static long f18() { return f17() + 1; }
        private static long f19() { return f18() + 1; }
        private static long f20() { return f19() + 1; }
        private static long f21() { return f20() + 1; }
        private static long f22() { return f21() + 1; }
        private static long f23() { return f22() + 1; }
        private static long f24() { return f23() + 1; }
        private static long f25() { return f24() + 1; }
        private static long f26() { return f25() + 1; }
        private static long f27() { return f26() + 1; }
        private static long f28() { return f27() + 1; }
        private static long f29() { return f28() + 1; }
        private static long f30() { return f29() + 1; }
        private static long f31() { return f30() + 1; }
        private static long f32() { return f31() + 1; }
        private static long f33() { return f32() + 1; }
        private static long f34() { return f33() + 1; }
        private static long f35() { return f34() + 1; }
        private static long f36() { return f35() + 1; }
        private static long f37() { return f36() + 1; }
        private static long f38() { return f37() + 1; }
        private static long f39() { return f38() + 1; }
        private static long f40() { return f39() + 1; }
        private static long f41() { return f40() + 1; }
        private static long f42() { return f41() + 1; }
        private static long f43() { return f42() + 1; }
        private static long f44() { return f43() + 1; }
        private static long f45() { return f44() + 1; }
        private static long f46() { return f45() + 1; }
        private static long f47() { return f46() + 1; }
        private static long f48() { return f47() + 1; }
        private static long f49() { return f48() + 1; }
        private static long f50() { return f49() + 1; }
        private static long f51() { return f50() + 1; }
        private static long f52() { return f51() + 1; }
        private static long f53() { return f52() + 1; }
        private static long f54() { return f53() + 1; }
        private static long f55() { return f54() + 1; }
        private static long f56() { return f55() + 1; }
        private static long f57() { return f56() + 1; }
        private static long f58() { return f57() + 1; }
        private static long f59() { return f58() + 1; }
        private static long f60() { return f59() + 1; }
        private static long f61() { return f60() + 1; }
        private static long f62() { return f61() + 1; }
        private static long f63() { return f62() + 1; }
        private static long f64() { return f63() + 1; }
        private static long f65() { return f64() + 1; }
        private static long f66() { return f65() + 1; }
        private static long f67() { return f66() + 1; }
        private static long f68() { return f67() + 1; }
        private static long f69() { return f68() + 1; }
        private static long f70() { return f69() + 1; }
        private static long f71() { return f70() + 1; }
        private static long f72() { return f71() + 1; }
        private static long f73() { return f72() + 1; }
        private static long f74() { return f73() + 1; }
        private static long f75() { return f74() + 1; }
        private static long f76() { return f75() + 1; }
        private static long f77() { return f76() + 1; }
        private static long f78() { return f77() + 1; }
        private static long f79() { return f78() + 1; }
        private static long f80() { return f79() + 1; }
        private static long f81() { return f80() + 1; }
        private static long f82() { return f81() + 1; }
        private static long f83() { return f82() + 1; }
        private static long f84() { return f83() + 1; }
        private static long f85() { return f84() + 1; }
        private static long f86() { return f85() + 1; }
        private static long f87() { return f86() + 1; }
        private static long f88() { return f87() + 1; }
        private static long f89() { return f88() + 1; }
        private static long f90() { return f89() + 1; }
        private static long f91() { return f90() + 1; }
        private static long f92() { return f91() + 1; }
        private static long f93() { return f92() + 1; }
        private static long f94() { return f93() + 1; }
        private static long f95() { return f94() + 1; }
        private static long f96() { return f95() + 1; }
        private static long f97() { return f96() + 1; }
        private static long f98() { return f97() + 1; }
        private static long f99() { return f98() + 1; }
        private static long f100() { return f99() + 1; }
        [Fact]
        public static int TestEntryPoint()
        {
            return (int)f100();
        }
    }
}
