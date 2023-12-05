// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript
{
    /// <summary>
    /// Extensions of SynchronizationContext which propagate errors and return values
    /// </summary>
    public static class SynchronizationContextExtension
    {
        public static void Send<T>(this SynchronizationContext self, Action<T> body, T value)
        {
            Exception? exc = default;
            self.Send((_value) =>
            {
                try
                {
                    body((T)_value!);
                }
                catch (Exception ex)
                {
                    exc = ex;
                }
            }, value);
            if (exc != null)
            {
                throw exc;
            }
        }

        public static TRes Send<TRes>(this SynchronizationContext self, Func<TRes> body)
        {
            TRes? value = default;
            Exception? exc = default;
            self.Send((_) =>
            {
                try
                {
                    value = body();
                }
                catch (Exception ex)
                {
                    exc = ex;
                }
            }, null);
            if (exc != null)
            {
                throw exc;
            }
            return value!;
        }

        public static Task<TRes> Post<TRes>(this SynchronizationContext self, Func<Task<TRes>> body)
        {
            TaskCompletionSource<TRes> tcs = new TaskCompletionSource<TRes>();
            self.Post(async (_) =>
            {
                try
                {
                    var value = await body().ConfigureAwait(false);
                    tcs.TrySetResult(value);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }, null);
            return tcs.Task;
        }

        public static Task<TRes> Post<T1, TRes>(this SynchronizationContext? self, Func<T1, Task<TRes>> body, T1 p1)
        {
            if (self == null) return body(p1);

            TaskCompletionSource<TRes> tcs = new TaskCompletionSource<TRes>();
            self.Post(async (_) =>
            {
                try
                {
                    var value = await body(p1).ConfigureAwait(false);
                    tcs.TrySetResult(value);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }, null);
            return tcs.Task;
        }

        public static Task Post<T1>(this SynchronizationContext self, Func<T1, Task> body, T1 p1)
        {
            TaskCompletionSource tcs = new TaskCompletionSource();
            self.Post(async (_) =>
            {
                try
                {
                    await body(p1).ConfigureAwait(false);
                    tcs.TrySetResult();
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }, null);
            return tcs.Task;
        }

        public static Task Post(this SynchronizationContext self, Func<Task> body)
        {
            TaskCompletionSource tcs = new TaskCompletionSource();
            self.Post(async (_) =>
            {
                try
                {
                    await body().ConfigureAwait(false);
                    tcs.TrySetResult();
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }, null);
            return tcs.Task;
        }

        public static TRes Send<T1, TRes>(this SynchronizationContext self, Func<T1, TRes> body, T1 p1)
        {
            TRes? value = default;
            Exception? exc = default;
            self.Send((_) =>
            {
                try
                {
                    value = body(p1);
                }
                catch (Exception ex)
                {
                    exc = ex;
                }
            }, null);
            if (exc != null)
            {
                throw exc;
            }
            return value!;
        }
    }
}
