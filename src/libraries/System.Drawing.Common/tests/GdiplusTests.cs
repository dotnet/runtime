// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Text;
using System.Reflection;
using Xunit;
using Xunit.Sdk;

namespace System.Drawing.Tests
{
    public class GdiplusTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsOSX))]
        public void IsAtLeastLibgdiplus6()
        {
            Assert.True(Helpers.GetIsWindowsOrAtLeastLibgdiplus6());
        }

        [Fact]
        public void GdiplusDefaultApiVersion()
        {
            Type startupInput = typeof(Bitmap).Assembly.GetType("System.Drawing.SafeNativeMethods+StartupInput");
            if (startupInput != null)
            {
                MethodInfo methodInfo = startupInput.GetMethod("GetDefault", BindingFlags.Public | BindingFlags.Static);
                if (methodInfo != null)
                {
                    object startupInputObject = methodInfo.Invoke(null, null);
                    int? version = (int?)startupInput.GetField("GdiplusVersion")?.GetValue(startupInputObject);
                    if (version.HasValue)
                    {
                        int expectedValue = PlatformDetection.IsWindows7 ? 1 : 2;
                        Assert.Equal(expectedValue, version.Value);
                        return;
                    }
                }
            }

            throw new XunitException("Couldn't get System.Drawing.SafeNativeMethods+StartupInput.GdiplusVersion field value.");
        }
    }
}
