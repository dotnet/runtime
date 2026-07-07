// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
    public static partial class AsyncHelpers
    {
        private sealed unsafe class TaskContinuation : Continuation
        {
            internal Task? Task;
            private delegate*<Task, ref byte, void> _getResult;

            public TaskContinuation()
            {
                ResumeInfo = (ResumeInfo*)Unsafe.AsPointer(in TaskContinuationResume.ResumeInfo);
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
                    var taskCont = (TaskContinuation)cont;
                    taskCont.Next = null;

                    Debug.Assert((taskCont.Flags & ContinuationFlags.AllContinuationFlags) == 0);

                    t_runtimeAsyncAwaitState.CachedTaskContinuation = taskCont;

                    taskCont.GetResult(ref result);
                    return null;
                }
            }
        }
    }
}
