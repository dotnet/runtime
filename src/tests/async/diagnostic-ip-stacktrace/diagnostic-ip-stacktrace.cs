// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

public class RuntimeAsyncDiagnosticIPStackTrace
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public static void SynchronousExceptionDoesNotDuplicateCaller(bool isValueTask, bool isGeneric)
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => InvokeSynchronouslyThrowingAsync(isValueTask, isGeneric));
        string stackTrace = exception.StackTrace;
        Assert.NotNull(stackTrace);

        string callerFrame = $"{nameof(RuntimeAsyncDiagnosticIPStackTrace)}.{nameof(InvokeSynchronouslyThrowingAsync)}(";
        int firstFrame = stackTrace.IndexOf(callerFrame, StringComparison.Ordinal);
        Assert.True(firstFrame >= 0, stackTrace);
        Assert.Equal(-1, stackTrace.IndexOf(callerFrame, firstFrame + callerFrame.Length, StringComparison.Ordinal));
        Assert.DoesNotContain("--- End of stack trace from previous location ---", stackTrace);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    [RuntimeAsyncMethodGeneration(false)]
    private static void InvokeSynchronouslyThrowingAsync(bool isValueTask, bool isGeneric)
    {
        if (isValueTask)
        {
            if (isGeneric)
            {
                _ = ThrowValueTaskOfInt().GetAwaiter().GetResult();
            }
            else
            {
                ThrowValueTask().GetAwaiter().GetResult();
            }
        }
        else
        {
            if (isGeneric)
            {
                _ = ThrowTaskOfInt().GetAwaiter().GetResult();
            }
            else
            {
                ThrowTask().GetAwaiter().GetResult();
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task ThrowTask() => throw new InvalidOperationException();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task<int> ThrowTaskOfInt() => throw new InvalidOperationException();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async ValueTask ThrowValueTask() => throw new InvalidOperationException();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async ValueTask<int> ThrowValueTaskOfInt() => throw new InvalidOperationException();

    [Fact]
    public static async Task TestEntryPoint()
    {
        // Exercise both the cold and warm ResumeInterpreterContinuation FCall paths.
        for (int i = 0; i < 2; i++)
        {
            TaskCompletionSource suspension = new();
            Task task = Level1(suspension.Task);

            Assert.False(task.IsCompleted);
            suspension.SetResult();
            Assert.True(task.IsCompleted);

            InvalidOperationException? exception = null;
            try
            {
                await task;
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
                nameof(NoInlineForwarder),
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
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task Level1(Task suspension) => await Level2(suspension);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task Level2(Task suspension) => await NoInlineForwarder(suspension);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Task NoInlineForwarder(Task suspension) => Level3(suspension);

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
