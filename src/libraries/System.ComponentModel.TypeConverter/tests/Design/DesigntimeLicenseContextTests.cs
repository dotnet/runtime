// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.Design.Tests
{
    public class DesigntimeLicenseContextTests
    {
        [Fact]
        public void UsageMode_Get_ReturnsDesigntime()
        {
            var context = new DesigntimeLicenseContext();
            Assert.Equal(LicenseUsageMode.Designtime, context.UsageMode);
        }

        [Fact]
        public void GetSavedLicenseKey_Invoke_ReturnsNull()
        {
            var context = new DesigntimeLicenseContext();
            Assert.Null(context.GetSavedLicenseKey(null, null));
        }

        [Fact]
        public void GetService_Invoke_ReturnsNull()
        {
            var context = new DesigntimeLicenseContext();
            Assert.Null(context.GetService(null));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("key")]
        public void SetSavedLicenseKey_ValidType_Success(string key)
        {
            var context = new DesigntimeLicenseContext();
            context.SetSavedLicenseKey(typeof(int), key);
        }

        [Fact]
        public void SetSavedLicenseKey_NullType_ThrowsNullReferenceException()
        {
            var context = new DesigntimeLicenseContext();
            AssertExtensions.Throws<ArgumentNullException, NullReferenceException>("type", () => context.SetSavedLicenseKey(null, "Key"));
        }
    }
}
