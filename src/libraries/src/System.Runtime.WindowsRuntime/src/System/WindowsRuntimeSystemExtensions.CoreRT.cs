// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading;
using Windows.Foundation;

using Internal.Interop;
using Internal.Threading.Tasks;

namespace System
{
    /// <summary>Provides extension methods in the System namespace for working with the Windows Runtime.<br />
    /// Currently contains:<br />
    /// <ul>
    /// <li>Extension methods for conversion between <code>Windows.Foundation.IAsyncInfo</code> and deriving generic interfaces
    ///     and <code>System.Threading.Tasks.Task</code>.</li>
    /// <li>Extension methods for conversion between <code>System.Threading.Tasks.Task</code>
    ///     and <code>Windows.Foundation.IAsyncInfo</code> and deriving generic interfaces.</li>
    /// </ul></summary>
    [CLSCompliant(false)]
    public static class WindowsRuntimeSystemExtensions
    {
        #region Converters from Windows.Foundation.IAsyncInfo (and interfaces that derive from it) to System.Threading.Tasks.Task

        #region Convenience Helpers

        private static void ConcatenateCancelTokens(CancellationToken source, CancellationTokenSource sink, Task concatenationLifetime)
        {
            Debug.Assert(sink != null);

            CancellationTokenRegistration ctReg = source.Register((state) => { ((CancellationTokenSource)state).Cancel(); },
                                                                  sink);

            concatenationLifetime.ContinueWith((_, state) => { ((CancellationTokenRegistration)state).Dispose(); },
                                               ctReg, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }


        private static void ConcatenateProgress<TProgress>(IAsyncActionWithProgress<TProgress> source, IProgress<TProgress> sink)
        {
            // This is separated out into a separate method so that we only pay the costs of compiler-generated closure if progress is non-null.
            source.Progress += new AsyncActionProgressHandler<TProgress>((_, info) => sink.Report(info));
        }


        private static void ConcatenateProgress<TResult, TProgress>(IAsyncOperationWithProgress<TResult, TProgress> source, IProgress<TProgress> sink)
        {
            // This is separated out into a separate method so that we only pay the costs of compiler-generated closure if progress is non-null.
            source.Progress += new AsyncOperationProgressHandler<TResult, TProgress>((_, info) => sink.Report(info));
        }

        #endregion Convenience Helpers


        #region Converters from IAsyncAction to Task

        /// <summary>Gets an awaiter used to await this asynchronous operation.</summary>
        /// <returns>An awaiter instance.</returns>
        /// <remarks>This method is intended for compiler user rather than use directly in code.</remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static TaskAwaiter GetAwaiter(this IAsyncAction source)
        {
            return AsTask(source).GetAwaiter();
        }


        /// <summary>Gets a Task to represent the asynchronous operation.</summary>
        /// <param name="source">The asynchronous operation.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        public static Task AsTask(this IAsyncAction source)
        {
            return AsTask(source, CancellationToken.None);
        }


        /// <summary>Gets a Task to represent the asynchronous operation.</summary>
        /// <param name="source">The asynchronous operation.</param>
        /// <param name="cancellationToken">The token used to request cancellation of the asynchronous operation.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        public static Task AsTask(this IAsyncAction source, CancellationToken cancellationToken)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            // If source is actually a NetFx-to-WinRT adapter, unwrap it instead of creating a new Task:
            var wrapper = source as TaskToAsyncActionAdapter;
            if (wrapper != null && !wrapper.CompletedSynchronously)
            {
                Task innerTask = wrapper.Task;
                Debug.Assert(innerTask != null);
                Debug.Assert(innerTask.Status != TaskStatus.Created);

                if (!innerTask.IsCompleted)
                {
                    // The race here is benign: If the task completes here, the concatination is useless, but not damaging.
                    if (cancellationToken.CanBeCanceled && wrapper.CancelTokenSource != null)
                        ConcatenateCancelTokens(cancellationToken, wrapper.CancelTokenSource, innerTask);
                }

                return innerTask;
            }

            // Fast path to return a completed Task if the operation has already completed:
            switch (source.Status)
            {
                case AsyncStatus.Completed:
                    return Task.CompletedTask;

                case AsyncStatus.Error:
                    return Task.FromException(source.ErrorCode.AttachRestrictedErrorInfo());

                case AsyncStatus.Canceled:
                    return Task.FromCanceled(cancellationToken.IsCancellationRequested ? cancellationToken : new CancellationToken(true));
            }

            // Benign race: source may complete here. Things still work, just not taking the fast path.

            // Source is not a NetFx-to-WinRT adapter, but a native future. Hook up the task:
            var bridge = new AsyncInfoToTaskBridge<VoidValueTypeParameter>(cancellationToken);
            try
            {
                source.Completed = new AsyncActionCompletedHandler(bridge.CompleteFromAsyncAction);
                bridge.RegisterForCancellation(source);
            }
            catch
            {
                AsyncCausalitySupport.RemoveFromActiveTasks(bridge.Task);
            }
            return bridge.Task;
        }

