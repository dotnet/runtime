// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace GitHub_24846;

using System;
using System.Runtime.CompilerServices;
using Xunit;

unsafe public class GitHub_24846
{
    internal static void TestCopy(byte* destination, byte* source)
    {
        Unsafe.CopyBlockUnaligned(destination, source, 0);
    }

    internal static void TestInit(byte* destination)
    {
        Unsafe.InitBlockUnaligned(destination, 0xff, 0);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/54392", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsMonoInterpreter)), ActiveIssue("https://github.com/dotnet/runtime/issues/54392", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsAppleMobile))]
    public static int TestEntryPoint()
    {
        int returnVal = 100;
        var destination = new byte[1];
        var source = new byte[1];
        fixed (byte* destPtr = &destination[0], srcPtr = &source[0])
        {
            try
            {
                TestCopy(destPtr, srcPtr);
                TestInit(destPtr);
            }
            catch (Exception e)
            {
                Console.WriteLine("FAILED: " + e.Message);
                returnVal = -1;
            }
            try
            {
                TestCopy(destPtr, null);
            }
            catch (Exception e)
            {
                Console.WriteLine("FAILED: " + e.Message);
                returnVal = -1;
            }
            try
            {
                TestCopy(null, srcPtr);
                TestInit(null);
            }
            catch (Exception e)
            {
                Console.WriteLine("FAILED: " + e.Message);
                returnVal = -1;
            }
        }
        return returnVal;
    }
}

