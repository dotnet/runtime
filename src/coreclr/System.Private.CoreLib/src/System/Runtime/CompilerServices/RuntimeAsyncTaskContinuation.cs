// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
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
            Debug.Assert(Task != null && RuntimeAsyncTask != null);

            Task raTask = RuntimeAsyncTask;
            object? continuationContext = ContinuationContext;
            ref ContinuationFlags flags = ref Flags;

            if (Task.IsCompletedSuccessfully)
            {
                // We are going to inline the completion to avoid the double continuation dispatch.
                Debug.Assert(Next != null);
                Continuation nextCont = Next;

                // We have two situations that are both compatible with a single "queue continuation" operation:
                // - This was a transparent await and the next continuation's continuation context is what matters
                //   (e.g. await Foo(), where Foo() is an async version)
                // - This was not a transparent await, e.g. "await AsyncHelpers.Await(someTask)". Next continuation
                //   does not have a continuation context and would run transparently.
                Debug.Assert(BitOperations.PopCount((nuint)((Flags | nextCont.Flags) & ContinuationFlags.AllContinuationFlags)) <= 1);

                nextCont.Flags |= Flags & ContinuationFlags.AllContinuationFlags;
                Flags &= ~ContinuationFlags.AllContinuationFlags;
                flags = ref nextCont.Flags;

                raTask.m_stateObject = nextCont;

                TaskContinuationResume.ResumeTaskContinuation(this, ref nextCont.GetResultStorageOrNull());
            }

            if (((flags & ContinuationFlags.AllContinuationFlags) == 0) || !QueueIfNecessary(canInline, raTask, continuationContext, ref flags))
            {
                if (canInline)
                {
                    raTask.ExecuteDirectly(null);
                }
                else
                {
                    ThreadPool.UnsafeQueueUserWorkItemInternal(raTask, preferLocal: true);
                }
            }
        }

        private static bool QueueIfNecessary(bool canInline, Task raTask, object? continuationContext, ref ContinuationFlags flags)
        {
            if ((flags & ContinuationFlags.ContinueOnThreadPool) != 0)
            {
                flags &= ~ContinuationFlags.ContinueOnThreadPool;
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

                ThreadPool.UnsafeQueueUserWorkItemInternal(raTask, preferLocal: true);
                return true;
            }

            if ((flags & ContinuationFlags.ContinueOnCapturedSynchronizationContext) != 0)
            {
                flags &= ~ContinuationFlags.ContinueOnCapturedSynchronizationContext;

                Debug.Assert(continuationContext is SynchronizationContext { });
                SynchronizationContext continuationSyncCtx = (SynchronizationContext)continuationContext;

                if (canInline && continuationSyncCtx == Thread.CurrentThread._synchronizationContext)
                {
                    return false;
                }

                try
                {
                    continuationSyncCtx.Post(TaskContinuationResume.s_postCallback, raTask);
                }
                catch (Exception ex)
                {
                    Task.ThrowAsync(ex, targetContext: null);
                }

                return true;
            }

            if ((flags & ContinuationFlags.ContinueOnCapturedTaskScheduler) != 0)
            {
                flags &= ~ContinuationFlags.ContinueOnCapturedTaskScheduler;
                Debug.Assert(continuationContext is TaskScheduler { });
                TaskScheduler sched = (TaskScheduler)continuationContext;

                TaskSchedulerAwaitTaskContinuation.RunOrScheduleAction((Action)raTask.m_action!, sched, capturedContext: null, allowInlining: canInline);

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

            internal static Continuation? ResumeTaskContinuation(Continuation cont, ref byte result)
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
