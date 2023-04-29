// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;
namespace VirtFunc
{
    public class CTest : ITest1, ITest2, ITest3, ITest4, ITest5, ITest6, ITest7, ITest8, ITest9, ITest10
    {
        public int f1a() { return 1; }
        public int f1b(int a) { return 1 + a; }
        public decimal f1c() { return 1; }
        public string f1d() { return "1"; }
        public int f2a() { return 2; }
        public int f2b(int a) { return 2 + a; }
        public decimal f2c() { return 2; }
        public string f2d() { return "2"; }
        public int f3a() { return 3; }
        public int f3b(int a) { return 3 + a; }
        public decimal f3c() { return 3; }
        public string f3d() { return "3"; }
        public int f4a() { return 4; }
        public int f4b(int a) { return 4 + a; }
        public decimal f4c() { return 4; }
        public string f4d() { return "4"; }
        public int f5a() { return 5; }
        public int f5b(int a) { return 5 + a; }
        public decimal f5c() { return 5; }
        public string f5d() { return "5"; }
        public int f6a() { return 6; }
        public int f6b(int a) { return 6 + a; }
        public decimal f6c() { return 6; }
        public string f6d() { return "6"; }
        public int f7a() { return 7; }
        public int f7b(int a) { return 7 + a; }
        public decimal f7c() { return 7; }
        public string f7d() { return "7"; }
        public int f8a() { return 8; }
        public int f8b(int a) { return 8 + a; }
        public decimal f8c() { return 8; }
        public string f8d() { return "8"; }
        public int f9a() { return 9; }
        public int f9b(int a) { return 9 + a; }
        public decimal f9c() { return 9; }
        public string f9d() { return "9"; }
        public int f10a() { return 10; }
        public int f10b(int a) { return 10 + a; }
        public decimal f10c() { return 10; }
        public string f10d() { return "10"; }
        [Fact]
        public static int TestEntryPoint()
        {
            CTest c = new CTest();

            if (c.f1a() != 1) return 1;
            if (c.f1b(0) != 1) return 1;
            if (c.f1c() != 1) return 1;
            if (c.f1d() != "1") return 1;
            if (c.f2a() != 2) return 2;
            if (c.f2b(0) != 2) return 2;
            if (c.f2c() != 2) return 2;
            if (c.f2d() != "2") return 2;
            if (c.f3a() != 3) return 3;
            if (c.f3b(0) != 3) return 3;
            if (c.f3c() != 3) return 3;
            if (c.f3d() != "3") return 3;
            if (c.f4a() != 4) return 4;
            if (c.f4b(0) != 4) return 4;
            if (c.f4c() != 4) return 4;
            if (c.f4d() != "4") return 4;
            if (c.f5a() != 5) return 5;
            if (c.f5b(0) != 5) return 5;
            if (c.f5c() != 5) return 5;
            if (c.f5d() != "5") return 5;
            if (c.f6a() != 6) return 6;
            if (c.f6b(0) != 6) return 6;
            if (c.f6c() != 6) return 6;
            if (c.f6d() != "6") return 6;
            if (c.f7a() != 7) return 7;
            if (c.f7b(0) != 7) return 7;
            if (c.f7c() != 7) return 7;
            if (c.f7d() != "7") return 7;
            if (c.f8a() != 8) return 8;
            if (c.f8b(0) != 8) return 8;
            if (c.f8c() != 8) return 8;
            if (c.f8d() != "8") return 8;
            if (c.f9a() != 9) return 9;
            if (c.f9b(0) != 9) return 9;
            if (c.f9c() != 9) return 9;
            if (c.f9d() != "9") return 9;
            if (c.f10a() != 10) return 10;
            if (c.f10b(0) != 10) return 10;
            if (c.f10c() != 10) return 10;
            if (c.f10d() != "10") return 10;

            Console.WriteLine("PASSED");
            return 100;
        }
    }
}
// csc /r:itest1.dll,itest2.dll,itest3.dll,itest4.dll,itest5.dll,itest6.dll,itest7.dll,itest8.dll,itest9.dll,itest10.dll ctest.cs
