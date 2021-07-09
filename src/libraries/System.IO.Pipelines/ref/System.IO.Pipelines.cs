// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.IO.Pipelines
{
    public partial struct FlushResult
    {
        private int _dummyPrimitive;
        public FlushResult(bool isCanceled, bool isCompleted) { throw null; }
        public bool IsCanceled { get { throw null; } }
        public bool IsCompleted { get { throw null; } }
    }
    public partial interface IDuplexPipe
    {
        System.IO.Pipelines.PipeReader Input { get; }
        System.IO.Pipelines.PipeWriter Output { get; }
    }
    public sealed partial class Pipe
    {
        public Pipe() { }
        public Pipe(System.IO.Pipelines.PipeOptions options) { }
        public System.IO.Pipelines.PipeReader Reader { get { throw null; } }
        public System.IO.Pipelines.PipeWriter Writer { get { throw null; } }
        public void Reset() { }
    }
    public partial class PipeOptions
    {
        public PipeOptions(System.Buffers.MemoryPool<byte>? pool = null, System.IO.Pipelines.PipeScheduler? readerScheduler = null, System.IO.Pipelines.PipeScheduler? writerScheduler = null, long pauseWriterThreshold = (long)-1, long resumeWriterThreshold = (long)-1, int minimumSegmentSize = -1, bool useSynchronizationContext = true) { }
        public static System.IO.Pipelines.PipeOptions Default { get { throw null; } }
        public int MinimumSegmentSize { get { throw null; } }
        public long PauseWriterThreshold { get { throw null; } }
        public System.Buffers.MemoryPool<byte> Pool { get { throw null; } }
        public System.IO.Pipelines.PipeScheduler ReaderScheduler { get { throw null; } }
        public long ResumeWriterThreshold { get { throw null; } }
        public bool UseSynchronizationContext { get { throw null; } }
        public System.IO.Pipelines.PipeScheduler WriterScheduler { get { throw null; } }
    }
    public abstract partial class PipeReader
    {
        protected PipeReader() { }
        public abstract void AdvanceTo(System.SequencePosition consumed);
        public abstract void AdvanceTo(System.SequencePosition consumed, System.SequencePosition examined);
        public virtual System.IO.Stream AsStream(bool leaveOpen = false) { throw null; }
        public abstract void CancelPendingRead();
        public abstract void Complete(System.Exception? exception = null);
        public virtual System.Threading.Tasks.ValueTask CompleteAsync(System.Exception? exception = null) { throw null; }
        public virtual System.Threading.Tasks.Task CopyToAsync(System.IO.Pipelines.PipeWriter destination, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual System.Threading.Tasks.Task CopyToAsync(System.IO.Stream destination, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.IO.Pipelines.PipeReader Create(System.Buffers.ReadOnlySequence<byte> sequence) { throw null; }
        public static System.IO.Pipelines.PipeReader Create(System.IO.Stream stream, System.IO.Pipelines.StreamPipeReaderOptions? readerOptions = null) { throw null; }
        [System.ObsoleteAttribute("OnWriterCompleted may not be invoked on all implementations of PipeReader. This will be removed in a future release.")]
        public virtual void OnWriterCompleted(System.Action<System.Exception?, object?> callback, object? state) { }
        public abstract System.Threading.Tasks.ValueTask<System.IO.Pipelines.ReadResult> ReadAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.ReadResult> ReadAtLeastAsync(int minimumSize, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        protected virtual System.Threading.Tasks.ValueTask<System.IO.Pipelines.ReadResult> ReadAtLeastAsyncCore(int minimumSize, System.Threading.CancellationToken cancellationToken) { throw null; }
        public abstract bool TryRead(out System.IO.Pipelines.ReadResult result);
    }
    public abstract partial class PipeScheduler
    {
        protected PipeScheduler() { }
        public static System.IO.Pipelines.PipeScheduler Inline { get { throw null; } }
        public static System.IO.Pipelines.PipeScheduler ThreadPool { get { throw null; } }
        public abstract void Schedule(System.Action<object?> action, object? state);
    }
    public abstract partial class PipeWriter : System.Buffers.IBufferWriter<byte>
    {
        protected PipeWriter() { }
        public abstract void Advance(int bytes);
        public virtual System.IO.Stream AsStream(bool leaveOpen = false) { throw null; }
        public abstract void CancelPendingFlush();
        public virtual bool CanGetUnflushedBytes => throw null;
        public abstract void Complete(System.Exception? exception = null);
        public virtual System.Threading.Tasks.ValueTask CompleteAsync(System.Exception? exception = null) { throw null; }
        protected internal virtual System.Threading.Tasks.Task CopyFromAsync(System.IO.Stream source, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.IO.Pipelines.PipeWriter Create(System.IO.Stream stream, System.IO.Pipelines.StreamPipeWriterOptions? writerOptions = null) { throw null; }
        public abstract System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> FlushAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        public abstract System.Memory<byte> GetMemory(int sizeHint = 0);
        public abstract System.Span<byte> GetSpan(int sizeHint = 0);
        [System.ObsoleteAttribute("OnReaderCompleted may not be invoked on all implementations of PipeWriter. This will be removed in a future release.")]
        public virtual void OnReaderCompleted(System.Action<System.Exception?, object?> callback, object? state) { }
        public virtual long UnflushedBytes => throw null;
        public virtual System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WriteAsync(System.ReadOnlyMemory<byte> source, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
    public readonly partial struct ReadResult
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public ReadResult(System.Buffers.ReadOnlySequence<byte> buffer, bool isCanceled, bool isCompleted) { throw null; }
        public System.Buffers.ReadOnlySequence<byte> Buffer { get { throw null; } }
        public bool IsCanceled { get { throw null; } }
        public bool IsCompleted { get { throw null; } }
    }
    public static partial class StreamPipeExtensions
    {
        public static System.Threading.Tasks.Task CopyToAsync(this System.IO.Stream source, System.IO.Pipelines.PipeWriter destination, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
    public partial class StreamPipeReaderOptions
    {
        public StreamPipeReaderOptions(System.Buffers.MemoryPool<byte>? pool, int bufferSize, int minimumReadSize, bool leaveOpen) { }
        public StreamPipeReaderOptions(System.Buffers.MemoryPool<byte>? pool = null, int bufferSize = -1, int minimumReadSize = -1, bool leaveOpen = false, bool useZeroByteReads = false) { }
        public int BufferSize { get { throw null; } }
        public bool LeaveOpen { get { throw null; } }
        public int MinimumReadSize { get { throw null; } }
        public System.Buffers.MemoryPool<byte> Pool { get { throw null; } }
        public bool UseZeroByteReads { get { throw null; } }
    }
    public partial class StreamPipeWriterOptions
    {
        public StreamPipeWriterOptions(System.Buffers.MemoryPool<byte>? pool = null, int minimumBufferSize = -1, bool leaveOpen = false) { }
        public bool LeaveOpen { get { throw null; } }
        public int MinimumBufferSize { get { throw null; } }
        public System.Buffers.MemoryPool<byte> Pool { get { throw null; } }
    }
}
