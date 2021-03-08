// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Drawing.Tests
{
    public class GdiplusTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsOSX))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49111", typeof(PlatformDetection), nameof(PlatformDetection.IsMacOsAppleSilicon))]
        public void IsAtLeastLibgdiplus6()
        {
            Assert.True(Helpers.GetIsWindowsOrAtLeastLibgdiplus6());
        }
    }
}
