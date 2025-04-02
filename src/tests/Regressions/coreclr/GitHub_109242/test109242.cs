// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Reflection;
using Xunit;

public class Test109242
{
    [Fact]
    public static void TestEntryPoint()
    {
        unsafe
        {
            void* p = stackalloc byte[Random.Shared.Next(100)];
            GC.KeepAlive(((IntPtr)p).ToString());
        }

        Assembly.Load("System.Runtime");
    }
}

