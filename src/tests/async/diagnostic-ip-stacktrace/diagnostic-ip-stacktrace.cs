// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

public class RuntimeAsyncDiagnosticIPStackTrace
{
    [Fact]
    public static void TestEntryPoint()
    {
        TaskCompletionSource suspension = new();
        Task task = Level1(suspension.Task);

        Assert.False(task.IsCompleted);
        suspension.SetResult();
        Assert.True(task.IsCompleted);

        InvalidOperationException? exception = null;
        try
        {
            task.GetAwaiter().GetResult();
        }
        catch (InvalidOperationException ex)
        {
            exception = ex;
        }

        Assert.NotNull(exception);
        string stackTrace = exception.StackTrace;
        Assert.NotNull(stackTrace);
        Console.WriteLine(stackTrace);

        string[] expectedMethods =
        [
            nameof(ThrowAfterResume),
            nameof(Level4),
            nameof(Level3),
            nameof(Level2),
            nameof(Level1),
        ];

        int previousFrame = -1;
        foreach (string method in expectedMethods)
        {
            string frame = $"{nameof(RuntimeAsyncDiagnosticIPStackTrace)}.{method}(";
            int currentFrame = stackTrace.IndexOf(frame, StringComparison.Ordinal);
            Assert.True(currentFrame > previousFrame, $"Expected '{frame}' after offset {previousFrame} in:{Environment.NewLine}{stackTrace}");
            previousFrame = currentFrame;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task Level1(Task suspension) => await Level2(suspension);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task Level2(Task suspension) => await Level3(suspension);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task Level3(Task suspension) => await Level4(suspension);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task Level4(Task suspension) => await ThrowAfterResume(suspension);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task ThrowAfterResume(Task suspension)
    {
        await suspension;
        throw new InvalidOperationException();
    }
}
