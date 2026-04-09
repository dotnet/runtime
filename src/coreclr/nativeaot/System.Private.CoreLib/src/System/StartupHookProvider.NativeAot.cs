// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System
{
    internal static partial class StartupHookProvider
    {
#pragma warning disable CA2255
        [ModuleInitializer]
#pragma warning restore CA2255
        internal static void Initialize()
        {
            if (IsSupported)
                ProcessStartupHooks(Environment.GetEnvironmentVariable("DOTNET_STARTUP_HOOKS"));
        }
    }
}
