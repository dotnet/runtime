// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