        #endregion Converters from IAsyncAction to Task


        #region Converters from IAsyncOperation<TResult> to Task

        /// <summary>Gets an awaiter used to await this asynchronous operation.</summary>
        /// <returns>An awaiter instance.</returns>
        /// <remarks>This method is intended for compiler user rather than use directly in code.</remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static TaskAwaiter<TResult> GetAwaiter<TResult>(this IAsyncOperation<TResult> source)
        {
            return AsTask(source).GetAwaiter();
        }


        /// <summary>Gets a Task to represent the asynchronous operation.</summary>
        /// <param name="source">The asynchronous operation.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        public static Task<TResult> AsTask<TResult>(this IAsyncOperation<TResult> source)
        {
            return AsTask(source, CancellationToken.None);
        }


        /// <summary>Gets a Task to represent the asynchronous operation.</summary>
        /// <param name="source">The asynchronous operation.</param>
        /// <param name="cancellationToken">The token used to request cancellation of the asynchronous operation.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        public static Task<TResult> AsTask<TResult>(this IAsyncOperation<TResult> source, CancellationToken cancellationToken)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            // If source is actually a NetFx-to-WinRT adapter, unwrap it instead of creating a new Task:
            var wrapper = source as TaskToAsyncOperationAdapter<TResult>;
            if (wrapper != null && !wrapper.CompletedSynchronously)
            {
                Task<TResult> innerTask = wrapper.Task as Task<TResult>;
                Debug.Assert(innerTask != null);
                Debug.Assert(innerTask.Status != TaskStatus.Created);  // Is WaitingForActivation a legal state at this moment?

                if (!innerTask.IsCompleted)
                {
                    // The race here is benign: If the task completes here, the concatination is useless, but not damaging.
                    if (cancellationToken.CanBeCanceled && wrapper.CancelTokenSource != null)
                        ConcatenateCancelTokens(cancellationToken, wrapper.CancelTokenSource, innerTask);
                }

                return innerTask;
            }

            // Fast path to return a completed Task if the operation has already completed
            switch (source.Status)
            {
                case AsyncStatus.Completed:
                    return Task.FromResult(source.GetResults());

                case AsyncStatus.Error:
                    return Task.FromException<TResult>(source.ErrorCode.AttachRestrictedErrorInfo());

                case AsyncStatus.Canceled:
                    return Task.FromCanceled<TResult>(cancellationToken.IsCancellationRequested ? cancellationToken : new CancellationToken(true));
            }

            // Benign race: source may complete here. Things still work, just not taking the fast path.

            // Source is not a NetFx-to-WinRT adapter, but a native future. Hook up the task:
            var bridge = new AsyncInfoToTaskBridge<TResult>(cancellationToken);
            try
            {
                source.Completed = new AsyncOperationCompletedHandler<TResult>(bridge.CompleteFromAsyncOperation);
                bridge.RegisterForCancellation(source);
            }
            catch
            {
                AsyncCausalitySupport.RemoveFromActiveTasks(bridge.Task);
            }
            return bridge.Task;
        }

        #endregion Converters from IAsyncOperation<TResult> to Task


        #region Converters from IAsyncActionWithProgress<TProgress> to Task

        /// <summary>Gets an awaiter used to await this asynchronous operation.</summary>
        /// <returns>An awaiter instance.</returns>
        /// <remarks>This method is intended for compiler user rather than use directly in code.</remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static TaskAwaiter GetAwaiter<TProgress>(this IAsyncActionWithProgress<TProgress> source)
        {
            return AsTask(source).GetAwaiter();
        }


