// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript
{
    /// <summary>
    /// This is draft for possible public API of SynchronizationContext
    /// </summary>
    public static class SynchronizationContextExtension
    {
        public static Task<T> SendAsync<T>(this SynchronizationContext? self, Func<Task<T>> body)
        {
            if (self == null) return body();

            TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();
            self.Send((_) =>
            {
                try
                {
                    var promise = body();
                    promise.ContinueWith(done =>
                    {
                        PropagateCompletion(tcs, done);
                    }, TaskScheduler.Current);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }, null);
            return tcs.Task;
        }

        public static Task SendAsync(this SynchronizationContext? self, Func<Task> body)
        {
            if (self == null) return body();

            TaskCompletionSource tcs = new TaskCompletionSource();
            self.Send((_) =>
            {
                try
                {
                    var promise = body();
                    promise.ContinueWith(done =>
                    {
                        PropagateCompletion(tcs, done);
                    }, TaskScheduler.Current);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }, null);
            return tcs.Task;
        }

        public static void Send<T>(this SynchronizationContext? self, Action<T> body, T value)
        {
            if (self == null)
            {
                body(value);
                return;
            }

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

        public static TRes Send<TRes>(this SynchronizationContext? self, Func<TRes> body)
        {
            if (self == null) return body();

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

        public static TRes Send<T1, TRes>(this SynchronizationContext? self, Func<T1, TRes> body, T1 p1)
        {
            if (self == null) return body(p1);

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

        public static void PropagateCompletion<T>(this TaskCompletionSource<T> tcs, Task<T> done)
        {
            if (done.IsFaulted)
            {
                if(done.Exception is AggregateException ag && ag.InnerException!=null)
                {
                    tcs.SetException(ag.InnerException);
                }
                else
                {
                    tcs.SetException(done.Exception);
                }
            }
            else if (done.IsCanceled)
                tcs.SetCanceled();
            else
                tcs.SetResult(done.Result);
        }

        public static void PropagateCompletion(this TaskCompletionSource tcs, Task done)
        {
            if (done.IsFaulted)
            {
                if (done.Exception is AggregateException ag && ag.InnerException != null)
                {
                    tcs.SetException(ag.InnerException);
                }
                else
                {
                    tcs.SetException(done.Exception);
                }
            }
            else if (done.IsCanceled)
                tcs.SetCanceled();
            else
                tcs.SetResult();
        }
    }
}
