// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ConfigurationTests;
using Xunit;

namespace System.Configuration
{
    public class AppSettingsReaderTests
    {
        private readonly AppSettingsReader _appSettingsReader = new AppSettingsReader();

        [Fact]
        public void GetValue_KeyNull()
        {
            Assert.Throws<ArgumentNullException>(() => _appSettingsReader.GetValue(null, typeof(object)));
        }

        [Fact]
        public void GetValue_TypeNull()
        {
            Assert.Throws<ArgumentNullException>(() => _appSettingsReader.GetValue(string.Empty, null));
        }
    }
}
