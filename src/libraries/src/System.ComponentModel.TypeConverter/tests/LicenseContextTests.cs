// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.Tests
{
    public class LicenseContextTests
    {
        [Fact]
        public void UsageMode_Get_ReturnsRuntime()
        {
            var context = new LicenseContext();
            Assert.Equal(LicenseUsageMode.Runtime, context.UsageMode);
        }
        
        [Fact]
        public void GetSavedLicenseKey_Invoke_ReturnsNull()
        {
            var context = new LicenseContext();
            Assert.Null(context.GetSavedLicenseKey(null, null));
        }

        [Fact]
        public void GetService_Invoke_ReturnsNull()
        {
            var context = new LicenseContext();
            Assert.Null(context.GetService(null));
        }

        [Fact]
        public void SetSavedLicenseKey_Invoke_Nop()
        {
            var context = new LicenseContext();
            context.SetSavedLicenseKey(null, null);
        }
    }
}
