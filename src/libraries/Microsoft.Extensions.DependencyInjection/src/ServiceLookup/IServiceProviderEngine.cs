// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal interface IServiceProviderEngine : IServiceProvider, IDisposable, IAsyncDisposable
    {
        IServiceScope RootScope { get; }
        void ValidateService(ServiceDescriptor descriptor);
    }
}
