// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets
{
    internal static class FireAndForgetHelper
    {
        // "Observe" either a ValueTask result, or any exception, ignoring it
        // to prevent the unobserved exception event from being raised.
        public static void Observe(ValueTask t)
        {
            if (t.IsCompletedSuccessfully)
            {
                t.GetAwaiter().GetResult();
            }
            else
            {
                ObserveException(t.AsTask());
            }
        }

        // "Observe" any exception, ignoring it to prevent the unobserved
        // exception event from being raised.
        public static void ObserveException(Task t)
        {
            t.ContinueWith(static p => { _ = p.Exception; },
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
    }
}
