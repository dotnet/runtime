// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace System.Xml
{
    internal static class AsyncHelper
    {
        public static readonly Task<bool> DoneTaskTrue = Task.FromResult(true);

        public static readonly Task<bool> DoneTaskFalse = Task.FromResult(false);

        public static readonly Task<int> DoneTaskZero = Task.FromResult(0);

        public static bool IsSuccess(this Task task)
        {
            return task.IsCompletedSuccessfully;
        }

        public static Task CallVoidFuncWhenFinishAsync<TArg>(this Task task, Action<TArg> func, TArg arg)
        {
            if (task.IsSuccess())
            {
                func(arg);
                return Task.CompletedTask;
            }
            else
            {
                return CallVoidFuncWhenFinishCoreAsync(task, func, arg);
            }
        }

        private static async Task CallVoidFuncWhenFinishCoreAsync<TArg>(this Task task, Action<TArg> func, TArg arg)
        {
            await task.ConfigureAwait(false);
            func(arg);
        }

        public static Task<bool> ReturnTrueTaskWhenFinishAsync(this Task task)
        {
            return task.IsSuccess() ?
                DoneTaskTrue :
                ReturnTrueTaskWhenFinishCoreAsync(task);
        }

        private static async Task<bool> ReturnTrueTaskWhenFinishCoreAsync(this Task task)
        {
            await task.ConfigureAwait(false);
            return true;
        }

        public static Task CallTaskFuncWhenFinishAsync<TArg>(this Task task, Func<TArg, Task> func, TArg arg)
        {
            return task.IsSuccess() ?
                func(arg) :
                CallTaskFuncWhenFinishCoreAsync(task, func, arg);
        }

        private static async Task CallTaskFuncWhenFinishCoreAsync<TArg>(Task task, Func<TArg, Task> func, TArg arg)
        {
            await task.ConfigureAwait(false);
            await func(arg).ConfigureAwait(false);
        }

        public static Task<bool> CallBoolTaskFuncWhenFinishAsync<TArg>(this Task task, Func<TArg, Task<bool>> func, TArg arg)
        {
            return task.IsSuccess() ?
                func(arg) :
                CallBoolTaskFuncWhenFinishCoreAsync(task, func, arg);
        }

        private static async Task<bool> CallBoolTaskFuncWhenFinishCoreAsync<TArg>(this Task task, Func<TArg, Task<bool>> func, TArg arg)
        {
            await task.ConfigureAwait(false);
            return await func(arg).ConfigureAwait(false);
        }

        public static Task<bool> ContinueBoolTaskFuncWhenFalseAsync<TArg>(this Task<bool> task, Func<TArg, Task<bool>> func, TArg arg)
        {
            if (task.IsSuccess())
            {
                return task.Result ? DoneTaskTrue : func(arg);
            }
            else
            {
                return ContinueBoolTaskFuncWhenFalseCoreAsync(task, func, arg);
            }
        }

        private static async Task<bool> ContinueBoolTaskFuncWhenFalseCoreAsync<TArg>(Task<bool> task, Func<TArg, Task<bool>> func, TArg arg)
        {
            if (await task.ConfigureAwait(false))
                return true;
            else
                return await func(arg).ConfigureAwait(false);
        }
    }
}
