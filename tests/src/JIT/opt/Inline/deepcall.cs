// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
namespace JitInliningTest
{
    class DeepCall
    {
        static long f1() { return 1; }
        static long f2() { return f1() + 1; }
        static long f3() { return f2() + 1; }
        static long f4() { return f3() + 1; }
        static long f5() { return f4() + 1; }
        static long f6() { return f5() + 1; }
        static long f7() { return f6() + 1; }
        static long f8() { return f7() + 1; }
        static long f9() { return f8() + 1; }
        static long f10() { return f9() + 1; }
        static long f11() { return f10() + 1; }
        static long f12() { return f11() + 1; }
        static long f13() { return f12() + 1; }
        static long f14() { return f13() + 1; }
        static long f15() { return f14() + 1; }
        static long f16() { return f15() + 1; }
        static long f17() { return f16() + 1; }
        static long f18() { return f17() + 1; }
        static long f19() { return f18() + 1; }
        static long f20() { return f19() + 1; }
        static long f21() { return f20() + 1; }
        static long f22() { return f21() + 1; }
        static long f23() { return f22() + 1; }
        static long f24() { return f23() + 1; }
        static long f25() { return f24() + 1; }
        static long f26() { return f25() + 1; }
        static long f27() { return f26() + 1; }
        static long f28() { return f27() + 1; }
        static long f29() { return f28() + 1; }
        static long f30() { return f29() + 1; }
        static long f31() { return f30() + 1; }
        static long f32() { return f31() + 1; }
        static long f33() { return f32() + 1; }
        static long f34() { return f33() + 1; }
        static long f35() { return f34() + 1; }
        static long f36() { return f35() + 1; }
        static long f37() { return f36() + 1; }
        static long f38() { return f37() + 1; }
        static long f39() { return f38() + 1; }
        static long f40() { return f39() + 1; }
        static long f41() { return f40() + 1; }
        static long f42() { return f41() + 1; }
        static long f43() { return f42() + 1; }
        static long f44() { return f43() + 1; }
        static long f45() { return f44() + 1; }
        static long f46() { return f45() + 1; }
        static long f47() { return f46() + 1; }
        static long f48() { return f47() + 1; }
        static long f49() { return f48() + 1; }
        static long f50() { return f49() + 1; }
        static long f51() { return f50() + 1; }
        static long f52() { return f51() + 1; }
        static long f53() { return f52() + 1; }
        static long f54() { return f53() + 1; }
        static long f55() { return f54() + 1; }
        static long f56() { return f55() + 1; }
        static long f57() { return f56() + 1; }
        static long f58() { return f57() + 1; }
        static long f59() { return f58() + 1; }
        static long f60() { return f59() + 1; }
        static long f61() { return f60() + 1; }
        static long f62() { return f61() + 1; }
        static long f63() { return f62() + 1; }
        static long f64() { return f63() + 1; }
        static long f65() { return f64() + 1; }
        static long f66() { return f65() + 1; }
        static long f67() { return f66() + 1; }
        static long f68() { return f67() + 1; }
        static long f69() { return f68() + 1; }
        static long f70() { return f69() + 1; }
        static long f71() { return f70() + 1; }
        static long f72() { return f71() + 1; }
        static long f73() { return f72() + 1; }
        static long f74() { return f73() + 1; }
        static long f75() { return f74() + 1; }
        static long f76() { return f75() + 1; }
        static long f77() { return f76() + 1; }
        static long f78() { return f77() + 1; }
        static long f79() { return f78() + 1; }
        static long f80() { return f79() + 1; }
        static long f81() { return f80() + 1; }
        static long f82() { return f81() + 1; }
        static long f83() { return f82() + 1; }
        static long f84() { return f83() + 1; }
        static long f85() { return f84() + 1; }
        static long f86() { return f85() + 1; }
        static long f87() { return f86() + 1; }
        static long f88() { return f87() + 1; }
        static long f89() { return f88() + 1; }
        static long f90() { return f89() + 1; }
        static long f91() { return f90() + 1; }
        static long f92() { return f91() + 1; }
        static long f93() { return f92() + 1; }
        static long f94() { return f93() + 1; }
        static long f95() { return f94() + 1; }
        static long f96() { return f95() + 1; }
        static long f97() { return f96() + 1; }
        static long f98() { return f97() + 1; }
        static long f99() { return f98() + 1; }
        static long f100() { return f99() + 1; }
        public static int Main()
        {
            return (int)f100();
        }
    }
}
