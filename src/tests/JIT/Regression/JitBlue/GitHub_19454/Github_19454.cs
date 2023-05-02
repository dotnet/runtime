// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// GitHub19454: a zero length span was tripping up the jit when trying
// to analyze a bounds check.

using System;
using Xunit;

public struct MyStruct
{
    public Span<byte> Span1
    {
        get { return Span<byte>.Empty; }
    }
}

public struct MyReader
{
    public void ReadBytesInner(int batch)
    {
        MyStruct value = new MyStruct();
        for (int i = 0; i < batch; i++)
        {
            value.Span1[i] = 0;
        }
    }
}

public class GitHub_19454
{
    [Fact]
    public static int TestEntryPoint()
    {
        MyReader r = new MyReader();
        r.ReadBytesInner(0);
        return 100;
    }
}

