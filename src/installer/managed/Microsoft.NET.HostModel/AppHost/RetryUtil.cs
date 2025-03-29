// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;

namespace Microsoft.NET.HostModel
{
    /// <summary>
    /// HostModel library implements several services for updating the AppHost DLL.
    /// These updates involve multiple file open/close operations.
    /// An Antivirus scanner may intercept in-between and lock the file,
    /// causing the operations to fail with IO-Error.
    /// So, the operations are retried a few times on failures such as
    /// - IOException
    /// </summary>
    public static class RetryUtil
    {
        public const int NumberOfRetries = 500;
        public const int NumMilliSecondsToWait = 100;

        public static void RetryOnIOError(Action func)
        {
            for (int i = 1; i <= NumberOfRetries; i++)
            {
                try
                {
                    func();
                    break;
                }
                catch (IOException) when (i < NumberOfRetries)
                {
                    Thread.Sleep(NumMilliSecondsToWait);
                }
            }
        }
    }
}
