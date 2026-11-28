// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class DeferredInterfaceLookup
{
    private interface IChoice<T>
    {
        int Choose();
    }

    private sealed class Choice<T> : IChoice<T>
    {
        public int Choose() => typeof(T) == typeof(string) ? 17 : 19;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Dispatch<T>(IChoice<T> receiver) => receiver.Choose();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Devirtualize<T>()
    {
        IChoice<T> receiver = new Choice<T>();
        return receiver.Choose();
    }

    [Fact]
    public static void TestEntryPoint()
    {
        for (int i = 0; i < 2; i++)
        {
            Assert.Equal(17, Dispatch<string>(new Choice<string>()));
            Assert.Equal(19, Dispatch<object>(new Choice<object>()));
            Assert.Equal(17, Devirtualize<string>());
            Assert.Equal(19, Devirtualize<object>());
        }
    }
}