        /// <summary>Gets a Task to represent the asynchronous operation.</summary>
        /// <param name="source">The asynchronous operation.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        public static Task AsTask<TProgress>(this IAsyncActionWithProgress<TProgress> source)
        {
            return AsTask(source, CancellationToken.None, null);
        }


        /// <summary>Gets a Task to represent the asynchronous operation.</summary>
        /// <param name="source">The asynchronous operation.</param>
        /// <param name="cancellationToken">The token used to request cancellation of the asynchronous operation.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        public static Task AsTask<TProgress>(this IAsyncActionWithProgress<TProgress> source, CancellationToken cancellationToken)
        {
            return AsTask(source, cancellationToken, null);
        }


        /// <summary>Gets a Task to represent the asynchronous operation.</summary>
        /// <param name="source">The asynchronous operation.</param>
        /// <param name="progress">The progress object used to receive progress updates.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        public static Task AsTask<TProgress>(this IAsyncActionWithProgress<TProgress> source, IProgress<TProgress> progress)
        {
            return AsTask(source, CancellationToken.None, progress);
        }


        /// <summary>Gets a Task to represent the asynchronous operation.</summary>
        /// <param name="source">The asynchronous operation.</param>
        /// <param name="cancellationToken">The token used to request cancellation of the asynchronous operation.</param>
        /// <param name="progress">The progress object used to receive progress updates.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        public static Task AsTask<TProgress>(this IAsyncActionWithProgress<TProgress> source,
                                             CancellationToken cancellationToken, IProgress<TProgress> progress)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            // If source is actually a NetFx-to-WinRT adapter, unwrap it instead of creating a new Task:
            var wrapper = source as TaskToAsyncActionWithProgressAdapter<TProgress>;
            if (wrapper != null && !wrapper.CompletedSynchronously)
            {
                Task innerTask = wrapper.Task;
                Debug.Assert(innerTask != null);
                Debug.Assert(innerTask.Status != TaskStatus.Created);  // Is WaitingForActivation a legal state at this moment?

                if (!innerTask.IsCompleted)
                {
                    // The race here is benign: If the task completes here, the concatinations are useless, but not damaging.

                    if (cancellationToken.CanBeCanceled && wrapper.CancelTokenSource != null)
                        ConcatenateCancelTokens(cancellationToken, wrapper.CancelTokenSource, innerTask);

                    if (progress != null)
                        ConcatenateProgress(source, progress);
                }

                return innerTask;
            }

            // Fast path to return a completed Task if the operation has already completed:
            switch (source.Status)
            {
                case AsyncStatus.Completed:
                    return Task.CompletedTask;

                case AsyncStatus.Error:
                    return Task.FromException(source.ErrorCode.AttachRestrictedErrorInfo());

                case AsyncStatus.Canceled:
                    return Task.FromCanceled(cancellationToken.IsCancellationRequested ? cancellationToken : new CancellationToken(true));
            }

            // Benign race: source may complete here. Things still work, just not taking the fast path.

            // Forward progress reports:
            if (progress != null)
                ConcatenateProgress(source, progress);

            // Source is not a NetFx-to-WinRT adapter, but a native future. Hook up the task:
            var bridge = new AsyncInfoToTaskBridge<VoidValueTypeParameter>(cancellationToken);
            try
            {
                source.Completed = new AsyncActionWithProgressCompletedHandler<TProgress>(bridge.CompleteFromAsyncActionWithProgress);
                bridge.RegisterForCancellation(source);
            }
            catch
            {
                AsyncCausalitySupport.RemoveFromActiveTasks(bridge.Task);
            }
            return bridge.Task;
        }

        #endregion Converters from IAsyncActionWithProgress<TProgress> to Task


        #region Converters from IAsyncOperationWithProgress<TResult,TProgress> to Task

        /// <summary>Gets an awaiter used to await this asynchronous operation.</summary>
        /// <returns>An awaiter instance.</returns>
        /// <remarks>This method is intended for compiler user rather than use directly in code.</remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static TaskAwaiter<TResult> GetAwaiter<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source)
        {
            return AsTask(source).GetAwaiter();
        }


        /// <summary>Gets a Task to represent the asynchronous operation.</summary>
        /// <param name="source">The asynchronous operation.</param>
        /// <returns>The Task representing the started asynchronous operation.</returns>
        public static Task<TResult> AsTask<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source)
        {
            return AsTask(source, CancellationToken.None, null);
        }


        /// <summary>Gets a Task to represent the asynchronous operation.</summary>
        /// <param name="source">The asynchronous operation.</param>
        /// <param name="cancellationToken">The token used to request cancellation of the asynchronous operation.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        public static Task<TResult> AsTask<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source,
                                                               CancellationToken cancellationToken)
        {
            return AsTask(source, cancellationToken, null);
        }


        /// <summary>Gets a Task to represent the asynchronous operation.</summary>
        /// <param name="source">The asynchronous operation.</param>
        /// <param name="progress">The progress object used to receive progress updates.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        public static Task<TResult> AsTask<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source,
                                                               IProgress<TProgress> progress)
        {
            return AsTask(source, CancellationToken.None, progress);
        }


        /// <summary>Gets a Task to represent the asynchronous operation.</summary>
        /// <param name="source">The asynchronous operation.</param>
        /// <param name="cancellationToken">The token used to request cancellation of the asynchronous operation.</param>
        /// <param name="progress">The progress object used to receive progress updates.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        public static Task<TResult> AsTask<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source,
                                                               CancellationToken cancellationToken, IProgress<TProgress> progress)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            // If source is actually a NetFx-to-WinRT adapter, unwrap it instead of creating a new Task:
            var wrapper = source as TaskToAsyncOperationWithProgressAdapter<TResult, TProgress>;
            if (wrapper != null && !wrapper.CompletedSynchronously)
            {
                Task<TResult> innerTask = wrapper.Task as Task<TResult>;
                Debug.Assert(innerTask != null);
                Debug.Assert(innerTask.Status != TaskStatus.Created);  // Is WaitingForActivation a legal state at this moment?

                if (!innerTask.IsCompleted)
                {
                    // The race here is benign: If the task completes here, the concatinations are useless, but not damaging.

                    if (cancellationToken.CanBeCanceled && wrapper.CancelTokenSource != null)
                        ConcatenateCancelTokens(cancellationToken, wrapper.CancelTokenSource, innerTask);

                    if (progress != null)
                        ConcatenateProgress(source, progress);
                }

                return innerTask;
            }

            // Fast path to return a completed Task if the operation has already completed
            switch (source.Status)
            {
                case AsyncStatus.Completed:
                    return Task.FromResult(source.GetResults());

                case AsyncStatus.Error:
                    return Task.FromException<TResult>(source.ErrorCode.AttachRestrictedErrorInfo());

                case AsyncStatus.Canceled:
                    return Task.FromCanceled<TResult>(cancellationToken.IsCancellationRequested ? cancellationToken : new CancellationToken(true));
            }

            // Benign race: source may complete here. Things still work, just not taking the fast path.

            // Forward progress reports:
            if (progress != null)
                ConcatenateProgress(source, progress);

            // Source is not a NetFx-to-WinRT adapter, but a native future. Hook up the task:
            var bridge = new AsyncInfoToTaskBridge<TResult>(cancellationToken);
            try
            {
                source.Completed = new AsyncOperationWithProgressCompletedHandler<TResult, TProgress>(bridge.CompleteFromAsyncOperationWithProgress);
                bridge.RegisterForCancellation(source);
            }
            catch
            {
                AsyncCausalitySupport.RemoveFromActiveTasks(bridge.Task);
            }
            return bridge.Task;
        }

        #endregion Converters from IAsyncOperationWithProgress<TResult,TProgress> to Task

        #endregion Converters from Windows.Foundation.IAsyncInfo (and interfaces that derive from it) to System.Threading.Tasks.Task


        #region Converters from System.Threading.Tasks.Task to Windows.Foundation.IAsyncInfo (and interfaces that derive from it)


        public static IAsyncAction AsAsyncAction(this Task source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return new TaskToAsyncActionAdapter(source, underlyingCancelTokenSource: null);
        }


        public static IAsyncOperation<TResult> AsAsyncOperation<TResult>(this Task<TResult> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return new TaskToAsyncOperationAdapter<TResult>(source, underlyingCancelTokenSource: null);
        }
        #endregion Converters from System.Threading.Tasks.Task to Windows.Foundation.IAsyncInfo (and interfaces that derive from it)

    }  // class WindowsRuntimeSystemExtensions
}  // namespace

// WindowsRuntimeExtensions.cs
