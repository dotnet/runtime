// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
