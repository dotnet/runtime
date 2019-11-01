// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using DPStressHarness;

namespace Stress.Data
{
    public enum SyncAsyncMode
    {
        Sync,           // call sync method, e.g. connection.Open(), and return completed task
        SyncOverAsync,  // call async method, e.g. connection.OpenAsync().Wait(), and return completed task
        Async           // call async method, e.g. connection.OpenAsync(), and return running task
    }

    public static class AsyncUtils
    {
        public static Task<TResult> SyncOrAsyncMethod<TResult>(Func<TResult> syncFunc, Func<Task<TResult>> asyncFunc, SyncAsyncMode mode)
        {
            switch (mode)
            {
                case SyncAsyncMode.Sync:
                    TResult result = syncFunc();
                    return Task.FromResult(result);

                case SyncAsyncMode.SyncOverAsync:
                    Task<TResult> t = asyncFunc();
                    WaitAndUnwrapException(t);
                    return t;

                case SyncAsyncMode.Async:
                    return asyncFunc();

                default:
                    throw new ArgumentException(mode.ToString());
            }
        }

        public static Task SyncOrAsyncMethod(Action syncFunc, Func<Task> asyncFunc, SyncAsyncMode mode)
        {
            switch (mode)
            {
                case SyncAsyncMode.Sync:
                    syncFunc();
                    return Task.CompletedTask;

                case SyncAsyncMode.SyncOverAsync:
                    Task t = asyncFunc();
                    WaitAndUnwrapException(t);
                    return t;

                case SyncAsyncMode.Async:
                    return asyncFunc();

                default:
                    throw new ArgumentException(mode.ToString());
            }
        }

        public static void WaitAll(params Task[] ts)
        {
            DeadlockDetection.DisableThreadAbort();
            try
            {
                Task.WaitAll(ts);
            }
            finally
            {
                DeadlockDetection.EnableThreadAbort();
            }
        }

        public static void WaitAllNullable(params Task[] ts)
        {
            DeadlockDetection.DisableThreadAbort();
            try
            {
                Task[] tasks = ts.Where(t => t != null).ToArray();
                Task.WaitAll(tasks);
            }
            finally
            {
                DeadlockDetection.EnableThreadAbort();
            }
        }

        public static void WaitAndUnwrapException(Task t)
        {
            DeadlockDetection.DisableThreadAbort();
            try
            {
                t.Wait();
            }
            catch (AggregateException ae)
            {
                // The callers of this API may not expect AggregateException, so throw the inner exception
                // If AggregateException contains more than one InnerExceptions, throw it out as it is,
                // because that is unexpected
                if ((ae.InnerExceptions != null) && (ae.InnerExceptions.Count == 1))
                {
                    if (ae.InnerException != null)
                    {
                        ExceptionDispatchInfo info = ExceptionDispatchInfo.Capture(ae.InnerException);
                        info.Throw();
                    }
                }

                throw;
            }
            finally
            {
                DeadlockDetection.EnableThreadAbort();
            }
        }

        public static T GetResult<T>(IAsyncResult result)
        {
            return GetResult<T>((Task<T>)result);
        }

        public static T GetResult<T>(Task<T> result)
        {
            DeadlockDetection.DisableThreadAbort();
            try
            {
                return result.Result;
            }
            finally
            {
                DeadlockDetection.EnableThreadAbort();
            }
        }

        public static SqlDataReader ExecuteReader(SqlCommand command)
        {
            DeadlockDetection.DisableThreadAbort();
            try
            {
                return command.ExecuteReader();
            }
            finally
            {
                DeadlockDetection.EnableThreadAbort();
            }
        }

        public static int ExecuteNonQuery(SqlCommand command)
        {
            DeadlockDetection.DisableThreadAbort();
            try
            {
                return command.ExecuteNonQuery();
            }
            finally
            {
                DeadlockDetection.DisableThreadAbort();
            }
        }

        public static XmlReader ExecuteXmlReader(SqlCommand command)
        {
            DeadlockDetection.DisableThreadAbort();
            try
            {
                return command.ExecuteXmlReader();
            }
            finally
            {
                DeadlockDetection.EnableThreadAbort();
            }
        }

        public static SyncAsyncMode ChooseSyncAsyncMode(Random rnd)
        {
            // Any mode is allowed
            return (SyncAsyncMode)rnd.Next(3);
        }
    }
}
