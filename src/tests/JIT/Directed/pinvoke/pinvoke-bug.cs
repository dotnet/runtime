// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

// Test includes an intentional unreachable return 
#pragma warning disable 162

namespace PInvokeTest
{
    public class Test
    {
        [DllImport("msvcrt", EntryPoint = "sin", CallingConvention = CallingConvention.Cdecl)]
        private static extern double sin(double x);

        private static double g;
        private static bool b;

        [Fact]
        public static int TestEntryPoint()
        {
            bool result = false;
            g = 0.0;
            double val = 1.0;
            b = false;
            try
            {
                Func(val);
            }
            catch(Exception)
            {
                result = (Math.Abs(g - sin(val)) < 0.0001);
            }

            return (result ? 100 : -1);
        }

        // An inline pinvoke in a method with float math followed by a
        // throw may causes trouble for liveness models for the inline
        // frame var.
        static double Func(double x)
        {
            g = sin(x);

            // A bit of control flow to throw off rareness detection
            // Also we need float in here 
            if (b)
            {
                g = 0.0;
            }

            throw new Exception();

            // Deliberately unreachable return
            return g;
        }
    }
}

