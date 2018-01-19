// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Runtime.CompilerServices
{
    [System.AttributeUsageAttribute((System.AttributeTargets)(5148), Inherited=false, AllowMultiple=false)]
    public sealed partial class AsyncMethodBuilderAttribute : System.Attribute
    {
        public AsyncMethodBuilderAttribute(System.Type builderType) { }
        public System.Type BuilderType { get { throw null; } }
    }
    public partial struct AsyncValueTaskMethodBuilder<TResult>
    {
        private TResult _result;
        public System.Threading.Tasks.ValueTask<TResult> Task { get { throw null; } }
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : System.Runtime.CompilerServices.INotifyCompletion where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
        public static System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder<TResult> Create() { throw null; }
        public void SetException(System.Exception exception) { }
        public void SetResult(TResult result) { }
        public void SetStateMachine(System.Runtime.CompilerServices.IAsyncStateMachine stateMachine) { }
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
    }
    public readonly partial struct ConfiguredValueTaskAwaitable<TResult>
    {
        private readonly object _dummy;
        public System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable<TResult>.ConfiguredValueTaskAwaiter GetAwaiter() { throw null; }
        public partial struct ConfiguredValueTaskAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion, System.Runtime.CompilerServices.INotifyCompletion
        {
            private object _dummy;
            public bool IsCompleted { get { throw null; } }
            public TResult GetResult() { throw null; }
            public void OnCompleted(System.Action continuation) { }
            public void UnsafeOnCompleted(System.Action continuation) { }
        }
    }
    public partial struct ValueTaskAwaiter<TResult> : System.Runtime.CompilerServices.ICriticalNotifyCompletion, System.Runtime.CompilerServices.INotifyCompletion
    {
        private object _dummy;
        public bool IsCompleted { get { throw null; } }
        public TResult GetResult() { throw null; }
        public void OnCompleted(System.Action continuation) { }
        public void UnsafeOnCompleted(System.Action continuation) { }
    }
}
namespace System.Threading.Tasks
{
    [System.Runtime.CompilerServices.AsyncMethodBuilderAttribute(typeof(System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder<>))]
    public readonly partial struct ValueTask<TResult> : System.IEquatable<System.Threading.Tasks.ValueTask<TResult>>
    {
        internal readonly TResult _result;
        public ValueTask(System.Threading.Tasks.Task<TResult> task) { throw null; }
        public ValueTask(TResult result) { throw null; }
        public bool IsCanceled { get { throw null; } }
        public bool IsCompleted { get { throw null; } }
        public bool IsCompletedSuccessfully { get { throw null; } }
        public bool IsFaulted { get { throw null; } }
        public TResult Result { get { throw null; } }
        public System.Threading.Tasks.Task<TResult> AsTask() { throw null; }
        public System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable<TResult> ConfigureAwait(bool continueOnCapturedContext) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public static System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder<TResult> CreateAsyncMethodBuilder() { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.Threading.Tasks.ValueTask<TResult> other) { throw null; }
        public System.Runtime.CompilerServices.ValueTaskAwaiter<TResult> GetAwaiter() { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Threading.Tasks.ValueTask<TResult> left, System.Threading.Tasks.ValueTask<TResult> right) { throw null; }
        public static bool operator !=(System.Threading.Tasks.ValueTask<TResult> left, System.Threading.Tasks.ValueTask<TResult> right) { throw null; }
        public override string ToString() { throw null; }
    }
}
