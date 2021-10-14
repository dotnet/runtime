// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
    public class Switch
    {
        public static void Main()
        {
            TestSwitchStatement(1);
            TestSwitchExpression(1);
        }

        [Kept]
        public static void TestSwitchStatement(int p)
        {
            switch (p)
            {
                case 0: Case1(); break;
                case 1: Case1(); break;
                case 3: Case1(); break;
                case 4: Case1(); break;
                case 5: Case1(); break;
                case 6: Case1(); break;
                case 7: Case1(); break;
                case 8: Case1(); break;
                case 9: Case1(); break;
                case 10: Case1(); break;
                default:
                    Case2();
                    break;
            }
        }

        [Kept]
        public static void Case1() { }
        [Kept]
        public static void Case2() { }

        [Kept]
        public static int TestSwitchExpression(int p) =>
            p switch
            {
                0 => 1,
                1 => 2,
                2 => 3,
                3 => 4,
                4 => 5,
                5 => 6,
                6 => 7,
                7 => 8,
                8 => 9,
                9 => 10,
                _ => 0
            };
    }
}