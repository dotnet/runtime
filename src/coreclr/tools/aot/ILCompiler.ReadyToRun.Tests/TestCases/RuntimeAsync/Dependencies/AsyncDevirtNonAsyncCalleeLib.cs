// Helper for the composite runtime-async devirt regression test. Compiled WITHOUT runtime-async, so
// WriterBase's virtuals are classic methods that get an "async variant" thunk when a runtime-async
// caller in another module awaits them. ConcreteWriter is sealed and doesn't override, and Holder
// exposes it base-typed, so the caller late-devirtualizes to the inherited base method.
using System.Threading.Tasks;

public class WriterBase
{
    public virtual ValueTask CompleteValueTaskAsync() => default;

    public virtual Task CompleteTaskAsync() => Task.CompletedTask;
}

public sealed class ConcreteWriter : WriterBase
{
}

public sealed class Holder
{
    private readonly ConcreteWriter _writer = new ConcreteWriter();

    public WriterBase Writer => _writer;
}
