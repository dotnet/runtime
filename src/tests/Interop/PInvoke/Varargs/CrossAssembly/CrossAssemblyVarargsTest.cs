// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using TestLibrary;
using VarArgsPInvokeLib;
using Xunit;

namespace PInvokeTests
{
    public class CrossAssemblyVarargsTest
    {
        [ConditionalFact(typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsVarArgSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
        public static void TestCrossAssemblyVarArgs()
        {
            int arg1 = 10;
            int arg2 = 20;
            double arg3 = 12.5;
            string expected = FormattableString.Invariant($"{arg1}, {arg2}, {arg3:F1}");

            var builder = new StringBuilder(30);
            VarArgsWrapper.TestVarArgs(builder, (IntPtr)30, "%i, %i, %.1f", __arglist(arg1, arg2, arg3));
            Assert.Equal(expected, builder.ToString());
        }
    }
}
