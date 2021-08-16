// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using TestLibrary;

namespace PInvokeTests
{
    class VarargsTest
    {
        [DllImport("VarargsNative", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern void TestVarArgs(StringBuilder builder, IntPtr bufferSize, string formatString, __arglist);

        [DllImport("VarargsNative", CharSet = CharSet.Unicode)]
        private static extern void TestArgIterator(StringBuilder builder, IntPtr bufferSize, string formatString, ArgIterator arguments);

        private static void TestArgIteratorWrapper(StringBuilder builder, IntPtr bufferSize, string formatString, __arglist)
        {
            TestArgIterator(builder, bufferSize, formatString, new ArgIterator(__arglist));
        }

        private static bool AssertEqual(string lhs, string rhs)
        {
            if (lhs != rhs)
            {
                Console.WriteLine($"FAIL! \"{lhs}\" != \"{rhs}\"");
                return false;
            }
            return true;
        }

        public static int Main()
        {
            var passed = true;
            int arg1 = 10;
            int arg2 = 20;
            double arg3 = 12.5;

            string expected = FormattableString.Invariant($"{arg1}, {arg2}, {arg3:F1}");

            StringBuilder builder;

            builder = new StringBuilder(30);
            TestVarArgs(builder, (IntPtr)30, "%i, %i, %.1f", __arglist(arg1, arg2, arg3));
            passed &= AssertEqual(builder.ToString(), expected);

            builder = new StringBuilder(30);
            TestArgIteratorWrapper(builder, (IntPtr)30, "%i, %i, %.1f", __arglist(arg1, arg2, arg3));
            passed &= AssertEqual(builder.ToString(), expected);

            return passed ? 100 : 101;
        }
    }
}
