// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Microsoft.VisualBasic.Devices.Tests
{
    public class ComputerInfoTests
    {
        [Fact]
        public void Properties()
        {
            var info = new ComputerInfo();
            Assert.Equal(System.Globalization.CultureInfo.InstalledUICulture, info.InstalledUICulture);
            Assert.Equal(System.Environment.OSVersion.Platform.ToString(), info.OSPlatform);
            Assert.Equal(System.Environment.OSVersion.Version.ToString(), info.OSVersion);
        }
    }
}
