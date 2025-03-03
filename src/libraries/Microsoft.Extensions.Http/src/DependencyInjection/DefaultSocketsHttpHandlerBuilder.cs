// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET
namespace Microsoft.Extensions.DependencyInjection
{
    internal sealed class DefaultSocketsHttpHandlerBuilder : ISocketsHttpHandlerBuilder
    {
        public DefaultSocketsHttpHandlerBuilder(IServiceCollection services, string name)
        {
            Services = services;
            Name = name;
        }

        public string Name { get; }

        public IServiceCollection Services { get; }
    }
}
#endif
