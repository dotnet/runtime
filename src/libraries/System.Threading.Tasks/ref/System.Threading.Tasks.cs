// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

// These types were moved down to System.Runtime
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.AggregateException))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Runtime.CompilerServices.ConfiguredTaskAwaitable))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Runtime.CompilerServices.ConfiguredTaskAwaitable<>))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Runtime.CompilerServices.ICriticalNotifyCompletion))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Runtime.CompilerServices.INotifyCompletion))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Runtime.CompilerServices.TaskAwaiter))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Runtime.CompilerServices.TaskAwaiter<>))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Runtime.CompilerServices.YieldAwaitable))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Threading.CancellationToken))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Threading.CancellationTokenRegistration))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Threading.Tasks.Task))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Threading.Tasks.Task<>))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Threading.Tasks.TaskContinuationOptions))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Threading.Tasks.TaskCreationOptions))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Threading.Tasks.TaskFactory))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Threading.Tasks.TaskFactory<>))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Threading.Tasks.TaskScheduler))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Threading.Tasks.TaskStatus))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Threading.Tasks.UnobservedTaskExceptionEventArgs))]

namespace System
{
    public partial class OperationCanceledException : System.SystemException
    {
        public OperationCanceledException() { }
        public OperationCanceledException(string message) { }
        public OperationCanceledException(string message, System.Exception innerException) { }
        public OperationCanceledException(string message, System.Exception innerException, System.Threading.CancellationToken token) { }
        public OperationCanceledException(string message, System.Threading.CancellationToken token) { }
        public OperationCanceledException(System.Threading.CancellationToken token) { }
        protected OperationCanceledException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public System.Threading.CancellationToken CancellationToken { get { throw null; } }
    }
}
namespace System.Runtime.CompilerServices
{
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct AsyncTaskMethodBuilder
    {
        public System.Threading.Tasks.Task Task { get { throw null; } }
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : System.Runtime.CompilerServices.INotifyCompletion where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
        public static System.Runtime.CompilerServices.AsyncTaskMethodBuilder Create() { throw null; }
        public void SetException(System.Exception exception) { }
        public void SetResult() { }
        public void SetStateMachine(System.Runtime.CompilerServices.IAsyncStateMachine stateMachine) { }
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct AsyncTaskMethodBuilder<TResult>
    {
        public System.Threading.Tasks.Task<TResult> Task { get { throw null; } }
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : System.Runtime.CompilerServices.INotifyCompletion where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
        public static System.Runtime.CompilerServices.AsyncTaskMethodBuilder<TResult> Create() { throw null; }
        public void SetException(System.Exception exception) { }
        public void SetResult(TResult result) { }
        public void SetStateMachine(System.Runtime.CompilerServices.IAsyncStateMachine stateMachine) { }
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct AsyncVoidMethodBuilder
    {
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : System.Runtime.CompilerServices.INotifyCompletion where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
        public static System.Runtime.CompilerServices.AsyncVoidMethodBuilder Create() { throw null; }
        public void SetException(System.Exception exception) { }
        public void SetResult() { }
        public void SetStateMachine(System.Runtime.CompilerServices.IAsyncStateMachine stateMachine) { }
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
    }
    public partial interface IAsyncStateMachine
    {
        void MoveNext();
        void SetStateMachine(System.Runtime.CompilerServices.IAsyncStateMachine stateMachine);
    }
}
namespace System.Threading
{
    public partial class CancellationTokenSource : System.IDisposable
    {
        public CancellationTokenSource() { }
        public CancellationTokenSource(int millisecondsDelay) { }
        public CancellationTokenSource(System.TimeSpan delay) { }
        public bool IsCancellationRequested { get { throw null; } }
        public System.Threading.CancellationToken Token { get { throw null; } }
        public void Cancel() { }
        public void Cancel(bool throwOnFirstException) { }
        public void CancelAfter(int millisecondsDelay) { }
        public void CancelAfter(System.TimeSpan delay) { }
        public static System.Threading.CancellationTokenSource CreateLinkedTokenSource(System.Threading.CancellationToken token1, System.Threading.CancellationToken token2) { throw null; }
        public static System.Threading.CancellationTokenSource CreateLinkedTokenSource(params System.Threading.CancellationToken[] tokens) { throw null; }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
    }
}
namespace System.Threading.Tasks
{
    public partial class ConcurrentExclusiveSchedulerPair
    {
        public ConcurrentExclusiveSchedulerPair() { }
        public ConcurrentExclusiveSchedulerPair(System.Threading.Tasks.TaskScheduler taskScheduler) { }
        public ConcurrentExclusiveSchedulerPair(System.Threading.Tasks.TaskScheduler taskScheduler, int maxConcurrencyLevel) { }
        public ConcurrentExclusiveSchedulerPair(System.Threading.Tasks.TaskScheduler taskScheduler, int maxConcurrencyLevel, int maxItemsPerTask) { }
        public System.Threading.Tasks.Task Completion { get { throw null; } }
        public System.Threading.Tasks.TaskScheduler ConcurrentScheduler { get { throw null; } }
        public System.Threading.Tasks.TaskScheduler ExclusiveScheduler { get { throw null; } }
        public void Complete() { }
    }
    public partial class TaskCanceledException : System.OperationCanceledException
    {
        public TaskCanceledException() { }
        public TaskCanceledException(string message) { }
        public TaskCanceledException(string message, System.Exception innerException) { }
        public TaskCanceledException(System.Threading.Tasks.Task task) { }
        protected TaskCanceledException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public System.Threading.Tasks.Task Task { get { throw null; } }
    }
    public partial class TaskCompletionSource<TResult>
    {
        public TaskCompletionSource() { }
        public TaskCompletionSource(object state) { }
        public TaskCompletionSource(object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { }
        public TaskCompletionSource(System.Threading.Tasks.TaskCreationOptions creationOptions) { }
        public System.Threading.Tasks.Task<TResult> Task { get { throw null; } }
        public void SetCanceled() { }
        public void SetException(System.Collections.Generic.IEnumerable<System.Exception> exceptions) { }
        public void SetException(System.Exception exception) { }
        public void SetResult(TResult result) { }
        public bool TrySetCanceled() { throw null; }
        public bool TrySetCanceled(System.Threading.CancellationToken cancellationToken) { throw null; }
        public bool TrySetException(System.Collections.Generic.IEnumerable<System.Exception> exceptions) { throw null; }
        public bool TrySetException(System.Exception exception) { throw null; }
        public bool TrySetResult(TResult result) { throw null; }
    }
    public static partial class TaskExtensions
    {
        public static System.Threading.Tasks.Task Unwrap(this System.Threading.Tasks.Task<System.Threading.Tasks.Task> task) { throw null; }
        public static System.Threading.Tasks.Task<TResult> Unwrap<TResult>(this System.Threading.Tasks.Task<System.Threading.Tasks.Task<TResult>> task) { throw null; }
    }
    public partial class TaskSchedulerException : System.Exception
    {
        public TaskSchedulerException() { }
        public TaskSchedulerException(System.Exception innerException) { }
        public TaskSchedulerException(string message) { }
        public TaskSchedulerException(string message, System.Exception innerException) { }
        protected TaskSchedulerException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
}
