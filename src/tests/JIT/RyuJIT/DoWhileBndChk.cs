// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Ensure bounds checks aren't elided for this do-while loop.
//
// Reference: TF Bug 150041

using TestLibrary;
#pragma warning disable SYSLIB0032 // HandleProcessCorruptedStateExceptionsAttribute is obsolete

using System;
using System.Runtime.ExceptionServices;
using Xunit;

public class Program
{
    [HandleProcessCorruptedStateExceptions]
    [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
    [Fact]
    public static int TestEntryPoint()
    {
        int ret = 99;

        try
        {
            int[] a = new int[0];
            int i = 0x1FFFFFFF;
            do
            {
                a[i] = 0;
                ++i;
            }
            while (i < a.Length);
        }
        catch (Exception e)
        {
            if (e is IndexOutOfRangeException)
            {
                ret = 100;
            }
        }

        return ret;
    }
}

