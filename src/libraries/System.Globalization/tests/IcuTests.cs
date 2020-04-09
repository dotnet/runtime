// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Xunit;
using Xunit.Sdk;

namespace System.Globalization.Tests
{
    public class IcuTests
    {
        private static bool IsIcuCompatiblePlatform => PlatformDetection.IsNotWindows ||
                                                       (!PlatformDetection.IsMonoRuntime &&
                                                       PlatformDetection.IsWindows10Version1903OrGreater);

        [ConditionalFact(nameof(IsIcuCompatiblePlatform))]
        public static void IcuShouldBeUsedByDefault()
        {
            Type globalizationMode = Type.GetType("System.Globalization.GlobalizationMode");
            if (globalizationMode != null)
            {
                MethodInfo methodInfo = globalizationMode.GetProperty("UseNls", BindingFlags.NonPublic | BindingFlags.Static)?.GetMethod;
                if (methodInfo != null)
                {
                    Assert.False((bool)methodInfo.Invoke(null, null));
                    return;
                }
            }

            throw new XunitException("Couldn't get System.Globalization.GlobalizationMode.UseIcu property.");
        }

        [ConditionalFact(nameof(IsIcuCompatiblePlatform))]
        public static void IcuShouldBeLoaded()
        {
            Assert.True(PlatformDetection.IsIcuGlobalization);
        }
    }
}
