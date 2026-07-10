// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// NOTE: Types/members which are not publicly exposed in System.Runtime.dll but still used internally by libraries.
//       Manually maintained, keep in sync with System.Private.CoreLib.ExtraApis.txt

namespace System.Runtime.Serialization
{
    public readonly partial struct DeserializationToken : System.IDisposable
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        internal DeserializationToken(object tracker) { }
        public void Dispose() { }
    }
    public sealed partial class SerializationInfo
    {
        public static System.Runtime.Serialization.DeserializationToken StartDeserialization() { throw null; }
    }
}
namespace System.Diagnostics
{
    public partial class DebugProvider
    {
        public DebugProvider() { }
        [System.Diagnostics.CodeAnalysis.DoesNotReturnAttribute]
        public virtual void Fail(string? message, string? detailMessage) { throw null; }
        public static void FailCore(string stackTrace, string? message, string? detailMessage, string errorSource) { }
        public virtual void OnIndentLevelChanged(int indentLevel) { }
        public virtual void OnIndentSizeChanged(int indentSize) { }
        public virtual void Write(string? message) { }
        public static void WriteCore(string message) { }
        public virtual void WriteLine(string? message) { }
    }
    public static partial class Debug
    {
        public static System.Diagnostics.DebugProvider GetProvider() { throw null; }
        public static System.Diagnostics.DebugProvider SetProvider(System.Diagnostics.DebugProvider provider) { throw null; }
    }
}

namespace System.Threading
{
    public sealed partial class UnixHandleAsyncContext
    {
        public UnixHandleAsyncContext(System.Runtime.InteropServices.SafeHandle handle) { }
        public static bool IsSupported { get { throw null; } }
        public bool InlineCompletions { get { throw null; } set { } }
        public bool IsReadReady(out int observedSequenceNumber) { throw null; }
        public bool IsWriteReady(out int observedSequenceNumber) { throw null; }
        public System.Threading.UnixHandleAsyncContext.AsyncResult ReadAsync(System.Threading.UnixHandleAsyncContext.Operation operation, int observedSequenceNumber, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.UnixHandleAsyncContext.AsyncResult WriteAsync(System.Threading.UnixHandleAsyncContext.Operation operation, int observedSequenceNumber, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.UnixHandleAsyncContext.SyncResult ReadSync(System.Threading.UnixHandleAsyncContext.Operation operation, int observedSequenceNumber, int timeout) { throw null; }
        public System.Threading.UnixHandleAsyncContext.SyncResult WriteSync(System.Threading.UnixHandleAsyncContext.Operation operation, int observedSequenceNumber, int timeout) { throw null; }
        public bool AbortAndDispose() { throw null; }
        public enum AsyncResult
        {
            Pending = 0,
            Completed = 1,
            Aborted = 2,
        }
        public enum OnCompletedResult
        {
            Completed = 1,
            Aborted = 2,
            Canceled = 3,
        }
        public enum SyncResult
        {
            Completed = 1,
            Aborted = 2,
            TimedOut = 4,
        }
        public abstract partial class Operation : System.Threading.IThreadPoolWorkItem
        {
            protected internal abstract bool TryCompleteOperation(System.Runtime.InteropServices.SafeHandle handle);
            protected internal abstract void OnCompleted(System.Threading.UnixHandleAsyncContext.OnCompletedResult result);
            protected virtual void ExecuteThreadPoolWorkItem() { }
            void System.Threading.IThreadPoolWorkItem.Execute() { }
        }
    }
}

#if FEATURE_WASM_MANAGED_THREADS
namespace System.Threading
{
    public partial class Thread
    {
        [ThreadStatic]
        public static bool ThrowOnBlockingWaitOnJSInteropThread;
        [ThreadStatic]
        public static bool WarnOnBlockingWaitOnJSInteropThread;

        public static void AssureBlockingPossible() { throw null; }
        public static void ForceBlockingWait(Action<object?> action, object? state) { throw null; }
    }
}
#endif
