// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit.Sdk;

namespace System.Diagnostics.Tests
{
    internal static class Helpers
    {
        public static async Task RetryWithBackoff(Action action, int delayInMilliseconds = 10, int times = 10)
        {
            // Guards against delay growing to an exceptionally large value. No special technical significance to 
            // the value chosen--just seemed like a good balancing point between allowing the delay to increase 
            // incrementally and keeping tests from taking a long time to complete.
            const int maxDelayInMilliseconds = 10000;

            if (delayInMilliseconds > maxDelayInMilliseconds)
                throw new ArgumentOutOfRangeException(nameof(delayInMilliseconds), $"Exceeds maximum allowed delay of {maxDelayInMilliseconds}");

            for (; times > 0; times--)
            {
                try
                {
                    action();
                    return;
                }
                catch (XunitException) when (times > 1)
                {
                    await Task.Delay(delayInMilliseconds);
                    delayInMilliseconds = Math.Min(maxDelayInMilliseconds, delayInMilliseconds * 2);
                }
            }
        }
    }
}
