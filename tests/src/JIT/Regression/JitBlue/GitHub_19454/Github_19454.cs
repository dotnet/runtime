// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// This test was extracted from the corefx System.Numerics.Vectors tests,
// and was failing with minOpts because a SIMD12 was being spilled using
// a 16-byte load, but only a 12-byte location had been allocated.

using System;

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

class GitHub_19454
{
    static int Main()
    {
        MyReader r = new MyReader();
        r.ReadBytesInner(0);
        return 100;
    }
}

