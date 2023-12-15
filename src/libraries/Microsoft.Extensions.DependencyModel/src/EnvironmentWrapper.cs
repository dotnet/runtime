// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
#if !NETFRAMEWORK
using System.Runtime.InteropServices;
#endif

namespace Microsoft.Extensions.DependencyModel
{
    internal sealed class EnvironmentWrapper : IEnvironment
    {
        public static IEnvironment Default = new EnvironmentWrapper();

        public string? GetEnvironmentVariable(string name) => Environment.GetEnvironmentVariable(name);

        public object? GetAppContextData(string name) => AppDomain.CurrentDomain.GetData(name);

        public bool IsWindows() =>
#if NETFRAMEWORK
            Environment.OSVersion.Platform == PlatformID.Win32NT;
#else
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#endif
    }
}
