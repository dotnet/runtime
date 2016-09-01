// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System
{
    public partial class AggregateException : System.Exception
    {
        public AggregateException() { }
        public AggregateException(System.Collections.Generic.IEnumerable<System.Exception> innerExceptions) { }
        public AggregateException(params System.Exception[] innerExceptions) { }
        public AggregateException(string message) { }
        public AggregateException(string message, System.Collections.Generic.IEnumerable<System.Exception> innerExceptions) { }
        public AggregateException(string message, System.Exception innerException) { }
        public AggregateException(string message, params System.Exception[] innerExceptions) { }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Exception> InnerExceptions { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Exception>); } }
        public System.AggregateException Flatten() { return default(System.AggregateException); }
        public override System.Exception GetBaseException() { return default(System.Exception); }
        public void Handle(System.Func<System.Exception, bool> predicate) { }
        public override string ToString() { return default(string); }
    }
    public partial class OperationCanceledException : System.Exception
    {
        public OperationCanceledException() { }
        public OperationCanceledException(string message) { }
        public OperationCanceledException(string message, System.Exception innerException) { }
        public OperationCanceledException(string message, System.Exception innerException, System.Threading.CancellationToken token) { }
        public OperationCanceledException(string message, System.Threading.CancellationToken token) { }
        public OperationCanceledException(System.Threading.CancellationToken token) { }
        public System.Threading.CancellationToken CancellationToken { get { return default(System.Threading.CancellationToken); } }
    }
}
namespace System.Runtime.CompilerServices
{
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct AsyncTaskMethodBuilder
    {
        public System.Threading.Tasks.Task Task { get { return default(System.Threading.Tasks.Task); } }
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : System.Runtime.CompilerServices.INotifyCompletion where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
        public static System.Runtime.CompilerServices.AsyncTaskMethodBuilder Create() { return default(System.Runtime.CompilerServices.AsyncTaskMethodBuilder); }
        public void SetException(System.Exception exception) { }
        public void SetResult() { }
        public void SetStateMachine(System.Runtime.CompilerServices.IAsyncStateMachine stateMachine) { }
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct AsyncTaskMethodBuilder<TResult>
    {
        public System.Threading.Tasks.Task<TResult> Task { get { return default(System.Threading.Tasks.Task<TResult>); } }
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : System.Runtime.CompilerServices.INotifyCompletion where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
        public static System.Runtime.CompilerServices.AsyncTaskMethodBuilder<TResult> Create() { return default(System.Runtime.CompilerServices.AsyncTaskMethodBuilder<TResult>); }
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
        public static System.Runtime.CompilerServices.AsyncVoidMethodBuilder Create() { return default(System.Runtime.CompilerServices.AsyncVoidMethodBuilder); }
        public void SetException(System.Exception exception) { }
        public void SetResult() { }
        public void SetStateMachine(System.Runtime.CompilerServices.IAsyncStateMachine stateMachine) { }
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct ConfiguredTaskAwaitable
    {
        public System.Runtime.CompilerServices.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter GetAwaiter() { return default(System.Runtime.CompilerServices.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter); }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public partial struct ConfiguredTaskAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion, System.Runtime.CompilerServices.INotifyCompletion
        {
            public bool IsCompleted { get { return default(bool); } }
            public void GetResult() { }
            public void OnCompleted(System.Action continuation) { }
            [System.Security.SecurityCriticalAttribute]
            public void UnsafeOnCompleted(System.Action continuation) { }
        }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct ConfiguredTaskAwaitable<TResult>
    {
        public System.Runtime.CompilerServices.ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter GetAwaiter() { return default(System.Runtime.CompilerServices.ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter); }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public partial struct ConfiguredTaskAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion, System.Runtime.CompilerServices.INotifyCompletion
        {
            public bool IsCompleted { get { return default(bool); } }
            public TResult GetResult() { return default(TResult); }
            public void OnCompleted(System.Action continuation) { }
            [System.Security.SecurityCriticalAttribute]
            public void UnsafeOnCompleted(System.Action continuation) { }
        }
    }
    public partial interface IAsyncStateMachine
    {
        void MoveNext();
        void SetStateMachine(System.Runtime.CompilerServices.IAsyncStateMachine stateMachine);
    }
    public partial interface ICriticalNotifyCompletion : System.Runtime.CompilerServices.INotifyCompletion
    {
        [System.Security.SecurityCriticalAttribute]
        void UnsafeOnCompleted(System.Action continuation);
    }
    public partial interface INotifyCompletion
    {
        void OnCompleted(System.Action continuation);
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct TaskAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion, System.Runtime.CompilerServices.INotifyCompletion
    {
        public bool IsCompleted { get { return default(bool); } }
        public void GetResult() { }
        public void OnCompleted(System.Action continuation) { }
        [System.Security.SecurityCriticalAttribute]
        public void UnsafeOnCompleted(System.Action continuation) { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct TaskAwaiter<TResult> : System.Runtime.CompilerServices.ICriticalNotifyCompletion, System.Runtime.CompilerServices.INotifyCompletion
    {
        public bool IsCompleted { get { return default(bool); } }
        public TResult GetResult() { return default(TResult); }
        public void OnCompleted(System.Action continuation) { }
        [System.Security.SecurityCriticalAttribute]
        public void UnsafeOnCompleted(System.Action continuation) { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential, Size = 1)]
    public partial struct YieldAwaitable
    {
        public System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter GetAwaiter() { return default(System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter); }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential, Size = 1)]
        public partial struct YieldAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion, System.Runtime.CompilerServices.INotifyCompletion
        {
            public bool IsCompleted { get { return default(bool); } }
            public void GetResult() { }
            public void OnCompleted(System.Action continuation) { }
            [System.Security.SecurityCriticalAttribute]
            public void UnsafeOnCompleted(System.Action continuation) { }
        }
    }
}
namespace System.Threading
{
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct CancellationToken
    {
        public CancellationToken(bool canceled) { throw new System.NotImplementedException(); }
        public bool CanBeCanceled { get { return default(bool); } }
        public bool IsCancellationRequested { get { return default(bool); } }
        public static System.Threading.CancellationToken None { get { return default(System.Threading.CancellationToken); } }
        public System.Threading.WaitHandle WaitHandle { get { return default(System.Threading.WaitHandle); } }
        public override bool Equals(object other) { return default(bool); }
        public bool Equals(System.Threading.CancellationToken other) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static bool operator ==(System.Threading.CancellationToken left, System.Threading.CancellationToken right) { return default(bool); }
        public static bool operator !=(System.Threading.CancellationToken left, System.Threading.CancellationToken right) { return default(bool); }
        public System.Threading.CancellationTokenRegistration Register(System.Action callback) { return default(System.Threading.CancellationTokenRegistration); }
        public System.Threading.CancellationTokenRegistration Register(System.Action callback, bool useSynchronizationContext) { return default(System.Threading.CancellationTokenRegistration); }
        public System.Threading.CancellationTokenRegistration Register(System.Action<object> callback, object state) { return default(System.Threading.CancellationTokenRegistration); }
        public System.Threading.CancellationTokenRegistration Register(System.Action<object> callback, object state, bool useSynchronizationContext) { return default(System.Threading.CancellationTokenRegistration); }
        public void ThrowIfCancellationRequested() { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct CancellationTokenRegistration : System.IDisposable, System.IEquatable<System.Threading.CancellationTokenRegistration>
    {
        public void Dispose() { }
        public override bool Equals(object obj) { return default(bool); }
        public bool Equals(System.Threading.CancellationTokenRegistration other) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static bool operator ==(System.Threading.CancellationTokenRegistration left, System.Threading.CancellationTokenRegistration right) { return default(bool); }
        public static bool operator !=(System.Threading.CancellationTokenRegistration left, System.Threading.CancellationTokenRegistration right) { return default(bool); }
    }
    public partial class CancellationTokenSource : System.IDisposable
    {
        public CancellationTokenSource() { }
        public CancellationTokenSource(int millisecondsDelay) { }
        public CancellationTokenSource(System.TimeSpan delay) { }
        public bool IsCancellationRequested { get { return default(bool); } }
        public System.Threading.CancellationToken Token { get { return default(System.Threading.CancellationToken); } }
        public void Cancel() { }
        public void Cancel(bool throwOnFirstException) { }
        public void CancelAfter(int millisecondsDelay) { }
        public void CancelAfter(System.TimeSpan delay) { }
        public static System.Threading.CancellationTokenSource CreateLinkedTokenSource(System.Threading.CancellationToken token1, System.Threading.CancellationToken token2) { return default(System.Threading.CancellationTokenSource); }
        public static System.Threading.CancellationTokenSource CreateLinkedTokenSource(params System.Threading.CancellationToken[] tokens) { return default(System.Threading.CancellationTokenSource); }
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
        public System.Threading.Tasks.Task Completion { get { return default(System.Threading.Tasks.Task); } }
        public System.Threading.Tasks.TaskScheduler ConcurrentScheduler { get { return default(System.Threading.Tasks.TaskScheduler); } }
        public System.Threading.Tasks.TaskScheduler ExclusiveScheduler { get { return default(System.Threading.Tasks.TaskScheduler); } }
        public void Complete() { }
    }
    public partial class Task : System.IAsyncResult
    {
        public Task(System.Action action) { }
        public Task(System.Action action, System.Threading.CancellationToken cancellationToken) { }
        public Task(System.Action action, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions) { }
        public Task(System.Action action, System.Threading.Tasks.TaskCreationOptions creationOptions) { }
        public Task(System.Action<object> action, object state) { }
        public Task(System.Action<object> action, object state, System.Threading.CancellationToken cancellationToken) { }
        public Task(System.Action<object> action, object state, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions) { }
        public Task(System.Action<object> action, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { }
        public object AsyncState { get { return default(object); } }
        public static System.Threading.Tasks.Task CompletedTask { get { return default(System.Threading.Tasks.Task); } }
        public System.Threading.Tasks.TaskCreationOptions CreationOptions { get { return default(System.Threading.Tasks.TaskCreationOptions); } }
        public static System.Nullable<int> CurrentId { get { return default(System.Nullable<int>); } }
        public System.AggregateException Exception { get { return default(System.AggregateException); } }
        public static System.Threading.Tasks.TaskFactory Factory { get { return default(System.Threading.Tasks.TaskFactory); } }
        public int Id { get { return default(int); } }
        public bool IsCanceled { get { return default(bool); } }
        public bool IsCompleted { get { return default(bool); } }
        public bool IsFaulted { get { return default(bool); } }
        public System.Threading.Tasks.TaskStatus Status { get { return default(System.Threading.Tasks.TaskStatus); } }
        System.Threading.WaitHandle System.IAsyncResult.AsyncWaitHandle { get { return default(System.Threading.WaitHandle); } }
        bool System.IAsyncResult.CompletedSynchronously { get { return default(bool); } }
        public System.Runtime.CompilerServices.ConfiguredTaskAwaitable ConfigureAwait(bool continueOnCapturedContext) { return default(System.Runtime.CompilerServices.ConfiguredTaskAwaitable); }
        public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task, object> continuationAction, object state) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task, object> continuationAction, object state, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task, object> continuationAction, object state, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task, object> continuationAction, object state, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task, object> continuationAction, object state, System.Threading.Tasks.TaskScheduler scheduler) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task> continuationAction) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task> continuationAction, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task> continuationAction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task> continuationAction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task> continuationAction, System.Threading.Tasks.TaskScheduler scheduler) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task<TResult> ContinueWith<TResult>(System.Func<System.Threading.Tasks.Task, TResult> continuationFunction) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> ContinueWith<TResult>(System.Func<System.Threading.Tasks.Task, TResult> continuationFunction, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> ContinueWith<TResult>(System.Func<System.Threading.Tasks.Task, TResult> continuationFunction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> ContinueWith<TResult>(System.Func<System.Threading.Tasks.Task, TResult> continuationFunction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> ContinueWith<TResult>(System.Func<System.Threading.Tasks.Task, TResult> continuationFunction, System.Threading.Tasks.TaskScheduler scheduler) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> ContinueWith<TResult>(System.Func<System.Threading.Tasks.Task, object, TResult> continuationFunction, object state) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> ContinueWith<TResult>(System.Func<System.Threading.Tasks.Task, object, TResult> continuationFunction, object state, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> ContinueWith<TResult>(System.Func<System.Threading.Tasks.Task, object, TResult> continuationFunction, object state, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> ContinueWith<TResult>(System.Func<System.Threading.Tasks.Task, object, TResult> continuationFunction, object state, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> ContinueWith<TResult>(System.Func<System.Threading.Tasks.Task, object, TResult> continuationFunction, object state, System.Threading.Tasks.TaskScheduler scheduler) { return default(System.Threading.Tasks.Task<TResult>); }
        public static System.Threading.Tasks.Task Delay(int millisecondsDelay) { return default(System.Threading.Tasks.Task); }
        public static System.Threading.Tasks.Task Delay(int millisecondsDelay, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
        public static System.Threading.Tasks.Task Delay(System.TimeSpan delay) { return default(System.Threading.Tasks.Task); }
        public static System.Threading.Tasks.Task Delay(System.TimeSpan delay, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
        public static System.Threading.Tasks.Task FromCanceled(System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
        public static System.Threading.Tasks.Task<TResult> FromCanceled<TResult>(System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<TResult>); }
        public static System.Threading.Tasks.Task FromException(System.Exception exception) { return default(System.Threading.Tasks.Task); }
        public static System.Threading.Tasks.Task<TResult> FromException<TResult>(System.Exception exception) { return default(System.Threading.Tasks.Task<TResult>); }
        public static System.Threading.Tasks.Task<TResult> FromResult<TResult>(TResult result) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Runtime.CompilerServices.TaskAwaiter GetAwaiter() { return default(System.Runtime.CompilerServices.TaskAwaiter); }
        public static System.Threading.Tasks.Task Run(System.Action action) { return default(System.Threading.Tasks.Task); }
        public static System.Threading.Tasks.Task Run(System.Action action, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
        public static System.Threading.Tasks.Task Run(System.Func<System.Threading.Tasks.Task> function) { return default(System.Threading.Tasks.Task); }
        public static System.Threading.Tasks.Task Run(System.Func<System.Threading.Tasks.Task> function, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
        public static System.Threading.Tasks.Task<TResult> Run<TResult>(System.Func<TResult> function) { return default(System.Threading.Tasks.Task<TResult>); }
        public static System.Threading.Tasks.Task<TResult> Run<TResult>(System.Func<TResult> function, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<TResult>); }
        public static System.Threading.Tasks.Task<TResult> Run<TResult>(System.Func<System.Threading.Tasks.Task<TResult>> function) { return default(System.Threading.Tasks.Task<TResult>); }
        public static System.Threading.Tasks.Task<TResult> Run<TResult>(System.Func<System.Threading.Tasks.Task<TResult>> function, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<TResult>); }
        public void RunSynchronously() { }
        public void RunSynchronously(System.Threading.Tasks.TaskScheduler scheduler) { }
        public void Start() { }
        public void Start(System.Threading.Tasks.TaskScheduler scheduler) { }
        public void Wait() { }
        public bool Wait(int millisecondsTimeout) { return default(bool); }
        public bool Wait(int millisecondsTimeout, System.Threading.CancellationToken cancellationToken) { return default(bool); }
        public void Wait(System.Threading.CancellationToken cancellationToken) { }
        public bool Wait(System.TimeSpan timeout) { return default(bool); }
        public static void WaitAll(params System.Threading.Tasks.Task[] tasks) { }
        public static bool WaitAll(System.Threading.Tasks.Task[] tasks, int millisecondsTimeout) { return default(bool); }
        public static bool WaitAll(System.Threading.Tasks.Task[] tasks, int millisecondsTimeout, System.Threading.CancellationToken cancellationToken) { return default(bool); }
        public static void WaitAll(System.Threading.Tasks.Task[] tasks, System.Threading.CancellationToken cancellationToken) { }
        public static bool WaitAll(System.Threading.Tasks.Task[] tasks, System.TimeSpan timeout) { return default(bool); }
        public static int WaitAny(params System.Threading.Tasks.Task[] tasks) { return default(int); }
        public static int WaitAny(System.Threading.Tasks.Task[] tasks, int millisecondsTimeout) { return default(int); }
        public static int WaitAny(System.Threading.Tasks.Task[] tasks, int millisecondsTimeout, System.Threading.CancellationToken cancellationToken) { return default(int); }
        public static int WaitAny(System.Threading.Tasks.Task[] tasks, System.Threading.CancellationToken cancellationToken) { return default(int); }
        public static int WaitAny(System.Threading.Tasks.Task[] tasks, System.TimeSpan timeout) { return default(int); }
        public static System.Threading.Tasks.Task WhenAll(System.Collections.Generic.IEnumerable<System.Threading.Tasks.Task> tasks) { return default(System.Threading.Tasks.Task); }
        public static System.Threading.Tasks.Task WhenAll(params System.Threading.Tasks.Task[] tasks) { return default(System.Threading.Tasks.Task); }
        public static System.Threading.Tasks.Task<TResult[]> WhenAll<TResult>(System.Collections.Generic.IEnumerable<System.Threading.Tasks.Task<TResult>> tasks) { return default(System.Threading.Tasks.Task<TResult[]>); }
        public static System.Threading.Tasks.Task<TResult[]> WhenAll<TResult>(params System.Threading.Tasks.Task<TResult>[] tasks) { return default(System.Threading.Tasks.Task<TResult[]>); }
        public static System.Threading.Tasks.Task<System.Threading.Tasks.Task> WhenAny(System.Collections.Generic.IEnumerable<System.Threading.Tasks.Task> tasks) { return default(System.Threading.Tasks.Task<System.Threading.Tasks.Task>); }
        public static System.Threading.Tasks.Task<System.Threading.Tasks.Task> WhenAny(params System.Threading.Tasks.Task[] tasks) { return default(System.Threading.Tasks.Task<System.Threading.Tasks.Task>); }
        public static System.Threading.Tasks.Task<System.Threading.Tasks.Task<TResult>> WhenAny<TResult>(System.Collections.Generic.IEnumerable<System.Threading.Tasks.Task<TResult>> tasks) { return default(System.Threading.Tasks.Task<System.Threading.Tasks.Task<TResult>>); }
        public static System.Threading.Tasks.Task<System.Threading.Tasks.Task<TResult>> WhenAny<TResult>(params System.Threading.Tasks.Task<TResult>[] tasks) { return default(System.Threading.Tasks.Task<System.Threading.Tasks.Task<TResult>>); }
        public static System.Runtime.CompilerServices.YieldAwaitable Yield() { return default(System.Runtime.CompilerServices.YieldAwaitable); }
    }
    public partial class Task<TResult> : System.Threading.Tasks.Task
    {
        public Task(System.Func<TResult> function) : base(default(System.Action)) { }
        public Task(System.Func<TResult> function, System.Threading.CancellationToken cancellationToken) : base(default(System.Action)) { }
        public Task(System.Func<TResult> function, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions) : base(default(System.Action)) { }
        public Task(System.Func<TResult> function, System.Threading.Tasks.TaskCreationOptions creationOptions) : base(default(System.Action)) { }
        public Task(System.Func<object, TResult> function, object state) : base(default(System.Action)) { }
        public Task(System.Func<object, TResult> function, object state, System.Threading.CancellationToken cancellationToken) : base(default(System.Action)) { }
        public Task(System.Func<object, TResult> function, object state, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions) : base(default(System.Action)) { }
        public Task(System.Func<object, TResult> function, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) : base(default(System.Action)) { }
        public static new System.Threading.Tasks.TaskFactory<TResult> Factory { get { return default(System.Threading.Tasks.TaskFactory<TResult>); } }
        public TResult Result { get { return default(TResult); } }
        public new System.Runtime.CompilerServices.ConfiguredTaskAwaitable<TResult> ConfigureAwait(bool continueOnCapturedContext) { return default(System.Runtime.CompilerServices.ConfiguredTaskAwaitable<TResult>); }
        public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task<TResult>, object> continuationAction, object state) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task<TResult>, object> continuationAction, object state, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task<TResult>, object> continuationAction, object state, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task<TResult>, object> continuationAction, object state, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task<TResult>, object> continuationAction, object state, System.Threading.Tasks.TaskScheduler scheduler) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task<TResult>> continuationAction) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task<TResult>> continuationAction, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task<TResult>> continuationAction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task<TResult>> continuationAction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task<TResult>> continuationAction, System.Threading.Tasks.TaskScheduler scheduler) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task<TNewResult> ContinueWith<TNewResult>(System.Func<System.Threading.Tasks.Task<TResult>, TNewResult> continuationFunction) { return default(System.Threading.Tasks.Task<TNewResult>); }
        public System.Threading.Tasks.Task<TNewResult> ContinueWith<TNewResult>(System.Func<System.Threading.Tasks.Task<TResult>, TNewResult> continuationFunction, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<TNewResult>); }
        public System.Threading.Tasks.Task<TNewResult> ContinueWith<TNewResult>(System.Func<System.Threading.Tasks.Task<TResult>, TNewResult> continuationFunction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { return default(System.Threading.Tasks.Task<TNewResult>); }
        public System.Threading.Tasks.Task<TNewResult> ContinueWith<TNewResult>(System.Func<System.Threading.Tasks.Task<TResult>, TNewResult> continuationFunction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { return default(System.Threading.Tasks.Task<TNewResult>); }
        public System.Threading.Tasks.Task<TNewResult> ContinueWith<TNewResult>(System.Func<System.Threading.Tasks.Task<TResult>, TNewResult> continuationFunction, System.Threading.Tasks.TaskScheduler scheduler) { return default(System.Threading.Tasks.Task<TNewResult>); }
        public System.Threading.Tasks.Task<TNewResult> ContinueWith<TNewResult>(System.Func<System.Threading.Tasks.Task<TResult>, object, TNewResult> continuationFunction, object state) { return default(System.Threading.Tasks.Task<TNewResult>); }
        public System.Threading.Tasks.Task<TNewResult> ContinueWith<TNewResult>(System.Func<System.Threading.Tasks.Task<TResult>, object, TNewResult> continuationFunction, object state, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<TNewResult>); }
        public System.Threading.Tasks.Task<TNewResult> ContinueWith<TNewResult>(System.Func<System.Threading.Tasks.Task<TResult>, object, TNewResult> continuationFunction, object state, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { return default(System.Threading.Tasks.Task<TNewResult>); }
        public System.Threading.Tasks.Task<TNewResult> ContinueWith<TNewResult>(System.Func<System.Threading.Tasks.Task<TResult>, object, TNewResult> continuationFunction, object state, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { return default(System.Threading.Tasks.Task<TNewResult>); }
        public System.Threading.Tasks.Task<TNewResult> ContinueWith<TNewResult>(System.Func<System.Threading.Tasks.Task<TResult>, object, TNewResult> continuationFunction, object state, System.Threading.Tasks.TaskScheduler scheduler) { return default(System.Threading.Tasks.Task<TNewResult>); }
        public new System.Runtime.CompilerServices.TaskAwaiter<TResult> GetAwaiter() { return default(System.Runtime.CompilerServices.TaskAwaiter<TResult>); }
    }
    public partial class TaskCanceledException : System.OperationCanceledException
    {
        public TaskCanceledException() { }
        public TaskCanceledException(string message) { }
        public TaskCanceledException(string message, System.Exception innerException) { }
        public TaskCanceledException(System.Threading.Tasks.Task task) { }
        public System.Threading.Tasks.Task Task { get { return default(System.Threading.Tasks.Task); } }
    }
    public partial class TaskCompletionSource<TResult>
    {
        public TaskCompletionSource() { }
        public TaskCompletionSource(object state) { }
        public TaskCompletionSource(object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { }
        public TaskCompletionSource(System.Threading.Tasks.TaskCreationOptions creationOptions) { }
        public System.Threading.Tasks.Task<TResult> Task { get { return default(System.Threading.Tasks.Task<TResult>); } }
        public void SetCanceled() { }
        public void SetException(System.Collections.Generic.IEnumerable<System.Exception> exceptions) { }
        public void SetException(System.Exception exception) { }
        public void SetResult(TResult result) { }
        public bool TrySetCanceled() { return default(bool); }
        public bool TrySetCanceled(System.Threading.CancellationToken cancellationToken) { return default(bool); }
        public bool TrySetException(System.Collections.Generic.IEnumerable<System.Exception> exceptions) { return default(bool); }
        public bool TrySetException(System.Exception exception) { return default(bool); }
        public bool TrySetResult(TResult result) { return default(bool); }
    }
    [System.FlagsAttribute]
    public enum TaskContinuationOptions
    {
        AttachedToParent = 4,
        DenyChildAttach = 8,
        ExecuteSynchronously = 524288,
        HideScheduler = 16,
        LazyCancellation = 32,
        LongRunning = 2,
        None = 0,
        NotOnCanceled = 262144,
        NotOnFaulted = 131072,
        NotOnRanToCompletion = 65536,
        OnlyOnCanceled = 196608,
        OnlyOnFaulted = 327680,
        OnlyOnRanToCompletion = 393216,
        PreferFairness = 1,
        RunContinuationsAsynchronously = 64,
    }
    [System.FlagsAttribute]
    public enum TaskCreationOptions
    {
        AttachedToParent = 4,
        DenyChildAttach = 8,
        HideScheduler = 16,
        LongRunning = 2,
        None = 0,
        PreferFairness = 1,
        RunContinuationsAsynchronously = 64,
    }
    public static partial class TaskExtensions
    {
        public static System.Threading.Tasks.Task Unwrap(this System.Threading.Tasks.Task<System.Threading.Tasks.Task> task) { return default(System.Threading.Tasks.Task); }
        public static System.Threading.Tasks.Task<TResult> Unwrap<TResult>(this System.Threading.Tasks.Task<System.Threading.Tasks.Task<TResult>> task) { return default(System.Threading.Tasks.Task<TResult>); }
    }
    public partial class TaskFactory
    {
        public TaskFactory() { }
        public TaskFactory(System.Threading.CancellationToken cancellationToken) { }
        public TaskFactory(System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { }
        public TaskFactory(System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { }
        public TaskFactory(System.Threading.Tasks.TaskScheduler scheduler) { }
        public System.Threading.CancellationToken CancellationToken { get { return default(System.Threading.CancellationToken); } }
        public System.Threading.Tasks.TaskContinuationOptions ContinuationOptions { get { return default(System.Threading.Tasks.TaskContinuationOptions); } }
        public System.Threading.Tasks.TaskCreationOptions CreationOptions { get { return default(System.Threading.Tasks.TaskCreationOptions); } }
        public System.Threading.Tasks.TaskScheduler Scheduler { get { return default(System.Threading.Tasks.TaskScheduler); } }
        public System.Threading.Tasks.Task ContinueWhenAll(System.Threading.Tasks.Task[] tasks, System.Action<System.Threading.Tasks.Task[]> continuationAction) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task ContinueWhenAll(System.Threading.Tasks.Task[] tasks, System.Action<System.Threading.Tasks.Task[]> continuationAction, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task ContinueWhenAll(System.Threading.Tasks.Task[] tasks, System.Action<System.Threading.Tasks.Task[]> continuationAction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task ContinueWhenAll(System.Threading.Tasks.Task[] tasks, System.Action<System.Threading.Tasks.Task[]> continuationAction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TResult>(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task[], TResult> continuationFunction) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TResult>(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task[], TResult> continuationFunction, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TResult>(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task[], TResult> continuationFunction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TResult>(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task[], TResult> continuationFunction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task ContinueWhenAll<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Action<System.Threading.Tasks.Task<TAntecedentResult>[]> continuationAction) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task ContinueWhenAll<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Action<System.Threading.Tasks.Task<TAntecedentResult>[]> continuationAction, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task ContinueWhenAll<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Action<System.Threading.Tasks.Task<TAntecedentResult>[]> continuationAction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task ContinueWhenAll<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Action<System.Threading.Tasks.Task<TAntecedentResult>[]> continuationAction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>[], TResult> continuationFunction) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>[], TResult> continuationFunction, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>[], TResult> continuationFunction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>[], TResult> continuationFunction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task ContinueWhenAny(System.Threading.Tasks.Task[] tasks, System.Action<System.Threading.Tasks.Task> continuationAction) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task ContinueWhenAny(System.Threading.Tasks.Task[] tasks, System.Action<System.Threading.Tasks.Task> continuationAction, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task ContinueWhenAny(System.Threading.Tasks.Task[] tasks, System.Action<System.Threading.Tasks.Task> continuationAction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task ContinueWhenAny(System.Threading.Tasks.Task[] tasks, System.Action<System.Threading.Tasks.Task> continuationAction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TResult>(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task, TResult> continuationFunction) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TResult>(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task, TResult> continuationFunction, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TResult>(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task, TResult> continuationFunction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TResult>(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task, TResult> continuationFunction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task ContinueWhenAny<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Action<System.Threading.Tasks.Task<TAntecedentResult>> continuationAction) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task ContinueWhenAny<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Action<System.Threading.Tasks.Task<TAntecedentResult>> continuationAction, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task ContinueWhenAny<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Action<System.Threading.Tasks.Task<TAntecedentResult>> continuationAction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task ContinueWhenAny<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Action<System.Threading.Tasks.Task<TAntecedentResult>> continuationAction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>, TResult> continuationFunction) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>, TResult> continuationFunction, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>, TResult> continuationFunction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>, TResult> continuationFunction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task FromAsync(System.Func<System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Action<System.IAsyncResult> endMethod, object state) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task FromAsync(System.Func<System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Action<System.IAsyncResult> endMethod, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task FromAsync(System.IAsyncResult asyncResult, System.Action<System.IAsyncResult> endMethod) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task FromAsync(System.IAsyncResult asyncResult, System.Action<System.IAsyncResult> endMethod, System.Threading.Tasks.TaskCreationOptions creationOptions) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task FromAsync(System.IAsyncResult asyncResult, System.Action<System.IAsyncResult> endMethod, System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskScheduler scheduler) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task FromAsync<TArg1>(System.Func<TArg1, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Action<System.IAsyncResult> endMethod, TArg1 arg1, object state) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task FromAsync<TArg1>(System.Func<TArg1, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Action<System.IAsyncResult> endMethod, TArg1 arg1, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task<TResult> FromAsync<TResult>(System.Func<System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, object state) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> FromAsync<TResult>(System.Func<System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> FromAsync<TResult>(System.IAsyncResult asyncResult, System.Func<System.IAsyncResult, TResult> endMethod) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> FromAsync<TResult>(System.IAsyncResult asyncResult, System.Func<System.IAsyncResult, TResult> endMethod, System.Threading.Tasks.TaskCreationOptions creationOptions) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> FromAsync<TResult>(System.IAsyncResult asyncResult, System.Func<System.IAsyncResult, TResult> endMethod, System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskScheduler scheduler) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task FromAsync<TArg1, TArg2>(System.Func<TArg1, TArg2, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Action<System.IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, object state) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task FromAsync<TArg1, TArg2>(System.Func<TArg1, TArg2, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Action<System.IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task<TResult> FromAsync<TArg1, TResult>(System.Func<TArg1, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, object state) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> FromAsync<TArg1, TResult>(System.Func<TArg1, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task FromAsync<TArg1, TArg2, TArg3>(System.Func<TArg1, TArg2, TArg3, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Action<System.IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task FromAsync<TArg1, TArg2, TArg3>(System.Func<TArg1, TArg2, TArg3, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Action<System.IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task<TResult> FromAsync<TArg1, TArg2, TResult>(System.Func<TArg1, TArg2, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> FromAsync<TArg1, TArg2, TResult>(System.Func<TArg1, TArg2, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> FromAsync<TArg1, TArg2, TArg3, TResult>(System.Func<TArg1, TArg2, TArg3, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> FromAsync<TArg1, TArg2, TArg3, TResult>(System.Func<TArg1, TArg2, TArg3, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task StartNew(System.Action action) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task StartNew(System.Action action, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task StartNew(System.Action action, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskScheduler scheduler) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task StartNew(System.Action action, System.Threading.Tasks.TaskCreationOptions creationOptions) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task StartNew(System.Action<object> action, object state) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task StartNew(System.Action<object> action, object state, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task StartNew(System.Action<object> action, object state, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskScheduler scheduler) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task StartNew(System.Action<object> action, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task<TResult> StartNew<TResult>(System.Func<TResult> function) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> StartNew<TResult>(System.Func<TResult> function, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> StartNew<TResult>(System.Func<TResult> function, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskScheduler scheduler) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> StartNew<TResult>(System.Func<TResult> function, System.Threading.Tasks.TaskCreationOptions creationOptions) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> StartNew<TResult>(System.Func<object, TResult> function, object state) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> StartNew<TResult>(System.Func<object, TResult> function, object state, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> StartNew<TResult>(System.Func<object, TResult> function, object state, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskScheduler scheduler) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> StartNew<TResult>(System.Func<object, TResult> function, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { return default(System.Threading.Tasks.Task<TResult>); }
    }
    public partial class TaskFactory<TResult>
    {
        public TaskFactory() { }
        public TaskFactory(System.Threading.CancellationToken cancellationToken) { }
        public TaskFactory(System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { }
        public TaskFactory(System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { }
        public TaskFactory(System.Threading.Tasks.TaskScheduler scheduler) { }
        public System.Threading.CancellationToken CancellationToken { get { return default(System.Threading.CancellationToken); } }
        public System.Threading.Tasks.TaskContinuationOptions ContinuationOptions { get { return default(System.Threading.Tasks.TaskContinuationOptions); } }
        public System.Threading.Tasks.TaskCreationOptions CreationOptions { get { return default(System.Threading.Tasks.TaskCreationOptions); } }
        public System.Threading.Tasks.TaskScheduler Scheduler { get { return default(System.Threading.Tasks.TaskScheduler); } }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAll(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task[], TResult> continuationFunction) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAll(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task[], TResult> continuationFunction, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAll(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task[], TResult> continuationFunction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAll(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task[], TResult> continuationFunction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>[], TResult> continuationFunction) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>[], TResult> continuationFunction, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>[], TResult> continuationFunction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>[], TResult> continuationFunction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAny(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task, TResult> continuationFunction) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAny(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task, TResult> continuationFunction, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAny(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task, TResult> continuationFunction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAny(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task, TResult> continuationFunction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>, TResult> continuationFunction) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>, TResult> continuationFunction, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>, TResult> continuationFunction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>, TResult> continuationFunction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> FromAsync(System.Func<System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, object state) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> FromAsync(System.Func<System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> FromAsync(System.IAsyncResult asyncResult, System.Func<System.IAsyncResult, TResult> endMethod) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> FromAsync(System.IAsyncResult asyncResult, System.Func<System.IAsyncResult, TResult> endMethod, System.Threading.Tasks.TaskCreationOptions creationOptions) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> FromAsync(System.IAsyncResult asyncResult, System.Func<System.IAsyncResult, TResult> endMethod, System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskScheduler scheduler) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> FromAsync<TArg1>(System.Func<TArg1, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, object state) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> FromAsync<TArg1>(System.Func<TArg1, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> FromAsync<TArg1, TArg2>(System.Func<TArg1, TArg2, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> FromAsync<TArg1, TArg2>(System.Func<TArg1, TArg2, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> FromAsync<TArg1, TArg2, TArg3>(System.Func<TArg1, TArg2, TArg3, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> FromAsync<TArg1, TArg2, TArg3>(System.Func<TArg1, TArg2, TArg3, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> StartNew(System.Func<TResult> function) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> StartNew(System.Func<TResult> function, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> StartNew(System.Func<TResult> function, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskScheduler scheduler) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> StartNew(System.Func<TResult> function, System.Threading.Tasks.TaskCreationOptions creationOptions) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> StartNew(System.Func<object, TResult> function, object state) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> StartNew(System.Func<object, TResult> function, object state, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> StartNew(System.Func<object, TResult> function, object state, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskScheduler scheduler) { return default(System.Threading.Tasks.Task<TResult>); }
        public System.Threading.Tasks.Task<TResult> StartNew(System.Func<object, TResult> function, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { return default(System.Threading.Tasks.Task<TResult>); }
    }
    public abstract partial class TaskScheduler
    {
        protected TaskScheduler() { }
        public static System.Threading.Tasks.TaskScheduler Current { get { return default(System.Threading.Tasks.TaskScheduler); } }
        public static System.Threading.Tasks.TaskScheduler Default { get { return default(System.Threading.Tasks.TaskScheduler); } }
        public int Id { get { return default(int); } }
        public virtual int MaximumConcurrencyLevel { get { return default(int); } }
        public static event System.EventHandler<System.Threading.Tasks.UnobservedTaskExceptionEventArgs> UnobservedTaskException { add { } remove { } }
        public static System.Threading.Tasks.TaskScheduler FromCurrentSynchronizationContext() { return default(System.Threading.Tasks.TaskScheduler); }
        [System.Security.SecurityCriticalAttribute]
        protected abstract System.Collections.Generic.IEnumerable<System.Threading.Tasks.Task> GetScheduledTasks();
        [System.Security.SecurityCriticalAttribute]
        protected internal abstract void QueueTask(System.Threading.Tasks.Task task);
        [System.Security.SecurityCriticalAttribute]
        protected internal virtual bool TryDequeue(System.Threading.Tasks.Task task) { return default(bool); }
        [System.Security.SecurityCriticalAttribute]
        protected bool TryExecuteTask(System.Threading.Tasks.Task task) { return default(bool); }
        [System.Security.SecurityCriticalAttribute]
        protected abstract bool TryExecuteTaskInline(System.Threading.Tasks.Task task, bool taskWasPreviouslyQueued);
    }
    public partial class TaskSchedulerException : System.Exception
    {
        public TaskSchedulerException() { }
        public TaskSchedulerException(System.Exception innerException) { }
        public TaskSchedulerException(string message) { }
        public TaskSchedulerException(string message, System.Exception innerException) { }
    }
    public enum TaskStatus
    {
        Canceled = 6,
        Created = 0,
        Faulted = 7,
        RanToCompletion = 5,
        Running = 3,
        WaitingForActivation = 1,
        WaitingForChildrenToComplete = 4,
        WaitingToRun = 2,
    }
    public partial class UnobservedTaskExceptionEventArgs : System.EventArgs
    {
        public UnobservedTaskExceptionEventArgs(System.AggregateException exception) { }
        public System.AggregateException Exception { get { return default(System.AggregateException); } }
        public bool Observed { get { return default(bool); } }
        public void SetObserved() { }
    }
}
