// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
    public static partial class AsyncHelpers
    {
        private sealed unsafe class TaskContinuation : Continuation, ITaskCompletionAction, IThreadPoolWorkItem
        {
            internal Task? Task;
            internal Task? RuntimeAsyncTask;
            private delegate*<Task, ref byte, void> _getResult;
            internal object? ContinuationContext;

            public TaskContinuation()
            {
                ResumeInfo = (ResumeInfo*)Unsafe.AsPointer(in TaskContinuationResume.ResumeInfo);
            }

            void ITaskCompletionAction.Invoke(Task completingTask)
            {
                if (!QueueIfNecessary())
                {
                    RuntimeAsyncTask!.ExecuteDirectly(null);
                }
            }

            void IThreadPoolWorkItem.Execute()
            {
                if (!QueueIfNecessary())
                {
                    RuntimeAsyncTask!.ExecuteDirectly(null);
                }
            }

            private bool QueueIfNecessary()
            {
                Debug.Assert(RuntimeAsyncTask != null);

                if ((Flags & ContinuationFlags.ContinueOnThreadPool) != 0)
                {
                    Flags &= ~ContinuationFlags.ContinueOnThreadPool;
                    SynchronizationContext? ctx = Thread.CurrentThreadAssumedInitialized._synchronizationContext;
                    if (ctx == null || ctx.GetType() == typeof(SynchronizationContext))
                    {
                        TaskScheduler? sched = TaskScheduler.InternalCurrent;
                        if (sched == null || sched == TaskScheduler.Default)
                        {
                            // Can inline
                            return false;
                        }
                    }

                    ThreadPool.UnsafeQueueUserWorkItemInternal(RuntimeAsyncTask, preferLocal: true);
                    return true;
                }

                if ((Flags & ContinuationFlags.ContinueOnCapturedSynchronizationContext) != 0)
                {
                    Flags &= ~ContinuationFlags.ContinueOnCapturedSynchronizationContext;

                    object? continuationContext = ContinuationContext;
                    Debug.Assert(continuationContext is SynchronizationContext { });
                    SynchronizationContext continuationSyncCtx = (SynchronizationContext)continuationContext;

                    if (continuationSyncCtx == Thread.CurrentThreadAssumedInitialized._synchronizationContext)
                    {
                        return false;
                    }

                    try
                    {
                        continuationSyncCtx.Post(TaskContinuationResume.s_postCallback, RuntimeAsyncTask);
                    }
                    catch (Exception ex)
                    {
                        Task.ThrowAsync(ex, targetContext: null);
                    }

                    return true;
                }

                if ((Flags & ContinuationFlags.ContinueOnCapturedTaskScheduler) != 0)
                {
                    Flags &= ~ContinuationFlags.ContinueOnCapturedTaskScheduler;
                    object? continuationContext = ContinuationContext;
                    Debug.Assert(continuationContext is TaskScheduler { });
                    TaskScheduler sched = (TaskScheduler)continuationContext;

                    // TODO: We do not need TaskSchedulerAwaitTaskContinuation here, just need to refactor its Run method...
                    var taskSchedCont = new TaskSchedulerAwaitTaskContinuation(sched, (Action)RuntimeAsyncTask.m_action!, flowExecutionContext: false);
                    taskSchedCont.Run(Task.CompletedTask, canInlineContinuationTask: true);

                    return true;
                }

                return false;
            }

            bool ITaskCompletionAction.InvokeMayRunArbitraryCode => true;

            public void GetResult(ref byte returnValue)
            {
                Debug.Assert(Task != null);

                // Avoid retaining the task. The call below may throw.
                Task task = Task;
                Task = null;

                _getResult(task, ref returnValue);
            }

            public void Initialize(Task task)
            {
                Task = task;
                _getResult = &GetResult;
            }

            public void Initialize<T>(Task<T> task)
            {
                Task = task;
                _getResult = &GetResult<T>;
            }

            private static void GetResult(Task task, ref byte result)
            {
                TaskAwaiter.ValidateEnd(task);
            }

            private static void GetResult<T>(Task task, ref byte result)
            {
                Debug.Assert(task is Task<T>);

                Task<T> taskOfT = Unsafe.As<Task, Task<T>>(ref task);
                TaskAwaiter.ValidateEnd(taskOfT);
                Unsafe.As<byte, T>(ref result) = taskOfT.ResultOnSuccess;
            }

            private static class TaskContinuationResume
            {
                [FixedAddressValueType]
                public static readonly ResumeInfo ResumeInfo = new ResumeInfo
                {
                    DiagnosticIP = null,
                    Resume = &ResumeTaskContinuation,
                };

                private static Continuation? ResumeTaskContinuation(Continuation cont, ref byte result)
                {
                    var taskCont = (TaskContinuation)cont;
                    taskCont.Next = null;
                    taskCont.RuntimeAsyncTask = null;
                    taskCont.ContinuationContext = null;

                    Debug.Assert((taskCont.Flags & ContinuationFlags.AllContinuationFlags) == 0);

                    t_runtimeAsyncAwaitState.CachedTaskContinuation = taskCont;

                    taskCont.GetResult(ref result);
                    return null;
                }

                internal static readonly SendOrPostCallback s_postCallback = static state =>
                {
                    Debug.Assert(state is Task);
                    ((Task)state).ExecuteDirectly(null);
                };
            }
        }
    }
}
