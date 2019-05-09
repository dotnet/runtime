// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System
{
    public partial interface IAsyncDisposable
    {
        System.Threading.Tasks.ValueTask DisposeAsync();
    }
}
namespace System.Collections.Generic
{
    public partial interface IAsyncEnumerable<out T>
    {
        System.Collections.Generic.IAsyncEnumerator<T> GetAsyncEnumerator(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
    }
    public partial interface IAsyncEnumerator<out T> : System.IAsyncDisposable
    {
        T Current { get; }
        System.Threading.Tasks.ValueTask<bool> MoveNextAsync();
    }
}
namespace System.Runtime.CompilerServices
{
    public partial struct AsyncIteratorMethodBuilder
    {
        private object _dummy;
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : System.Runtime.CompilerServices.INotifyCompletion where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
        public void Complete() { }
        public static System.Runtime.CompilerServices.AsyncIteratorMethodBuilder Create() { throw null; }
        public void MoveNext<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Method, Inherited=false, AllowMultiple=false)]
    public sealed partial class AsyncIteratorStateMachineAttribute : System.Runtime.CompilerServices.StateMachineAttribute
    {
        public AsyncIteratorStateMachineAttribute(System.Type stateMachineType) : base (default(System.Type)) { }
    }
    public readonly partial struct ConfiguredAsyncDisposable
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable DisposeAsync() { throw null; }
    }
    public readonly partial struct ConfiguredCancelableAsyncEnumerable<T>
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Runtime.CompilerServices.ConfiguredCancelableAsyncEnumerable<T> ConfigureAwait(bool continueOnCapturedContext) { throw null; }
        public System.Runtime.CompilerServices.ConfiguredCancelableAsyncEnumerable<T>.Enumerator GetAsyncEnumerator() { throw null; }
        public System.Runtime.CompilerServices.ConfiguredCancelableAsyncEnumerable<T> WithCancellation(System.Threading.CancellationToken cancellationToken) { throw null; }
        public readonly partial struct Enumerator
        {
            private readonly object _dummy;
            private readonly int _dummyPrimitive;
            public T Current { get { throw null; } }
            public System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable DisposeAsync() { throw null; }
            public System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable<bool> MoveNextAsync() { throw null; }
        }
    }
}
namespace System.Threading.Tasks
{
    public static partial class TaskAsyncEnumerableExtensions
    {
        public static System.Runtime.CompilerServices.ConfiguredAsyncDisposable ConfigureAwait(this System.IAsyncDisposable source, bool continueOnCapturedContext) { throw null; }
        public static System.Runtime.CompilerServices.ConfiguredCancelableAsyncEnumerable<T> ConfigureAwait<T>(this System.Collections.Generic.IAsyncEnumerable<T> source, bool continueOnCapturedContext) { throw null; }
        public static System.Runtime.CompilerServices.ConfiguredCancelableAsyncEnumerable<T> WithCancellation<T>(this System.Collections.Generic.IAsyncEnumerable<T> source, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
}
namespace System.Threading.Tasks.Sources
{
    public partial struct ManualResetValueTaskSourceCore<TResult>
    {
        private TResult _result;
        private object _dummy;
        private int _dummyPrimitive;
        public bool RunContinuationsAsynchronously { get { throw null; } set { } }
        public short Version { get { throw null; } }
        public TResult GetResult(short token) { throw null; }
        public System.Threading.Tasks.Sources.ValueTaskSourceStatus GetStatus(short token) { throw null; }
        public void OnCompleted(System.Action<object> continuation, object state, short token, System.Threading.Tasks.Sources.ValueTaskSourceOnCompletedFlags flags) { }
        public void Reset() { }
        public void SetException(System.Exception error) { }
        public void SetResult(TResult result) { }
    }
}
