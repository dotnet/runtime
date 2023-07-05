// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Http.Logging;

namespace Microsoft.Extensions.DependencyInjection
{
    public interface IHttpClientLoggingBuilder
    {
        string Name { get; }
        IServiceCollection Services { get; }

        IHttpClientLoggingBuilder AddLogger(Func<IServiceProvider, IHttpClientLogger> httpClientLoggerFactory, bool wrapHandlersPipeline = false);

        IHttpClientLoggingBuilder RemoveAllLoggers();
    }
}
