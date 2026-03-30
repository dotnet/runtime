// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System
{
    /// <summary>Provides downlevel polyfills for static members on <see cref="Environment"/>.</summary>
    internal static class EnvironmentPolyfills
    {
        extension(Environment)
        {
            public static int ProcessId => Process.GetCurrentProcess().Id;
        }
    }
}
