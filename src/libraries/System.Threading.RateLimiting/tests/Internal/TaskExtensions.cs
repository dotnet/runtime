// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace System.Threading.RateLimiting.Tests.Internal
{
    public static class TaskExtensions
    {
#if DEBUG
        // Shorter duration when running tests with debug.
        // Less time waiting for hanging unit tests to fail locally.
        public const int DefaultTimeoutDuration = 5 * 1000;
#else
        public const int DefaultTimeoutDuration = 30 * 1000;
#endif

        public static TimeSpan DefaultTimeoutTimeSpan { get; } = TimeSpan.FromMilliseconds(DefaultTimeoutDuration);

        public static Task DefaultTimeout(this Task task, int milliseconds = DefaultTimeoutDuration, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = default)
        {
            return task.TimeoutAfter(TimeSpan.FromMilliseconds(milliseconds), filePath, lineNumber);
        }

        public static Task DefaultTimeout(this Task task, TimeSpan timeout, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = default)
        {
            return task.TimeoutAfter(timeout, filePath, lineNumber);
        }

        public static Task DefaultTimeout(this ValueTask task, int milliseconds = DefaultTimeoutDuration, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = default)
        {
            return task.AsTask().TimeoutAfter(TimeSpan.FromMilliseconds(milliseconds), filePath, lineNumber);
        }

        public static Task DefaultTimeout(this ValueTask task, TimeSpan timeout, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = default)
        {
            return task.AsTask().TimeoutAfter(timeout, filePath, lineNumber);
        }

        public static Task<T> DefaultTimeout<T>(this Task<T> task, int milliseconds = DefaultTimeoutDuration, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = default)
        {
            return task.TimeoutAfter(TimeSpan.FromMilliseconds(milliseconds), filePath, lineNumber);
        }

        public static Task<T> DefaultTimeout<T>(this Task<T> task, TimeSpan timeout, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = default)
        {
            return task.TimeoutAfter(timeout, filePath, lineNumber);
        }

        public static Task<T> DefaultTimeout<T>(this ValueTask<T> task, int milliseconds = DefaultTimeoutDuration, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = default)
        {
            return task.AsTask().TimeoutAfter(TimeSpan.FromMilliseconds(milliseconds), filePath, lineNumber);
        }

        public static Task<T> DefaultTimeout<T>(this ValueTask<T> task, TimeSpan timeout, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = default)
        {
            return task.AsTask().TimeoutAfter(timeout, filePath, lineNumber);
        }

        public static async Task<T> TimeoutAfter<T>(this Task<T> task, TimeSpan timeout,
            [CallerFilePath] string? filePath = null,
            [CallerLineNumber] int lineNumber = default)
        {
            // Don't create a timer if the task is already completed
            // or the debugger is attached
            if (task.IsCompleted || Debugger.IsAttached)
            {
                return await task;
            }
#if NET6_0_OR_GREATER
            try
            {
                return await task.WaitAsync(timeout);
            }
            catch (TimeoutException ex) when (ex.Source == typeof(TaskExtensions).Namespace)
            {
                throw new TimeoutException(CreateMessage(timeout, filePath, lineNumber));
            }
#else
            var cts = new CancellationTokenSource();
            if (task == await Task.WhenAny(task, Task.Delay(timeout, cts.Token)))
            {
                cts.Cancel();
                return await task;
            }
            else
            {
                throw new TimeoutException(CreateMessage(timeout, filePath, lineNumber));
            }
#endif
        }

        public static async Task TimeoutAfter(this Task task, TimeSpan timeout,
            [CallerFilePath] string? filePath = null,
            [CallerLineNumber] int lineNumber = default)
        {
            // Don't create a timer if the task is already completed
            // or the debugger is attached
            if (task.IsCompleted || Debugger.IsAttached)
            {
                await task;
                return;
            }
#if NET6_0_OR_GREATER
            try
            {
                await task.WaitAsync(timeout);
            }
            catch (TimeoutException ex) when (ex.Source == typeof(TaskExtensions).Namespace)
            {
                throw new TimeoutException(CreateMessage(timeout, filePath, lineNumber));
            }
#else
            var cts = new CancellationTokenSource();
            if (task == await Task.WhenAny(task, Task.Delay(timeout, cts.Token)))
            {
                cts.Cancel();
                await task;
            }
            else
            {
                throw new TimeoutException(CreateMessage(timeout, filePath, lineNumber));
            }
#endif
        }

        private static string CreateMessage(TimeSpan timeout, string? filePath, int lineNumber)
            => string.IsNullOrEmpty(filePath)
            ? $"The operation timed out after reaching the limit of {timeout.TotalMilliseconds}ms."
            : $"The operation at {filePath}:{lineNumber} timed out after reaching the limit of {timeout.TotalMilliseconds}ms.";
    }
}
