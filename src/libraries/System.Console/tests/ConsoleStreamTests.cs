// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class ConsoleStreamTests
{
    [Fact]
    public void WriteToOutputStream_EmptyArray()
    {
        Stream outStream = Console.OpenStandardOutput();
        outStream.Write(new byte[] { }, 0, 0);
    }

    [ConditionalFact(typeof(Helpers), nameof(Helpers.IsConsoleInSupported))]
    public void ReadAsyncRespectsCancellation()
    {
        Stream inStream = Console.OpenStandardInput();
        CancellationTokenSource cts = new CancellationTokenSource();
        cts.Cancel();

        byte[] buffer = new byte[1024];
        Task result = inStream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
        Assert.True(result.IsCanceled);

        ValueTask<int> valueTaskResult = inStream.ReadAsync(buffer.AsMemory(), cts.Token);
        Assert.True(valueTaskResult.IsCanceled);
    }

    [ConditionalFact(typeof(Helpers), nameof(Helpers.IsConsoleInSupported))]
    public void ReadAsyncHandlesInvalidParams()
    {
        Stream inStream = Console.OpenStandardInput();

        byte[] buffer = new byte[1024];
        Assert.Throws<ArgumentNullException>(() => { inStream.ReadAsync(null, 0, buffer.Length); });
        Assert.Throws<ArgumentOutOfRangeException>(() => { inStream.ReadAsync(buffer, -1, buffer.Length); });
        Assert.Throws<ArgumentOutOfRangeException>(() => { inStream.ReadAsync(buffer, 0, buffer.Length + 1); });
    }

    [Fact]
    public void WriteAsyncRespectsCancellation()
    {
        Stream outStream = Console.OpenStandardOutput();
        CancellationTokenSource cts = new CancellationTokenSource();
        cts.Cancel();

        byte[] bytes = Encoding.ASCII.GetBytes("Hi");
        Task result = outStream.WriteAsync(bytes, 0, bytes.Length, cts.Token);
        Assert.True(result.IsCanceled);

        ValueTask valueTaskResult = outStream.WriteAsync(bytes.AsMemory(), cts.Token);
        Assert.True(valueTaskResult.IsCanceled);
    }

    [Fact]
    public void WriteAsyncHandlesInvalidParams()
    {
        Stream outStream = Console.OpenStandardOutput();

        byte[] bytes = Encoding.ASCII.GetBytes("Hi");
        Assert.Throws<ArgumentNullException>(() => { outStream.WriteAsync(null, 0, bytes.Length); });
        Assert.Throws<ArgumentOutOfRangeException>(() => { outStream.WriteAsync(bytes, -1, bytes.Length); });
        Assert.Throws<ArgumentOutOfRangeException>(() => { outStream.WriteAsync(bytes, 0, bytes.Length + 1); });
    }

    [ConditionalFact(typeof(Helpers), nameof(Helpers.IsConsoleInSupported))]
    public void InputCannotWriteAsync()
    {
        Stream inStream = Console.OpenStandardInput();

        byte[] bytes = Encoding.ASCII.GetBytes("Hi");
        Assert.Throws<NotSupportedException>(() => { inStream.WriteAsync(bytes, 0, bytes.Length); });

        Assert.Throws<NotSupportedException>(() => { inStream.WriteAsync(bytes.AsMemory()); });
    }

    [Fact]
    public void OutputCannotReadAsync()
    {
        Stream outStream = Console.OpenStandardOutput();

        byte[] buffer = new byte[1024];
        Assert.Throws<NotSupportedException>(() =>
        {
            outStream.ReadAsync(buffer, 0, buffer.Length);
        });

        Assert.Throws<NotSupportedException>(() =>
        {
            outStream.ReadAsync(buffer.AsMemory());
        });
    }
}
