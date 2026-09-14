// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using TestLibrary;
using Xunit;

public class Runtime_133822
{
    private readonly byte[] _source = new byte[8192];
    private readonly byte[] _destination = new byte[8192];
    private volatile bool _started;
    private volatile bool _stop;
    private bool _equal;

    [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
    [InlineData(nameof(Copy))]
    [InlineData(nameof(Compare))]
    [InlineData(nameof(Fill))]
    [InlineData(nameof(Clear))]
    [InlineData(nameof(EmptyCopy))]
    [InlineData(nameof(LargeCopy))]
    public static void TestEntryPoint(string operation)
    {
        Runtime_133822 test = new Runtime_133822();
        ThreadStart action = operation switch
        {
            nameof(Copy) => test.Copy,
            nameof(Compare) => test.Compare,
            nameof(Fill) => test.Fill,
            nameof(Clear) => test.Clear,
            nameof(EmptyCopy) => test.EmptyCopy,
            nameof(LargeCopy) => test.LargeCopy,
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };
        Thread thread = new Thread(action) { IsBackground = true };
        thread.Start();
        try
        {
            // The worker signals from inside the loop without adding a call that could be a GC safe point.
            SpinWait.SpinUntil(() => test._started);
            GC.Collect();
        }
        finally
        {
            test._stop = true;
            thread.Join();
        }

        if (operation == nameof(Compare))
        {
            Assert.True(test._equal);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private void Copy()
    {
        while (!_stop)
        {
            _source.AsSpan(0, 32).CopyTo(_destination.AsSpan(0, 32));
            _started = true;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private void Compare()
    {
        while (!_stop)
        {
            _equal = _source.AsSpan(0, 32).SequenceEqual(_destination.AsSpan(0, 32));
            _started = true;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private void Fill()
    {
        while (!_stop)
        {
            _destination.AsSpan(0, 32).Fill(42);
            _started = true;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private void Clear()
    {
        while (!_stop)
        {
            _destination.AsSpan(0, 32).Clear();
            _started = true;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private void EmptyCopy()
    {
        while (!_stop)
        {
            _source.AsSpan(0, 0).CopyTo(_destination.AsSpan(0, 0));
            _started = true;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private void LargeCopy()
    {
        while (!_stop)
        {
            _source.AsSpan(0, 4096).CopyTo(_destination.AsSpan(0, 4096));
            _started = true;
        }
    }
}
