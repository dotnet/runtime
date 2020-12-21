// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting.IntegrationTesting
{
    public class RetryHelper
    {
        public static void RetryOperation(
            Action retryBlock,
            Action<Exception> exceptionBlock,
            int retryCount = 3,
            int retryDelayMilliseconds = 0)
        {
            for (var retry = 0; retry < retryCount; ++retry)
            {
                try
                {
                    retryBlock();
                    break;
                }
                catch (Exception exception)
                {
                    exceptionBlock(exception);
                }

                Thread.Sleep(retryDelayMilliseconds);
            }
        }
    }
}
