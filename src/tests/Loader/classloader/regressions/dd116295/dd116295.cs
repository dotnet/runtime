// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Xunit;

namespace ConsoleApplication1
{
    public class Program
    {
        public struct A
        {
        }

        public struct B
        {
            A a;
        }

        public struct C
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public B[] b;
        }

        [Fact]
        public static void TestEntryPoint()
        {
            M();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void M()
        {
            C obj = new C(); // exception occurs in the initializing this member
        }
    }
}
