// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Xunit;
using Xunit.Sdk;

namespace System.Globalization.Tests
{
    public class NlsSwitchTests
    {
        [Fact]
        public static void NlsRuntimeSwitchIsHonored()
        {
            Type globalizationMode = Type.GetType("System.Globalization.GlobalizationMode");
            if (globalizationMode != null)
            {
                MethodInfo methodInfo = globalizationMode.GetProperty("UseNls", BindingFlags.NonPublic | BindingFlags.Static)?.GetMethod;
                if (methodInfo != null)
                {
                    Assert.True((bool)methodInfo.Invoke(null, null));
                    return;
                }
            }

            throw new XunitException("Couldn't get System.Globalization.GlobalizationMode.UseIcu property.");
        }

        [Fact]
        public static void IcuShouldNotBeLoaded()
        {
            Assert.False(PlatformDetection.IsIcuGlobalization, $"Found ICU: {PlatformDetection.ICUVersion}");
        }
    }
}
