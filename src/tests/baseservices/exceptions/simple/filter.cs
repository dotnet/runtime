// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

public class FilterTests
{
    private sealed class GenericFilterHolder<T>
    {
        private readonly int _field = 42;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool InstanceFilterReadsThisField()
        {
            try
            {
                ThrowInvalidOperation();
                return false;
            }
            catch (InvalidOperationException) when (_field == 42)
            {
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static async Task<bool> AsyncFilterReadsCatchVar()
        {
            try
            {
                ThrowInvalidOperation();
                await Task.Yield();
                return false;
            }
            catch (InvalidOperationException e) when (e.Message.Length > 0)
            {
                return true;
            }
        }
    }

    private sealed class SynchronizedFilterHolder
    {
        private readonly int _field = 42;

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.Synchronized)]
        public bool SynchronizedFilterReadsThisField()
        {
            try
            {
                ThrowInvalidOperation();
                return false;
            }
            catch (InvalidOperationException) when (_field == 42)
            {
                return true;
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowInvalidOperation()
    {
        throw new InvalidOperationException("boom");
    }

    [Fact]
    public static void FilterAccessesThisAndLocals()
    {
        Assert.True(new GenericFilterHolder<string>().InstanceFilterReadsThisField());
        Assert.True(new GenericFilterHolder<int>().InstanceFilterReadsThisField());
        Assert.True(GenericFilterHolder<string>.AsyncFilterReadsCatchVar().GetAwaiter().GetResult());
        Assert.True(new SynchronizedFilterHolder().SynchronizedFilterReadsThisField());
    }
}
