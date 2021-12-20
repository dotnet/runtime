// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Tests
{
    public partial class AppContextTests
    {
        [Theory]
        [InlineData("AppContext_Case1", 123)]
        [InlineData("AppContext_Case2", "")]
        [InlineData("AppContext_Case3", null)]
        public void AppContext_GetSetDataTest(string dataKey, object value)
        {
            // Set data
            AppContext.SetData(dataKey, value);

            // Get previously set data
            object actual = AppContext.GetData(dataKey);

            // Validate data equality
            Assert.Equal(value, actual);

            // Validate instance equality
            Assert.Same(value, actual);
        }

        [Fact]
        public void AppContext_ThrowTest()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => AppContext.SetData(null, 123));
            Assert.Contains("name", exception.Message);
        }
    }
}
