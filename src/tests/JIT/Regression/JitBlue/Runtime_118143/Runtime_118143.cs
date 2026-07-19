// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Text;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

public class Runtime_118143
{
    [Fact]
    public static void TestEntryPoint()
    {
        for (int i = 0; i < 300; i++)
        {
            RunBase64Test();
            Thread.Sleep(1);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RunBase64Test()
    {
        byte[] input = new byte[64];
        byte[] output = new byte[Base64.GetMaxEncodedToUtf8Length(input.Length)];
        byte[] expected = Convert.FromHexString(
            "5957466859574668595746685957466859574668595746685957466859574668595746685957466859574668" +
            "5957466859574668595746685957466859574668595746685957466859574668595746685957466859513D3D");
        input.AsSpan().Fill((byte)'a');
        Base64.EncodeToUtf8(input, output, out _, out _);
        if (!output.SequenceEqual(expected))
            throw new InvalidOperationException("Invalid Base64 output");
    }
}
