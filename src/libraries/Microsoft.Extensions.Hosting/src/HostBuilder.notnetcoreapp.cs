// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Internal;

namespace Microsoft.Extensions.Hosting
{
    public partial class HostBuilder
    {
        private static void AddLifetime(ServiceCollection services)
        {
            services.AddSingleton<IHostLifetime, ConsoleLifetime>();
        }
    }
}
