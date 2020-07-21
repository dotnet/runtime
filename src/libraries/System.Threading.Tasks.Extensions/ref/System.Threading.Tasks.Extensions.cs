// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Runtime.CompilerServices
{
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Delegate | System.AttributeTargets.Enum | System.AttributeTargets.Interface | System.AttributeTargets.Struct, Inherited=false, AllowMultiple=false)]
    public sealed partial class AsyncMethodBuilderAttribute : System.Attribute
    {
        public AsyncMethodBuilderAttribute(System.Type builderType) { }
        public System.Type BuilderType { get { throw null; } }
    }
    public partial struct AsyncValueTaskMethodBuilder
    {
        private object _dummy;
        private int _dummyPrimitive;
        public System.Threading.Tasks.ValueTask Task { get { throw null; } }
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : System.Runtime.CompilerServices.INotifyCompletion where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
        public static System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder Create() { throw null; }
        public void SetException(System.Exception exception) { }
        public void SetResult() { }
        public void SetStateMachine(System.Runtime.CompilerServices.IAsyncStateMachine stateMachine) { }
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
    }
    public partial struct AsyncValueTaskMethodBuilder<TResult>
    {
        private TResult _result;
        private object _dummy;
        private int _dummyPrimitive;
        public System.Threading.Tasks.ValueTask<TResult> Task { get { throw null; } }
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : System.Runtime.CompilerServices.INotifyCompletion where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
        public static System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder<TResult> Create() { throw null; }
        public void SetException(System.Exception exception) { }
        public void SetResult(TResult result) { }
        public void SetStateMachine(System.Runtime.CompilerServices.IAsyncStateMachine stateMachine) { }
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
    }
    public readonly partial struct ConfiguredValueTaskAwaitable
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter GetAwaiter() { throw null; }
        public readonly partial struct ConfiguredValueTaskAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion, System.Runtime.CompilerServices.INotifyCompletion
        {
            private readonly object _dummy;
            private readonly int _dummyPrimitive;
            public bool IsCompleted { get { throw null; } }
            public void GetResult() { }
            public void OnCompleted(System.Action continuation) { }
            public void UnsafeOnCompleted(System.Action continuation) { }
        }
    }
    public readonly partial struct ConfiguredValueTaskAwaitable<TResult>
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable<TResult>.ConfiguredValueTaskAwaiter GetAwaiter() { throw null; }
        public readonly partial struct ConfiguredValueTaskAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion, System.Runtime.CompilerServices.INotifyCompletion
        {
            private readonly object _dummy;
            private readonly int _dummyPrimitive;
            public bool IsCompleted { get { throw null; } }
            public TResult GetResult() { throw null; }
            public void OnCompleted(System.Action continuation) { }
            public void UnsafeOnCompleted(System.Action continuation) { }
        }
    }
    public readonly partial struct ValueTaskAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion, System.Runtime.CompilerServices.INotifyCompletion
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public bool IsCompleted { get { throw null; } }
        public void GetResult() { }
        public void OnCompleted(System.Action continuation) { }
        public void UnsafeOnCompleted(System.Action continuation) { }
    }
    public readonly partial struct ValueTaskAwaiter<TResult> : System.Runtime.CompilerServices.ICriticalNotifyCompletion, System.Runtime.CompilerServices.INotifyCompletion
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public bool IsCompleted { get { throw null; } }
        public TResult GetResult() { throw null; }
        public void OnCompleted(System.Action continuation) { }
        public void UnsafeOnCompleted(System.Action continuation) { }
    }
}
namespace System.Threading.Tasks
{
    public readonly partial struct ValueTask : System.IEquatable<System.Threading.Tasks.ValueTask>
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public ValueTask(System.Threading.Tasks.Sources.IValueTaskSource source, short token) { throw null; }
        public ValueTask(System.Threading.Tasks.Task task) { throw null; }
        public static System.Threading.Tasks.ValueTask CompletedTask { get { throw null; } }
        public bool IsCanceled { get { throw null; } }
        public bool IsCompleted { get { throw null; } }
        public bool IsCompletedSuccessfully { get { throw null; } }
        public bool IsFaulted { get { throw null; } }
        public System.Threading.Tasks.Task AsTask() { throw null; }
        public System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable ConfigureAwait(bool continueOnCapturedContext) { throw null; }
        public override bool Equals(object? obj) { throw null; }
        public bool Equals(System.Threading.Tasks.ValueTask other) { throw null; }
        public static System.Threading.Tasks.ValueTask FromCanceled(System.Threading.CancellationToken cancellationToken) { throw null; }
        public static System.Threading.Tasks.ValueTask<TResult> FromCanceled<TResult>(System.Threading.CancellationToken cancellationToken) { throw null; }
        public static System.Threading.Tasks.ValueTask FromException(System.Exception exception) { throw null; }
        public static System.Threading.Tasks.ValueTask<TResult> FromException<TResult>(System.Exception exception) { throw null; }
        public static System.Threading.Tasks.ValueTask<TResult> FromResult<TResult>(TResult result) { throw null; }
        public System.Runtime.CompilerServices.ValueTaskAwaiter GetAwaiter() { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Threading.Tasks.ValueTask left, System.Threading.Tasks.ValueTask right) { throw null; }
        public static bool operator !=(System.Threading.Tasks.ValueTask left, System.Threading.Tasks.ValueTask right) { throw null; }
        public System.Threading.Tasks.ValueTask Preserve() { throw null; }
    }
    public readonly partial struct ValueTask<TResult> : System.IEquatable<System.Threading.Tasks.ValueTask<TResult>>
    {
        private readonly TResult _result;
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public ValueTask(System.Threading.Tasks.Sources.IValueTaskSource<TResult> source, short token) { throw null; }
        public ValueTask(System.Threading.Tasks.Task<TResult> task) { throw null; }
        public ValueTask(TResult result) { throw null; }
        public bool IsCanceled { get { throw null; } }
        public bool IsCompleted { get { throw null; } }
        public bool IsCompletedSuccessfully { get { throw null; } }
        public bool IsFaulted { get { throw null; } }
        public TResult Result { get { throw null; } }
        public System.Threading.Tasks.Task<TResult> AsTask() { throw null; }
        public System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable<TResult> ConfigureAwait(bool continueOnCapturedContext) { throw null; }
        public override bool Equals(object? obj) { throw null; }
        public bool Equals(System.Threading.Tasks.ValueTask<TResult> other) { throw null; }
        public System.Runtime.CompilerServices.ValueTaskAwaiter<TResult> GetAwaiter() { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Threading.Tasks.ValueTask<TResult> left, System.Threading.Tasks.ValueTask<TResult> right) { throw null; }
        public static bool operator !=(System.Threading.Tasks.ValueTask<TResult> left, System.Threading.Tasks.ValueTask<TResult> right) { throw null; }
        public System.Threading.Tasks.ValueTask<TResult> Preserve() { throw null; }
        public override string? ToString() { throw null; }
    }
}
namespace System.Threading.Tasks.Sources
{
    public partial interface IValueTaskSource
    {
        void GetResult(short token);
        System.Threading.Tasks.Sources.ValueTaskSourceStatus GetStatus(short token);
        void OnCompleted(System.Action<object?> continuation, object? state, short token, System.Threading.Tasks.Sources.ValueTaskSourceOnCompletedFlags flags);
    }
    public partial interface IValueTaskSource<out TResult>
    {
        TResult GetResult(short token);
        System.Threading.Tasks.Sources.ValueTaskSourceStatus GetStatus(short token);
        void OnCompleted(System.Action<object?> continuation, object? state, short token, System.Threading.Tasks.Sources.ValueTaskSourceOnCompletedFlags flags);
    }
    [System.FlagsAttribute]
    public enum ValueTaskSourceOnCompletedFlags
    {
        None = 0,
        UseSchedulingContext = 1,
        FlowExecutionContext = 2,
    }
    public enum ValueTaskSourceStatus
    {
        Pending = 0,
        Succeeded = 1,
        Faulted = 2,
        Canceled = 3,
    }
}
