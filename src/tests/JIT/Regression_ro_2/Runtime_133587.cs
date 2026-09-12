// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_133587
{
    private static Big s_src = new Big
    {
        A = new object(),
        B = new object(),
        C = new object(),
        D = new object(),
        E = new object()
    };
    private static string s_log = "";

    private struct Big
    {
        public object A, B, C, D, E;
    }

    public enum SourceKind
    {
        Valid,
        Null,
        Throwing
    }

    [Theory]
    [InlineData(false, SourceKind.Valid)]
    [InlineData(true, SourceKind.Valid)]
    [InlineData(false, SourceKind.Null)]
    [InlineData(true, SourceKind.Null)]
    [InlineData(false, SourceKind.Throwing)]
    [InlineData(true, SourceKind.Throwing)]
    public static void TestEntryPoint(bool nullDestination, SourceKind sourceKind)
    {
        s_log = "";
        Big destination = default;
        Action copy = () => Copy(ref (nullDestination ? ref Unsafe.NullRef<Big>() : ref destination), sourceKind);

        if (sourceKind is SourceKind.Throwing)
        {
            Assert.Throws<InvalidOperationException>(copy);
        }
        else if (nullDestination || sourceKind is SourceKind.Null)
        {
            Assert.Throws<NullReferenceException>(copy);
        }
        else
        {
            copy();
            Assert.Same(s_src.A, destination.A);
            Assert.Same(s_src.B, destination.B);
            Assert.Same(s_src.C, destination.C);
            Assert.Same(s_src.D, destination.D);
            Assert.Same(s_src.E, destination.E);
        }

        Assert.Equal("Index;GetSrc;", s_log);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ref Big GetSrc(SourceKind sourceKind)
    {
        s_log += nameof(GetSrc) + ";";
        if (sourceKind is SourceKind.Throwing)
        {
            throw new InvalidOperationException("Source evaluation failed.");
        }

        return ref (sourceKind is SourceKind.Null ? ref Unsafe.NullRef<Big>() : ref s_src);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Index()
    {
        s_log += nameof(Index) + ";";
        return 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void Copy(ref Big destination, SourceKind sourceKind)
    {
        Unsafe.Add(ref destination, Index()) = GetSrc(sourceKind);
    }
}
