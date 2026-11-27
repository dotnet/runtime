// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Tests;

public static class TimeZoneDirectoryTests
{
    [Fact]
    [PlatformSpecific(TestPlatforms.Browser)]
    public static void BrowserIgnoresTimeZoneDirectoryAtStartup()
    {
        Assert.Equal("nonexistent-time-zone-directory", Environment.GetEnvironmentVariable("TZDIR"));
        Assert.Equal("Asia/Tokyo", Environment.GetEnvironmentVariable("TZ"));
        Assert.Equal("Asia/Tokyo", TimeZoneInfo.Local.Id);
        Assert.Equal(TimeSpan.FromHours(9), TimeZoneInfo.Local.BaseUtcOffset);
        Assert.Equal(TimeSpan.FromHours(9), TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo").BaseUtcOffset);
        Assert.Contains(TimeZoneInfo.GetSystemTimeZones(), zone => zone.Id == "Asia/Tokyo");
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Wasi)]
    public static void WasiUsesTimeZoneDirectoryAtStartup()
    {
        Assert.Equal("nonexistent-time-zone-directory", Environment.GetEnvironmentVariable("TZDIR"));
        Assert.Equal("Asia/Tokyo", Environment.GetEnvironmentVariable("TZ"));
        Assert.Equal(TimeZoneInfo.Utc, TimeZoneInfo.Local);
        Assert.Throws<TimeZoneNotFoundException>(() => TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo"));
        Assert.DoesNotContain(TimeZoneInfo.GetSystemTimeZones(), zone => zone.Id == "Asia/Tokyo");
    }
}
