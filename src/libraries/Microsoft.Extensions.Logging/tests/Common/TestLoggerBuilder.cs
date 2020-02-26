// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Logging.Test
{
    public static class TestLoggerBuilder
    {
        public static ILoggerFactory Create(Action<ILoggingBuilder> configure)
        {
            return new ServiceCollection()
                .AddLogging(configure)
                .BuildServiceProvider()
                .GetRequiredService<ILoggerFactory>();
        }
    }
}
