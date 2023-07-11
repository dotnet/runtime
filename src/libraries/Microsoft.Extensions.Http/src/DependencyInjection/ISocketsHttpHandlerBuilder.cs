// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET5_0_OR_GREATER
namespace Microsoft.Extensions.DependencyInjection
{
    public interface ISocketsHttpHandlerBuilder
    {
        string Name { get; }

        IServiceCollection Services { get; }
    }
}
#endif
