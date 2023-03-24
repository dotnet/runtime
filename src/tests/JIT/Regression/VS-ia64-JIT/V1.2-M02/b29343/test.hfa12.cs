// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

namespace JitTest.HFA
{

    public class TestCase
    {

        public struct HFA12
        {
            public float f1, f2, f3, f4, f5, f6, f7, f8, f9, f10, f11, f12;//, f13, f14, f15;
        }

        internal static void Init(out HFA12 hfa)
        {
            hfa.f1 = 1;
            hfa.f2 = 2;
            hfa.f3 = 3;
            hfa.f4 = 4;
            hfa.f5 = 5;
            hfa.f6 = 6;
            hfa.f7 = 7;
            hfa.f8 = 8;
            hfa.f9 = 9;
            hfa.f10 = 10;
            hfa.f11 = 11;
            hfa.f12 = 12;
        }


        internal static void Print0(HFA12 hfa)
        {
            System.Console.WriteLine(" -- Print0");
            System.Console.WriteLine("f1 = {0}", hfa.f1);
            System.Console.WriteLine("f2 = {0}", hfa.f2);
            System.Console.WriteLine("f3 = {0}", hfa.f3);
            System.Console.WriteLine("f4 = {0}", hfa.f4);
            System.Console.WriteLine("f5 = {0}", hfa.f5);
            System.Console.WriteLine("f6 = {0}", hfa.f6);
            System.Console.WriteLine("f7 = {0}", hfa.f7);
            System.Console.WriteLine("f8 = {0}", hfa.f8);
            System.Console.WriteLine("f9 = {0}", hfa.f9);
            System.Console.WriteLine("f10 = {0}", hfa.f10);
            System.Console.WriteLine("f11 = {0}", hfa.f11);
            System.Console.WriteLine("f12 = {0}", hfa.f12);
        }

        internal static void Print1(float v1, HFA12 hfa)
        {
            System.Console.WriteLine(" -- Print1");
            System.Console.WriteLine("f1 = {0}", hfa.f1);
            System.Console.WriteLine("f2 = {0}", hfa.f2);
            System.Console.WriteLine("f3 = {0}", hfa.f3);
            System.Console.WriteLine("f4 = {0}", hfa.f4);
            System.Console.WriteLine("f5 = {0}", hfa.f5);
            System.Console.WriteLine("f6 = {0}", hfa.f6);
            System.Console.WriteLine("f7 = {0}", hfa.f7);
            System.Console.WriteLine("f8 = {0}", hfa.f8);
            System.Console.WriteLine("f9 = {0}", hfa.f9);
            System.Console.WriteLine("f10 = {0}", hfa.f10);
            System.Console.WriteLine("f11 = {0}", hfa.f11);
            System.Console.WriteLine("f12 = {0}", hfa.f12);
        }


        [Fact]
        public static int TestEntryPoint()
        {

            HFA12 hfa11;
            Init(out hfa11);
            Print0(hfa11);
            Print1(11, hfa11);

            return 100;
        }
    }
}
