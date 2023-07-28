// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using Xunit;

namespace Test
{

    public class C2
    {
        [Fact]
        public static int TestEntryPoint()
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
