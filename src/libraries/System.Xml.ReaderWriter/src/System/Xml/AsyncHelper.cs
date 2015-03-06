// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace System.Xml
{
    internal static class AsyncHelper
    {
        public static readonly Task DoneTask = Task.FromResult(true);

        public static readonly Task<bool> DoneTaskTrue = Task.FromResult(true);

        public static readonly Task<bool> DoneTaskFalse = Task.FromResult(false);

        public static readonly Task<int> DoneTaskZero = Task.FromResult(0);

        public static bool IsSuccess(this Task task)
        {
            return task.IsCompleted && task.Exception == null;
        }

        public static Task CallVoidFuncWhenFinish(this Task task, Action func)
        {
            if (task.IsSuccess())
            {
                func();
                return DoneTask;
            }
            else
            {
                return _CallVoidFuncWhenFinish(task, func);
            }
        }

        private static async Task _CallVoidFuncWhenFinish(this Task task, Action func)
        {
            await task.ConfigureAwait(false);
            func();
        }

        public static Task<bool> ReturnTaskBoolWhenFinish(this Task task, bool ret)
        {
            if (task.IsSuccess())
            {
                if (ret)
                    return DoneTaskTrue;
                else
                    return DoneTaskFalse;
            }
            else
            {
                return _ReturnTaskBoolWhenFinish(task, ret);
            }
        }

        public static async Task<bool> _ReturnTaskBoolWhenFinish(this Task task, bool ret)
        {
            await task.ConfigureAwait(false);
            return ret;
        }

        public static Task CallTaskFuncWhenFinish(this Task task, Func<Task> func)
        {
            if (task.IsSuccess())
            {
                return func();
            }
            else
            {
                return _CallTaskFuncWhenFinish(task, func);
            }
        }

        private static async Task _CallTaskFuncWhenFinish(Task task, Func<Task> func)
        {
            await task.ConfigureAwait(false);
            await func().ConfigureAwait(false);
        }

        public static Task<bool> CallBoolTaskFuncWhenFinish(this Task task, Func<Task<bool>> func)
        {
            if (task.IsSuccess())
            {
                return func();
            }
            else
            {
                return _CallBoolTaskFuncWhenFinish(task, func);
            }
        }

        private static async Task<bool> _CallBoolTaskFuncWhenFinish(this Task task, Func<Task<bool>> func)
        {
            await task.ConfigureAwait(false);
            return await func().ConfigureAwait(false);
        }

        public static Task<bool> ContinueBoolTaskFuncWhenFalse(this Task<bool> task, Func<Task<bool>> func)
        {
            if (task.IsSuccess())
            {
                if (task.Result)
                    return DoneTaskTrue;
                else
                    return func();
            }
            else
            {
                return _ContinueBoolTaskFuncWhenFalse(task, func);
            }
        }

        private static async Task<bool> _ContinueBoolTaskFuncWhenFalse(Task<bool> task, Func<Task<bool>> func)
        {
            if (await task.ConfigureAwait(false))
                return true;
            else
                return await func().ConfigureAwait(false);
        }
    }
}
