// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class PlatformSpecificFactAttribute : FactAttribute
{
    public PlatformSpecificFactAttribute(TestPlatforms platforms)
    {
        if (!TestPlatformApplies(platforms))
        {
            base.Skip = "Test only runs on platform(s): " + platforms;
        }
    }

    internal static bool TestPlatformApplies(TestPlatforms platforms)
    {
        if (!platforms.HasFlag(TestPlatforms.Any)
            && (!platforms.HasFlag(TestPlatforms.FreeBSD) || !RuntimeInformation.IsOSPlatform(OSPlatform.Create("FREEBSD")))
            && (!platforms.HasFlag(TestPlatforms.Linux) || !RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            && (!platforms.HasFlag(TestPlatforms.NetBSD) || !RuntimeInformation.IsOSPlatform(OSPlatform.Create("NETBSD")))
            && (!platforms.HasFlag(TestPlatforms.OSX) || !RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            && (!platforms.HasFlag(TestPlatforms.illumos) || !RuntimeInformation.IsOSPlatform(OSPlatform.Create("ILLUMOS")))
            && (!platforms.HasFlag(TestPlatforms.Solaris) || !RuntimeInformation.IsOSPlatform(OSPlatform.Create("SOLARIS")))
            && (!platforms.HasFlag(TestPlatforms.iOS) || !RuntimeInformation.IsOSPlatform(OSPlatform.Create("IOS")) || RuntimeInformation.IsOSPlatform(OSPlatform.Create("MACCATALYST")))
            && (!platforms.HasFlag(TestPlatforms.tvOS) || !RuntimeInformation.IsOSPlatform(OSPlatform.Create("TVOS")))
            && (!platforms.HasFlag(TestPlatforms.MacCatalyst) || !RuntimeInformation.IsOSPlatform(OSPlatform.Create("MACCATALYST")))
            && (!platforms.HasFlag(TestPlatforms.LinuxBionic) || !RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ANDROID_STORAGE")))
            && (!platforms.HasFlag(TestPlatforms.Android) || !RuntimeInformation.IsOSPlatform(OSPlatform.Create("ANDROID")))
            && (!platforms.HasFlag(TestPlatforms.Browser) || !RuntimeInformation.IsOSPlatform(OSPlatform.Create("BROWSER")))
            && (!platforms.HasFlag(TestPlatforms.Wasi) || !RuntimeInformation.IsOSPlatform(OSPlatform.Create("WASI")))
            && (!platforms.HasFlag(TestPlatforms.Haiku) || !RuntimeInformation.IsOSPlatform(OSPlatform.Create("HAIKU")))
            && (!platforms.HasFlag(TestPlatforms.Windows) || !RuntimeInformation.IsOSPlatform(OSPlatform.Windows)))
        {
            return false;
        }
        return true;
    }
}