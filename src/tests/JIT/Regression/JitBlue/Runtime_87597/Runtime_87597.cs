// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_87597
{
    interface IFace
    {
        static IFace() {}
        void Method();
    }

    class GenericType<T> : IFace
    {
        static GenericType()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Method()
        {
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestL1(IFace iface)
    {
        iface.Method();
    }

    [Fact]
    public static void TestEntryPoint()
    {
        for (int i = 0; i < 100; i++)
        {
            System.Threading.Thread.Sleep(16);
            TestL1(new GenericType<string>());
        }
    }
}
