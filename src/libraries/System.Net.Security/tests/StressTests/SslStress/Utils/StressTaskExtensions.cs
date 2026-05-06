// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace SslStress.Utils
{
    public static class StressTaskExtensions
    {

        /// <summary>
        /// Starts and awaits a collection of cancellable tasks.
        /// Will surface the first exception that has occurred (instead of AggregateException)
        /// and trigger cancellation for all sibling tasks.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="tasks"></param>
        /// <returns></returns>
        public static async Task WhenAllThrowOnFirstException(CancellationToken token, params Func<CancellationToken, Task>[] tasks)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            Exception? firstException = null;

            await Task.WhenAll(tasks.Select(RunOne));

            if (firstException != null)
            {
                ExceptionDispatchInfo.Capture(firstException).Throw();
            }

            async Task RunOne(Func<CancellationToken, Task> task)
            {
                try
                {
                    await Task.Run(() => task(cts.Token));
                }
                catch (Exception e)
                {
                    if (Interlocked.CompareExchange(ref firstException, e, null) == null)
                    {
                        cts.Cancel();
                    }
                }
            }
        }
    }
}
