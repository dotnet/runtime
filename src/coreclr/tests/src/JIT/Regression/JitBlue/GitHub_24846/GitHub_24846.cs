// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

unsafe public class GitHub_24846
{
    public static void TestCopy(byte* destination, byte* source)
    {
        Unsafe.CopyBlockUnaligned(destination, source, 0);
    }

    public static void TestInit(byte* destination)
    {
        Unsafe.InitBlockUnaligned(destination, 0xff, 0);
    }

    public static int Main(string[] args)
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

