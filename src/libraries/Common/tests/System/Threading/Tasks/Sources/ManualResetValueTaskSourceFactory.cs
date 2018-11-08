// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.ExceptionServices;

namespace System.Threading.Tasks.Sources.Tests
{
    internal static class ManualResetValueTaskSourceFactory
    {
        public static ManualResetValueTaskSource<T> Completed<T>(T result, Exception error = null)
        {
            var vts = new ManualResetValueTaskSource<T>();
            if (error != null)
            {
                vts.SetException(error);
            }
            else
            {
                vts.SetResult(result);
            }
            return vts;
        }

        public static ManualResetValueTaskSource<T> Delay<T>(int delayMs, T result, Exception error = null)
        {
            var vts = new ManualResetValueTaskSource<T>();
            Task.Delay(delayMs).ContinueWith(_ =>
            {
                if (error != null)
                {
                    vts.SetException(error);
                }
                else
                {
                    vts.SetResult(result);
                }
            });
            return vts;
        }
    }
}
