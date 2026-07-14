// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
    internal sealed unsafe class RuntimeAsyncTaskContinuation : Continuation, IThreadPoolWorkItem
    {
        internal Task? Task;
        internal Task? RuntimeAsyncTask;
        private delegate*<Task, ref byte, void> _getResult;
        internal object? ContinuationContext;

        public RuntimeAsyncTaskContinuation()
        {
            ResumeInfo = (ResumeInfo*)Unsafe.AsPointer(in TaskContinuationResume.ResumeInfo);
        }

        void IThreadPoolWorkItem.Execute()
        {
            Execute(canInline: true);
        }

        internal void Execute(bool canInline)
        {
            Debug.Assert(RuntimeAsyncTask != null);

            if (((Flags & ContinuationFlags.AllContinuationFlags) == 0) || !QueueIfNecessary(canInline))
            {
                if (canInline)
                {
                    RuntimeAsyncTask.ExecuteDirectly(null);
                }
                else
                {
                    ThreadPool.UnsafeQueueUserWorkItemInternal(RuntimeAsyncTask, preferLocal: true);
                }
            }
        }

        private bool QueueIfNecessary(bool canInline)
        {
            Debug.Assert(RuntimeAsyncTask != null);

            if ((Flags & ContinuationFlags.ContinueOnThreadPool) != 0)
            {
                Flags &= ~ContinuationFlags.ContinueOnThreadPool;
                SynchronizationContext? ctx = Thread.CurrentThread._synchronizationContext;
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

                if (canInline && continuationSyncCtx == Thread.CurrentThread._synchronizationContext)
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

                TaskSchedulerAwaitTaskContinuation.RunOrScheduleAction((Action)RuntimeAsyncTask.m_action!, sched, capturedContext: null, allowInlining: canInline);

                return true;
            }

            return false;
        }

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
                var taskCont = (RuntimeAsyncTaskContinuation)cont;
                taskCont.Next = null;
                taskCont.RuntimeAsyncTask = null;
                taskCont.ContinuationContext = null;

                Debug.Assert((taskCont.Flags & ContinuationFlags.AllContinuationFlags) == 0);

                AsyncHelpers.ReturnTaskContinuation(taskCont);

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
