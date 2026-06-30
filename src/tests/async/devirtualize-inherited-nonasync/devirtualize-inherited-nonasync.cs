// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

// A runtime-async caller awaits a devirtualized call to a NON-runtime-async virtual method that a
// sealed receiver inherits without overriding. Resolving the callee's synthesized async-variant thunk
// must unwrap it to the underlying method instead of indexing the thunk's small token table with the
// callee's real methoddef token; otherwise composite ReadyToRun compilation corrupts token resolution
// and crossgen2 aborts.
public class Async2DevirtualizeInheritedNonAsync
{
    public class WriterBase
    {
        // Non-runtime-async virtuals with bodies: awaited from a runtime-async caller via a thunk.
        [RuntimeAsyncMethodGeneration(false)]
        public virtual ValueTask CompleteValueTaskAsync() => default;

        [RuntimeAsyncMethodGeneration(false)]
        public virtual Task CompleteTaskAsync() => Task.CompletedTask;
    }

    // Sealed and does NOT override: late devirtualization resolves to the inherited base methods.
    public sealed class ConcreteWriter : WriterBase
    {
    }

    public sealed class Holder
    {
        private readonly ConcreteWriter _writer = new ConcreteWriter();

        // Base-typed accessor over the sealed concrete type, so the exact receiver is only known via
        // late devirtualization.
        public WriterBase Writer => _writer;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task AwaitInheritedValueTask(Holder h)
    {
        await h.Writer.CompleteValueTaskAsync();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task AwaitInheritedTask(Holder h)
    {
        await h.Writer.CompleteTaskAsync();
    }

    [Fact]
    public static void TestEntryPoint()
    {
        var h = new Holder();
        AwaitInheritedValueTask(h).GetAwaiter().GetResult();
        AwaitInheritedTask(h).GetAwaiter().GetResult();
    }
}
