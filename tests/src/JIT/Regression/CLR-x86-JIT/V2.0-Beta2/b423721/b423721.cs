// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

namespace Test
{

    public class C2
    {
        public static int Main(string[] args)
        {
            int ret = 100;

            if (C1Helper.IsFoo(C1<string>.GetString()))
            {
                Console.WriteLine("PASS: C1<string> handles intra-assembly interning");
            }
            else
            {
                Console.WriteLine("FAIL: C1<string> does NOT handles intra-assembly interning");
                ret = 101;
            }

            if (C1Helper.IsFoo(C1<int>.GetString()))
            {
                Console.WriteLine("PASS: C1<int> handles intra-assembly interning");
            }
            else
            {
                Console.WriteLine("FAIL: C1<int> does NOT handles intra-assembly interning");
                ret = 101;
            }

            Type t = Type.GetType("Test.C1`1[[System.Int64, mscorlib, Version=0.0.0.0, Culture=neutral ]], c1, Version=0.0.0.0, Culture=neutral");
            if (t == null)
            {
                Console.WriteLine("FAIL: Could not get Type C1`1[[System.Int64, mscorlib, Version=0.0.0.0, Culture=neutral ]], c1, Version=0.0.0.0, Culture=neutral");
                return 101;
            }

            Console.WriteLine("Test SUCCESS");

            return ret;
        }
    }

}