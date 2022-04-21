// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Xunit;
using Xunit.Sdk;

namespace System.Globalization.Tests
{
    public class IcuTests
    {
        private static bool IsIcuCompatiblePlatform => PlatformDetection.IsNotWindows ||
                                                       (PlatformDetection.IsWindows10Version1903OrGreater &&
                                                        // Server core doesn't have icu.dll on SysWOW64
                                                        !(PlatformDetection.IsWindowsServerCore && PlatformDetection.IsX86Process));

        [ConditionalFact(nameof(IsIcuCompatiblePlatform))]
        public static void IcuShouldBeUsedByDefault()
        {
            Type globalizationMode = Type.GetType("System.Globalization.GlobalizationMode");
            MethodInfo methodInfo = globalizationMode?.GetProperty("UseNls", BindingFlags.NonPublic | BindingFlags.Static)?.GetMethod;

            if (methodInfo != null)
            {
                // When GlobalizationMode is present
                Assert.False((bool)methodInfo.Invoke(null, null));
            }
            else
            {
                // When GlobalizationMode is trimmed
                Type cultureDataType = Type.GetType("System.Globalization.CultureData");
                Assert.NotNull(cultureDataType);

                methodInfo = cultureDataType.GetMethod("NlsGetCultureDataFromRegionName", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.Null(methodInfo);

                methodInfo = cultureDataType.GetMethod("InitIcuCultureDataCore", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                Assert.NotNull(methodInfo);
            }
        }

        [ConditionalFact(nameof(IsIcuCompatiblePlatform))]
        public static void IcuShouldBeLoaded()
        {
            Assert.True(PlatformDetection.IsIcuGlobalization);
        }
    }
}
