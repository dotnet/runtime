// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Internal;

namespace Microsoft.Extensions.Hosting
{
    public partial class HostBuilder
    {
        private static void AddLifetime(ServiceCollection services)
        {
            if (!OperatingSystem.IsAndroid() && !OperatingSystem.IsBrowser() && !OperatingSystem.IsIOS() && !OperatingSystem.IsMacCatalyst() && !OperatingSystem.IsTvOS())
            {
                services.AddSingleton<IHostLifetime, ConsoleLifetime>();
            }
            else
            {
                services.AddSingleton<IHostLifetime, NullLifetime>();
            }
        }
    }
}
