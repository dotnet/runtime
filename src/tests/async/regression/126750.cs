// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

// The async version of a synchronous Task-returning method does not save and restore
// contexts, so it has no GC safe point ahead of its user code. That allowed
// optRemoveRedundantZeroInits to mark 'o' below as having an explicit init and drop its
// zero initialization from the prolog, even though the resumption path branches around
// the store. 'o' is address exposed, so it is reported to the GC as an untracked slot for
// the whole method, and a GC on the resumption path would then scan stack garbage.

public class Runtime_126750
{
    [InlineArray(512)]
    private struct Buffer
    {
        private long _element;
    }

    // The large return value is copied with a helper call in the restore path, which makes
    // GC suspension possible there.
    private struct Large
    {
        public Buffer Buffer;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        for (int depth = 0; depth < 64; depth++)
        {
            Run(depth);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Run(int depth)
    {
        TaskCompletionSource tcs = new TaskCompletionSource();
        Task<Large> task = Caller(tcs.Task);

        FillStackWithGarbage(depth);

        tcs.SetResult();
        task.GetAwaiter().GetResult();
    }

    // Leaves a non-null, non-heap value in the stack range that the resumed frame lands in.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void FillStackWithGarbage(int depth)
    {
        Span<nint> buffer = stackalloc nint[16];
        buffer.Fill(unchecked((nint)0x1122334455667788));

        if (depth > 0)
        {
            FillStackWithGarbage(depth - 1);
        }

        Consume(buffer[0]);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume(nint value)
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<Large> Caller(Task task) => await TaskReturning("hello", task);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Task<Large> TaskReturning(string s, Task task)
    {
        object o = s;
        Escape(ref o);
        return Suspend(task);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<Large> Suspend(Task task)
    {
        await task.ConfigureAwait(false);
        return default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Escape(ref object o)
    {
    }
}
